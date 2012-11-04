using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ZXSpectrum.Z_80.Assembler
{
    public class ZAsm
    {
        TextReader reader;
        int[] Memory;
        Z80 Cpu;
        Dictionary<string, int> OpcodeMap;
        Dictionary<string, int> SymbolTable;

        List<string> Registers;
        List<string> Mnemonics;

        int locationCounter;    //  Location Counter
        int sourceLine;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cpu"></param>
        /// <param name="memory"></param>
        public ZAsm(Z80 cpu, int[] memory)
        {
            Cpu = cpu;
            Memory = memory;
            ReadOpcodeMap();

            Registers = new List<string>();
            ReadListFromFile("Registers.txt", Registers);

            Mnemonics = new List<string>();
            ReadListFromFile("Mnemonics.txt", Mnemonics);

            SymbolTable = new Dictionary<string,int>();   
        }

        /// <summary>
        /// Assembles the given file and runs the emulator until a HALT instruction is encountered.
        /// </summary>
        /// <param name="sourcePath"></param>
        public void AssembleAndRun(string sourcePath)
        {
            DoSecondPass(DoFirstPass(sourcePath));
            Cpu.Run(true);
            Console.WriteLine(sourcePath);
            Console.WriteLine("Execution complete.\n");
            Cpu.PrintRegisters();
            Cpu.PrintMemory(0);
        }

        /// <summary>
        /// 2ns pass of assembly.
        /// Converts mnemonics to opcodes and evaluates expressions.
        /// </summary>
        /// <param name="sourceCode"></param>
        private void DoSecondPass(string sourceCode)
        {
            sourceLine = 0;
            locationCounter = 0;

            reader = new StringReader(sourceCode);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                sourceLine++;
                if (line == "") continue;
                //  Evaluate expressions
                line = EvaluateOperands(line);

                //  Process well-formed instructions
                bool processedLine = false;
                foreach (string s in OpcodeMap.Keys)
                {
                    Match match = Regex.Match(line, s);
                    if (match.Success && match.Length == line.Length)
                    {
                        if (OpcodeMap[s] > 0xffff)
                        {
                            //  3-byte instruction: prefix byte, prefix byte, (displacement byte - follows instruction), opcode
                            Memory[locationCounter++] = (OpcodeMap[s] >> 16) & 0xff;
                            Memory[locationCounter++] = (OpcodeMap[s] >> 8) & 0xff;
                            Memory[locationCounter + 1] = OpcodeMap[s] & 0xff;
                        }
                        else if (OpcodeMap[s] > 0xff)
                        {
                            //  2-byte instruction: prefix byte, opcode
                            Memory[locationCounter++] = (OpcodeMap[s] >> 8) & 0xff;
                            Memory[locationCounter++] = OpcodeMap[s] & 0xff;
                        }
                        else
                        {
                            //  1-byte instruction
                            Memory[locationCounter++] = OpcodeMap[s] & 0xff;
                        }

                        //  Named groups in the regex match indicate we need to process further bytes for this instruction :-
                        //  Displacement byte
                        if (match.Groups["d"].Value != "") EncodeDisplacementByte(match.Groups["d"].Value);
                        //  Label
                        if (match.Groups["e"].Value != "") EncodeRelativeLabel(match.Groups["e"].Value);
                        //  16-bit immediate data
                        if (match.Groups["nn"].Value != "") Encode16BitInteger(match.Groups["nn"].Value);
                        //  8-bit immediate data
                        if (match.Groups["n"].Value != "") Encode8BitInteger(match.Groups["n"].Value);
                        
                        //  If necessary, correct the LC for a 3-byte instruction
                        if (OpcodeMap[s] > 0xffff) locationCounter++;
                            
                        processedLine = true;
                        break;
                    }
                }
                //  Warn if no pattern found
                if (!processedLine) Console.WriteLine("Syntax Error on second pass!");
            }
            reader.Close();

            //  Success
            Console.WriteLine(locationCounter + " bytes successfully assembled.\n");

            PrintSymbolTable();
            Cpu.PrintRegisters();
            Cpu.PrintMemory(0);
        }

        /// <summary>
        /// 1st pass of Assembly.
        /// Builds symbol table.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns>the text output ready for the second pass</returns>
        private string DoFirstPass(string sourcePath)
        {
            SymbolTable.Clear();

            sourceLine = 0;
            locationCounter = 0;
            string output = "";

            using (StreamReader reader = new StreamReader(sourcePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    sourceLine++;

                    line = TrimLine(line);

                    if (line != "") line = ParseLabel(line);

                    string tempLine = PreParseOpcode(line);

                    //  Check for well-formed instruction
                    bool processedLine = false;
                    foreach (string s in OpcodeMap.Keys)
                    {
                        Match match = Regex.Match(tempLine, s);
                        if (match.Success && match.Length == tempLine.Length)
                        {
                            //  Account for various length instructions
                            if (OpcodeMap[s] > 0xffff) locationCounter += 3;
                            else if (OpcodeMap[s] > 0xff) locationCounter += 2;
                            else locationCounter++;

                            //  Account for immediate data
                            if (match.Groups["d"].Value != "") locationCounter++;
                            if (match.Groups["e"].Value != "") locationCounter++;
                            if (match.Groups["nn"].Value != "") locationCounter+= 2;
                            if (match.Groups["n"].Value != "") locationCounter++;

                            processedLine = true;
                            break;
                        }
                    }
                    //  Warn if no pattern found
                    if (line != "" && !processedLine) Console.WriteLine("Syntax Error: Line " + sourceLine);
                    output += line + "\n";
                }
            }
            return output;
        }

        /// <summary>
        /// Replaces expressions in opcodes with 0 for first pass evaluation.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string PreParseOpcode(string line)
        {
            if (line == "") return line;
            //  Recognise opcode
            int endToken = line.IndexOf(' ');
            if (endToken == -1) endToken = line.Length;
            string opcode = line.Substring(0, endToken);
            if (!Mnemonics.Contains(opcode))
            {
                //  Erroneous mnemonic
                Console.WriteLine("Syntax Error: Unknown mnemonic at line " + sourceLine);
            }

            //  Recognise operands
            string[] operands = new string[0];
            if (endToken != line.Length)
            {
                operands = line.Substring(endToken).Split(',');
            }

            //  Replace expressions with temp value for pattern matching
            for (int i = 0; i < operands.Length; i++)
            {
                operands[i] = operands[i].Trim();
                if (!Registers.Contains(operands[i]))
                {
                    operands[i] = "0";
                }
            }

            string tempLine = opcode;
            if (operands.Length > 0) tempLine += " " + operands[0];
            if (operands.Length > 1) tempLine += ", " + operands[1];
            return tempLine;
        }

        /// <summary>
        /// Adds a label to the symbol table with its address.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string ParseLabel(string line)
        {
            //  Check for labels
            int endToken = line.IndexOf(' ');
            if (endToken == -1) endToken = line.Length;

            string identifier = line.Substring(0, endToken);

            if (Registers.Contains(identifier))
            {
                //  Line must not start with a register
                Console.WriteLine("Syntax Error: line " + sourceLine);
            }
            if (!Mnemonics.Contains(identifier))
            {
                //  Label definition - add to symbol table and strip from line
                if (SymbolTable.ContainsKey(identifier))
                {
                    Console.WriteLine("Syntax Error: label already defined at line " + sourceLine);
                }
                SymbolTable.Add(identifier, locationCounter);
                endToken = line.IndexOf(' ');
                if (endToken == -1) return "";
                line = line.Substring(endToken);
            }
            return line.Trim();
        }

        /// <summary>
        /// Sanitises a line of input, removing tabs, spaces and comments
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string TrimLine(string line)
        {
            //  Replace tabs, colons & trim
            line = line.Replace('\t', ' ').Replace(":", "").Trim();

            //  Strip comments
            if (line.Contains(';'))
            {
                line = line.Substring(0, line.IndexOf(';'));
            }

            return line;
        }

        /// <summary>
        /// Evaluates operands.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string EvaluateOperands(string line)
        {
            //  If no operands then return
            if (!line.Contains(' ')) return line;

            //  Split opcode to its components
            string opcode = line.Substring(0, line.IndexOf(' '));
            string temp = line.Substring(line.IndexOf(' ') + 1);
            string[] operands;
            if (temp.Contains(','))
            {
                operands = temp.Split(',');
            } else {
                operands = new string[1];
                operands[0] = temp;
            }
            bool[] indAddress = new bool[operands.Length];

            //  Evaluate operands
            for (int i = 0; i < operands.Length; i++)
            {
                operands[i] = operands[i].Trim();

                //  Surrounding parentheses should not be tokenised as they indicate indirect addressing
                if (operands[i][0] == '(')
                {
                    operands[i] = operands[i].Substring(1, operands[i].Length - 2);
                    indAddress[i] = true;
                }

                //  Evaluate everything except a register or flag identifier
                if (!Registers.Contains(operands[i].ToString()))
                {
                    operands[i] = Eval.EvaluateExpression(operands[i], SymbolTable).ToString();
                }

                //  Replace any outer-parentheses
                if (indAddress[i])
                {
                    operands[i] = "(" + operands[i] + ")";
                }
            }

            //  Rebuild the opcode with expressions fully evaluated
            line = opcode + " " + operands[0];
            if (operands.Length > 1)
            {
                line += ", " + operands[1];
            }
            return line;
        }

        /// <summary>
        /// Prints the Symbol table.
        /// </summary>
        private void PrintSymbolTable()
        {
            Console.WriteLine("Symbols: ");
            foreach (string s in SymbolTable.Keys)
            {
                Console.WriteLine(SymbolTable[s] + " : " + s);
            }
        }

        /// <summary>
        /// Writes an 8-bit signed displacement byte to the memory at the current LC
        /// </summary>
        /// <param name="d"></param>
        private void EncodeDisplacementByte(string d)
        { 
            int value = int.Parse(d);

            if (value < -128 || value > 127)
            {
                Console.WriteLine("Syntax Error: displacement byte expected a 8-bit signed integer. Line " + sourceLine);
            }
            else
            {
                Memory[locationCounter++] = (byte)value;
            }
        }

        /// <summary>
        /// Records label references.
        /// </summary>
        /// <param name="p"></param>
        private void EncodeRelativeLabel(string label)
        {
            int loc = int.Parse(label);

            int offset = loc - locationCounter;
            if (offset < -128 || offset > 127)
            {
                Console.WriteLine("Syntax Error: Relative jump out of range. Line " + sourceLine);
            }
            else
            {
                Memory[locationCounter++] = (byte)offset;
            }
        }

        /// <summary>
        /// Writes a 8-bit value to the memory at the current LC.
        /// </summary>
        /// <param name="n"></param>
        private void Encode8BitInteger(string n)
        {
            int value = int.Parse(n);
            if (value < 0 || value > 255)
            {
                Console.WriteLine("Syntax Error: expected a 8-bit positive integer. Line " + sourceLine);
            }
            else
            {
                Memory[locationCounter++] = value;
            }
        }

        /// <summary>
        /// Writes a 16-bit value to the memory at the current LC. 
        /// </summary>
        /// <param name="p"></param>
        private void Encode16BitInteger(string nn)
        {
            int value = int.Parse(nn);
            if (value < 0 || value > 65535)
            {
                Console.WriteLine("Syntax Error: expected a 16-bit positive integer. Line " + sourceLine);
            }
            else
            {
                Memory[locationCounter++] = value & 0xFF;
                Memory[locationCounter++] = value >> 8 & 0xFF;
            }
        }

        /// <summary>
        /// Reads opcodes and corresponding regular expression patterns from .txt file.
        /// </summary>
        private void ReadOpcodeMap()
        {
            OpcodeMap = new Dictionary<string, int>();
            reader = new StreamReader("OpcodeMap.txt");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Equals("") || line.StartsWith("#"))
                {
                    continue;
                }
                int opcode = int.Parse(line.Substring(0, line.IndexOf(' ')), System.Globalization.NumberStyles.HexNumber);
                string pattern = line.Substring(line.IndexOf(' ') + 1);
                if (!OpcodeMap.ContainsKey(pattern))
                {
                    OpcodeMap.Add(pattern, opcode);
                }
            }
            reader.Close();
        }

        /// <summary>
        /// Initialises a list from .txt file.
        /// </summary>
        private void ReadListFromFile(string fileName, List<string> list)
        {
            using (reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Equals("") || line.StartsWith("#"))
                    {
                        continue;
                    }
                    list.Add(line);
                }
            }
        }
    }
}
