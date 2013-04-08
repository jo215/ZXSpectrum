using System;
using ZXSpectrum.Z_80;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ZXSpectrum;

namespace TestProject2
{
    public partial class Z80Test
    {
        /// <summary>
        ///A suite of tests for every opcode.
        ///Source: http://fuse-emulator.sourceforge.net/
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void FUSETests()
        {
            PrivateObject param0 = new PrivateObject(new Z80(new Memory48K()));
            Z80_Accessor target = new Z80_Accessor(param0);
            /*
                tests.in
                --------

                Each test has the format:

                <arbitrary test description>
                AF BC DE HL AF' BC' DE' HL' IX IY SP PC
                I R IFF1 IFF2 <halted> <tstates>

                <halted> specifies whether the Z80 is halted.
                <tstates> specifies the number of tstates to run the test for.

                Then followed by lines specifying the initial memory setup. Each has
                the format:

                <start address> <byte1> <byte2> ... -1

                eg

                1234 56 78 9a -1

                says to put 0x56 at 0x1234, 0x78 at 0x1235 and 0x9a at 0x1236.

                Finally, -1 to end the test. Blank lines may follow before the next test.
             
               
                tests.expected
                --------------

                Each test output starts with the test description, followed by a list
                of 'events': each has the format

                <time> <type> <address> <data>

                <time> is simply the time at which the event occurs.
                <type> is one of MR (memory read), MW (memory write), MC (memory
                       contend), PR (port read), PW (port write) or PC (port contend).
                <address> is the address (or IO port) affected.
                <data> is the byte written or read. Missing for contentions.

                After that, lines specifying AF, BC etc as for .in files. <tstates>
                now specifies the final time.

                After that, lines specifying which bits of memory have changed since
                the initial setup. Same format as for .in files.

                Why some specific tests are here
                ================================

                37_{1,2,3}: check the behaviour of SCF with respect to bits 3 and 5
	                    (bug fixed on 20040225).

                cb{4,5,6,7}{7,f}_1: designed to check that bits 3 and 5 are copied to
		                    F only for BIT 3,<arg> and BIT 5,<arg> respectively
		                    (bug fixed on 20040225).

		                    However, later research has revealed the bits 3
		                    and 5 are copied on all BIT instructions, so these
		                    tests are now essentially redundant.

                d{3,b}_{1,2,3}: check for correct port contention on IO in the four
	                        relevant states (port high byte in 0x40 to 0x7f or not,
		                port low bit set or reset).

                dd00.in, ddfd00.in: test timings of "extended NOP" opcodes DD 00 and
		                    DD FD 00; the extra 00 at the end is to check the
		                    next opcode executes at the right time (bug fixed
		                    on 20060722).
             
            */

            using (FileStream fs = File.Open(AppDomain.CurrentDomain.BaseDirectory + "\\tests.in", FileMode.Open, FileAccess.Read))
            {
                using (StreamReader testIn = new StreamReader(fs))
                {
                    using (FileStream fs2 = File.Open(AppDomain.CurrentDomain.BaseDirectory + "\\tests.expected", FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader testExpected = new StreamReader(fs2))
                        {
                            //  Read in test:
                            //for (int j = 0; j <128; j++)
                            while (!testIn.EndOfStream)
                            {
                                target.Reset();
                                target.Memory = new Memory48K();
                                //  Opcode being tested
                                string testName = testIn.ReadLine();
                                int opcode;

                                if (testName.Contains("_"))
                                {
                                    opcode = Convert.ToInt32(testName.Substring(0, testName.IndexOf('_')), 16);
                                }
                                else
                                {
                                    opcode = Convert.ToInt32(testName, 16);
                                }

                                //  Standard registers
                                string inputRegisters = testIn.ReadLine();
                                string[] regtemp = inputRegisters.Split(' ');
                                
                                target.A = Convert.ToInt32(regtemp[0].Substring(0, 2), 16);
                                target.SetFlags(Convert.ToInt32(regtemp[0].Substring(2, 2), 16));
                                target.B = Convert.ToInt32(regtemp[1].Substring(0, 2), 16);
                                target.C = Convert.ToInt32(regtemp[1].Substring(2, 2), 16);
                                target.D = Convert.ToInt32(regtemp[2].Substring(0, 2), 16);
                                target.E = Convert.ToInt32(regtemp[2].Substring(2, 2), 16);
                                target.H = Convert.ToInt32(regtemp[3].Substring(0, 2), 16);
                                target.L = Convert.ToInt32(regtemp[3].Substring(2, 2), 16);
                                target.A2 = Convert.ToInt32(regtemp[4].Substring(0, 2), 16);
                                target.SetShadowFlags(Convert.ToInt32(regtemp[4].Substring(2, 2), 16));
                                target.B2 = Convert.ToInt32(regtemp[5].Substring(0, 2), 16);
                                target.C2 = Convert.ToInt32(regtemp[5].Substring(2, 2), 16);
                                target.D2 = Convert.ToInt32(regtemp[6].Substring(0, 2), 16);
                                target.E2 = Convert.ToInt32(regtemp[6].Substring(2, 2), 16);
                                target.H2 = Convert.ToInt32(regtemp[7].Substring(0, 2), 16);
                                target.L2 = Convert.ToInt32(regtemp[7].Substring(2, 2), 16);
                                target.IXH = Convert.ToInt32(regtemp[8].Substring(0, 2), 16);
                                target.IXL = Convert.ToInt32(regtemp[8].Substring(2, 2), 16);
                                target.IYH = Convert.ToInt32(regtemp[9].Substring(0, 2), 16);
                                target.IYL = Convert.ToInt32(regtemp[9].Substring(2, 2), 16);
                                target.SP = Convert.ToInt32(regtemp[10], 16);
                                target.PC = Convert.ToInt32(regtemp[11], 16);

                                //  Special registers
                                regtemp = testIn.ReadLine().Split(' ');
                                target.I = Convert.ToInt32(regtemp[0], 16);
                                target.R = Convert.ToInt32(regtemp[1], 16);
                                target.IFF1 = Convert.ToBoolean(int.Parse(regtemp[2]));
                                target.IFF2 = Convert.ToBoolean(int.Parse(regtemp[3]));
                                target.isHalted = Convert.ToBoolean(int.Parse(regtemp[5]));
                                int numStates = int.Parse(regtemp[regtemp.Length - 1]);

                                //  Memory
                                string testInput = testIn.ReadLine();
                                while (!testInput.Equals("-1"))
                                {
                                    string[] memDef = testInput.Split(' ');
                                    Assert.IsTrue(memDef[memDef.Length - 1] == "-1");
                                    for (int i = 1; i < memDef.Length - 1; i++)
                                    {
                                        target.Memory[Convert.ToInt32(memDef[0], 16) + (i - 1)] = Convert.ToInt32(memDef[i], 16);
                                    }
                                    testInput = testIn.ReadLine();
                                }
                                //  Blank line
                                testIn.ReadLine();

                                //  Run the Z80 emulator for 1 instruction 
                                target.Run(false, numStates);

                                //  Read in expected results
                                Assert.IsTrue(testExpected.ReadLine().Equals(testName));

                                //  Ignore precise timing info for now
                                testInput = testExpected.ReadLine();
                                while (testInput.StartsWith(" "))
                                {
                                    testInput = testExpected.ReadLine();
                                }
                                
                                //  Registers
                                regtemp = testInput.Split(' ');
                                Assert.IsTrue(target.A == Convert.ToInt32(regtemp[0].Substring(0, 2), 16));
                                Assert.IsTrue(target.GetFlagsAsByte() == Convert.ToInt32(regtemp[0].Substring(2, 2), 16));
                                Assert.IsTrue(target.B == Convert.ToInt32(regtemp[1].Substring(0, 2), 16));
                                Assert.IsTrue(target.C == Convert.ToInt32(regtemp[1].Substring(2, 2), 16));
                                Assert.IsTrue(target.D == Convert.ToInt32(regtemp[2].Substring(0, 2), 16));
                                Assert.IsTrue(target.E == Convert.ToInt32(regtemp[2].Substring(2, 2), 16));
                                Assert.IsTrue(target.H == Convert.ToInt32(regtemp[3].Substring(0, 2), 16));
                                Assert.IsTrue(target.L == Convert.ToInt32(regtemp[3].Substring(2, 2), 16));
                                Assert.IsTrue(target.A2 == Convert.ToInt32(regtemp[4].Substring(0, 2), 16));
                                Assert.IsTrue(target.GetShadowFlagsAsByte() == Convert.ToInt32(regtemp[4].Substring(2, 2), 16));
                                Assert.IsTrue(target.B2 == Convert.ToInt32(regtemp[5].Substring(0, 2), 16));
                                Assert.IsTrue(target.C2 == Convert.ToInt32(regtemp[5].Substring(2, 2), 16));
                                Assert.IsTrue(target.D2 == Convert.ToInt32(regtemp[6].Substring(0, 2), 16));
                                Assert.IsTrue(target.E2 == Convert.ToInt32(regtemp[6].Substring(2, 2), 16));
                                Assert.IsTrue(target.H2 == Convert.ToInt32(regtemp[7].Substring(0, 2), 16));
                                Assert.IsTrue(target.L2 == Convert.ToInt32(regtemp[7].Substring(2, 2), 16));
                                Assert.IsTrue(target.IXH == Convert.ToInt32(regtemp[8].Substring(0, 2), 16));
                                Assert.IsTrue(target.IXL == Convert.ToInt32(regtemp[8].Substring(2, 2), 16));
                                Assert.IsTrue(target.IYH == Convert.ToInt32(regtemp[9].Substring(0, 2), 16));
                                Assert.IsTrue(target.IYL == Convert.ToInt32(regtemp[9].Substring(2, 2), 16));
                                Assert.IsTrue(target.SP == Convert.ToInt32(regtemp[10], 16));
                                Assert.IsTrue(opcode == 0 || target.PC == Convert.ToInt32(regtemp[11], 16));

                                //  Special registers
                                regtemp = testExpected.ReadLine().Split(' ');
                                Assert.IsTrue(target.I == Convert.ToInt32(regtemp[0], 16));
                                Assert.IsTrue(opcode == 0 || target.R == Convert.ToInt32(regtemp[1], 16));
                                Assert.IsTrue(target.IFF1 == Convert.ToBoolean(int.Parse(regtemp[2])));
                                Assert.IsTrue(target.IFF2 == Convert.ToBoolean(int.Parse(regtemp[3])));
                                Assert.IsTrue(target.isHalted == Convert.ToBoolean(int.Parse(regtemp[5])));
                                Assert.IsTrue(opcode == 0 || target.CycleTStates == int.Parse(regtemp[6]));

                                //  Memory
                                testInput = testExpected.ReadLine();
                                while (testInput.Length > 2)
                                {
                                    string[] memDef = testInput.Split(' ');
                                    Assert.IsTrue(memDef[memDef.Length - 1] == "-1");
                                    for (int i = 1; i < memDef.Length - 1; i++)
                                    {
                                        int val = Convert.ToInt32(memDef[i], 16);
                                        Assert.IsTrue(target.Memory[Convert.ToInt32(memDef[0], 16) + (i - 1)] == val);
                                    }
                                    testInput = testExpected.ReadLine();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
