using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXSpectrum.Z_80
{
    /// <summary>
    /// Utility class for evaluating expressions in string form.
    /// </summary>
    public static class Eval
    {

        /// <summary>
        /// Evaluates a standard mathematical expression
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="symbolTable"></param>
        /// <returns></returns>
        public static int EvaluateExpression(string expression, Dictionary<string,int> symbolTable)
        {
            //  Tokenise the expression
            List<string> tokens = Tokenise(expression, symbolTable);
            if (tokens.Count == 0) return 0;
            //  Convert infix to postfix using a stack
            Stack<string> stack = new Stack<string>();
            List<string> postfix = new List<string>();

            foreach (string token in tokens)
            {
                if (token == "(")
                {
                    stack.Push(token);
                }
                else if (token == ")")
                {
                    while (stack.Peek() != "(")
                    {
                        postfix.Add(stack.Pop());
                    }
                    stack.Pop();
                }
                else if (isOperator(token[0]))
                {
                    while (stack.Count != 0 && stack.Peek() != "(" && Precedence(token) <= Precedence(stack.Peek()))
                    {
                        postfix.Add(stack.Pop());
                    }
                    stack.Push(token);
                }
                else
                {
                    //  Operand
                    postfix.Add(token);
                }
            }
            while (stack.Count != 0)
            {
                postfix.Add(stack.Pop());
            }

            //  Evaluate postfix, again using the now-empty stack
            foreach (string token in postfix)
            {
                if (isOperator(token[0]))
                {
                    int second = int.Parse(stack.Pop());
                    int first = int.Parse(stack.Pop());
                    switch (token)
                    {
                        case "+": stack.Push((first + second).ToString()); break;
                        case "-": stack.Push((first - second).ToString()); break;
                        case "*": stack.Push((first * second).ToString()); break;
                        case "/": stack.Push((first / second).ToString()); break;
                    }
                }
                else
                {
                    //  Operand
                    stack.Push(token);
                }
            }
            return int.Parse(stack.Pop());
        }

        /// <summary>
        /// Gets the precedence of a mathematical operator.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static int Precedence(string p)
        {
            switch (p)
            {
                case "/": return 4;
                case "*": return 3;
                case "+": return 2;
                case "-": return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// Tokenises a expression string.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static List<string> Tokenise(string expression, Dictionary<string, int> symbolTable)
        {
            expression += "\n";
            List<string> tokens = new List<string>();
            string readChars;
            int lookAhead = 0;
            do
            {
                if (expression[lookAhead].Equals('\n'))
                {
                    break;
                }

                //  Ignore whitespace
                while (expression[lookAhead].Equals(' '))
                {
                    lookAhead++;
                }
                readChars = "";

                if (isDigit(expression[lookAhead]))
                {
                    //  Numbers
                    while (isDigitOrSuffix(expression[lookAhead]))
                    {
                        readChars += expression[lookAhead++];
                    }
                    tokens.Add(ParseValue(readChars).ToString());
                }
                else if (isOperator(expression[lookAhead]))
                {
                    //  Operators
                    readChars += expression[lookAhead++];
                    tokens.Add(readChars);
                }
                else if (expression[lookAhead] == '\'')
                {
                    //  Character literal
                    tokens.Add(((int)expression[++lookAhead]).ToString());
                    lookAhead += 2;
                }
                else
                {
                    //  Identifiers
                    while (!isOperator(expression[lookAhead]) && expression[lookAhead] != ' ' && expression[lookAhead] != '\n')
                    {
                        readChars += expression[lookAhead++];
                    }
                    if (symbolTable.ContainsKey(readChars))
                    {
                        tokens.Add(symbolTable[readChars].ToString());
                    }
                    else
                    {
                        Console.WriteLine("Syntax Error: Symbol not defined '" + readChars + "'");
                        return new List<string>();
                    }
                }

            } while (lookAhead <= expression.Length);


            return tokens;
        }

        /// <summary>
        /// Returns true if the character is a mathematical operator.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool isOperator(char p)
        {
            return p == '(' || p == ')' || p == '+' || p == '-' || p == '*' || p == '/';
        }

        /// <summary>
        /// Returns true if the character is a digit or number-base identifying suffix.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool isDigitOrSuffix(char p)
        {
            return isDigit(p) || p == 'B' || p == 'D' || p == 'H' || p == 'O' || p == 'Q';
        }

        /// <summary>
        /// Returns true if the character argument is a digit.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool isDigit(char p)
        {
            return (int)p > 47 && (int)p < 58;
        }

        /// <summary>
        /// Parses unsigned constants in binary, decimal, hexadecimal or octal form, as well as character literals. Returns -1 on error.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ParseValue(string value)
        {
            //  Check prefix
            switch (value[0])
            {
                case '\'':
                case '"':
                    //  ASCII Literal
                    return (int)value[1];
                case '#':
                case '$':
                    //  Hex value
                    return Convert.ToInt32(value.Substring(1), 16);
                case '%':
                    //  Binary value
                    return Convert.ToInt32(value.Substring(1, value.Length - 1), 2);
                default:
                    //  Check suffix
                    switch (value[value.Length - 1])
                    {
                        case 'B':
                            //  Binary
                            return Convert.ToInt32(value.Substring(0, value.Length - 1), 2);
                        case 'D':
                            //  Decimal
                            return Convert.ToInt32(value.Substring(0, value.Length - 1), 10);
                        case 'H':
                            //  Hexadecimal
                            //Console.WriteLine("KHLGL" + Convert.ToInt32(constant.Substring(0, constant.Length - 1), 16));
                            return Convert.ToInt32(value.Substring(0, value.Length - 1), 16);
                        case 'O':
                        case 'Q':
                            //  Octal
                            return Convert.ToInt32(value.Substring(0, value.Length - 1), 8);
                        default:
                            //  Decimal
                            int n;
                            if (int.TryParse(value, out n))
                            {
                                return n;
                            }
                            return -1;
                    }
            }
        }
    }
}
