using ZXSpectrum.Z_80;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TestProject2
{


    /// <summary>
    ///This is a test class for Z80Test and is intended
    ///to contain all Z80Test Unit Tests
    ///</summary>
    [TestClass()]
    public partial class Z80Test
    {

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for the Stack
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void StackTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(0, 0x1234);
            Assert.IsTrue(target.B == 0x12
                && target.C == 0x34);

            target.SP = 0x504;
            target.PUSH_qq(0);

            Assert.IsTrue(target.Memory[0x503] == 0x12
                    && target.Memory[0x502] == 0x34
                    && target.SP == 0x502);

            target.LD_dd_nn(0);
            target.POP_qq(0);
            Assert.IsTrue(target.B == 0x12
                && target.C == 0x34
                && target.SP == 0x504);

        }

        /// <summary>
        ///A test for the Carry/Half Carry and Zero Flags
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CarryTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 5;
            target.Memory[0] = 3;
            target.ADD_A_n();
            Assert.IsTrue((target.F & Flag.Carry) != Flag.Carry
                    && (target.F & Flag.HalfCarry) != Flag.HalfCarry);
            target.Memory[1] = 255;
            target.ADD_A_n();
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry
                    && (target.F & Flag.HalfCarry) == Flag.HalfCarry);
            target.Memory[2] = 248;
            target.ADC_A_n();
            Assert.IsTrue(target.A == 0
                && (target.F & Flag.Zero) == Flag.Zero);
        }

        /// <summary>
        ///A test for 8 bit additions
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void Arithmetic8BitTest()
        {
            //  8-bit Load group
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 64;
            target.B = 65;
            target.ADD_A_r(0);
            //  129  / -127
            Assert.IsTrue(target.A == 129
                && (target.F & Flag.Carry) != Flag.Carry
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);

            target.A = 255;
            target.B = 255;
            target.ADD_A_r(0);
            //  510 -> 254 / -2
            Assert.IsTrue(target.A == 254
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);

            target.A = 192;
            target.B = 191;
            target.ADD_A_r(0);
            //  383 -> 127 / -129 -> 127
            Assert.IsTrue(target.A == 127
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 6;
            target.B = 8;
            target.ADD_A_r(0);
            //  14 / 14
            Assert.IsTrue(target.A == 14
                && (target.F & Flag.Carry) != Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 127;
            target.B = 1;
            target.ADD_A_r(0);
            //  128 / 128 -> -128
            Assert.IsTrue(target.A == 128
                && (target.F & Flag.Carry) != Flag.Carry
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);

            target.A = 4;
            target.B = 254;
            target.ADD_A_r(0);
            //  258 -> 2 / -2 -> 2
            Assert.IsTrue(target.A == 2
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 254;
            target.B = 252;
            target.ADD_A_r(0);
            //  506 -> 250 / -6
            Assert.IsTrue(target.A == 250
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);

            target.A = 129;
            target.B = 194;
            target.ADD_A_r(0);
            //  323 -> 67 / -189 -> 67
            Assert.IsTrue(target.A == 67
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 254;
            target.B = 129;
            target.SUB_r(0);
            //  125 / 125
            Assert.IsTrue(target.A == 125
                && (target.F & Flag.Carry) != Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 196;
            target.B = 91;
            target.SUB_r(0);
            //  105 / -151 -> 105
            Assert.IsTrue(target.A == 105
                && (target.F & Flag.Carry) != Flag.Carry
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Sign) != Flag.Sign);

            target.A = 12;
            target.B = 60;
            target.SUB_r(0);
            //  -48 -> 208 / -48
            Assert.IsTrue(target.A == 208
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);

            target.A = 146;
            target.B = 231;
            target.SUB_r(0);
            //  -85 -> 171 / -341 -> -85
            Assert.IsTrue(target.A == 171
                && (target.F & Flag.Carry) == Flag.Carry
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && (target.F & Flag.Sign) == Flag.Sign);
        }

        /// <summary>
        ///A test for the Zero Flag
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ZeroTest()
        {
            //  8-bit Load group
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.I = 3;
            target.LD_A_I();
            Assert.IsTrue((target.F & Flag.Zero) != Flag.Zero);
            target.I = 0;
            target.LD_A_I();
            Assert.IsTrue((target.F & Flag.Zero) == Flag.Zero);
            target.R = 3;
            target.LD_A_R();
            Assert.IsTrue((target.F & Flag.Zero) != Flag.Zero);
            target.R = 0;
            target.LD_A_R();
            Assert.IsTrue((target.F & Flag.Zero) == Flag.Zero);
            //  Search Group
            target.A = 3;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x1000] = 3;
            target.CPI();
            Assert.IsTrue((target.F & Flag.Zero) == Flag.Zero);
            target.A = 4;
            target.CPI();
            Assert.IsTrue((target.F & Flag.Zero) != Flag.Zero);
        }

        /// <summary>
        ///A test for ADC_A_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADC_A_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x16;
            target.Set(Flag.Carry);
            target.Memory[0] = 0x10;
            target.ADC_A_n();
            Assert.IsTrue(target.A == 0x27, "Error: ADC A, n");
        }

        /// <summary>
        ///A test for ADC_A_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADC_A_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x16;
            target.Set(Flag.Carry);
            target.B = 0x10;
            target.ADC_A_r(0);
            Assert.IsTrue(target.A == 0x27, "Error: ADD A r");
        }

        /// <summary>
        ///A test for ADC_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADC_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(0, 0x16);
            target.Set(Flag.Carry);
            target.Set16BitRegisters(2, 0x10);
            target.ADC_HL(0);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x27, "Error: ADC HL");
        }

        /// <summary>
        ///A test for ADD_A_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADD_A_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x01;
            target.Memory[0] = 0x04;
            target.ADD_A_n();
            Assert.IsTrue(target.A == 0x05, "Error: ADD A, n");
        }

        /// <summary>
        ///A test for ADD_A_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADD_A_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x01;
            target.B = 0x04;
            target.ADD_A_r(0);
            Assert.IsTrue(target.A == 0x05, "Error: ADD A, r");
        }

        /// <summary>
        ///A test for ADD_HL_ss
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void ADD_HL_ssTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x4242);
            target.Set16BitRegisters(1, 0x1111);
            target.ADD_HL_ss(1);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x5353, "Error: ADD HL ss");
        }

        /// <summary>
        ///A test for AND_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void AND_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0] = 0x7b;
            target.A = 0xc3;
            target.AND_n();
            Assert.IsTrue(target.A == 0x43, "Error: AND n");
        }

        /// <summary>
        ///A test for AND_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void AND_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.B = 0x7b;
            target.A = 0xc3;
            target.AND_r(0);
            Assert.IsTrue(target.A == 0x43, "Error: AND r");
        }

        /// <summary>
        ///A test for BIT
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void BITTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.prefix = 0xcb;
            target.B = 0;
            target.BIT(2, 0);
            Assert.IsTrue((target.F & Flag.Zero) == Flag.Zero && target.B == 0, "Error: BIT b, r");
        }

        /// <summary>
        ///A test for CALL_cc_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CALL_cc_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Reset(Flag.Carry);
            target.PC = 0x1a48;
            target.SP = 0x3002;
            target.Memory[0x1a47] = 0xd4;
            target.Memory[0x1a48] = 0x35;
            target.Memory[0x1a49] = 0x21;
            target.CALL_cc_nn(2);
            Assert.IsTrue(target.Memory[0x3001] == 0x1a
                && target.Memory[0x3000] == 0x4a
                && target.SP == 0x3000
                && target.PC == 0x2135, "Error: CALL cc, nn");
        }

        /// <summary>
        ///A test for CALL_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CALL_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.PC = 0x1a48;
            target.Memory[0x1a48] = 0x35;
            target.Memory[0x1a49] = 0x21;
            target.SP = 0x3002;
            target.CALL_nn();
            Assert.IsTrue(target.Memory[0x3001] == 0x1a
            && target.Memory[0x3000] == 0x4a
            && target.SP == 0x3000
            && target.PC == 0x2135, "Error: CALL nn");
        }

        /// <summary>
        ///A test for CCF
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CCFTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set(Flag.Carry);
            target.CCF();
            Assert.IsTrue((target.F & Flag.Carry) != Flag.Carry, "Error: CCF");
        }

        /// <summary>
        ///A test for CPD
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CPDTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.Memory[0x1111] = 0x3b;
            target.A = 0x3b;
            target.Set16BitRegisters(0, 0x0001);
            target.CPD();
            Assert.IsTrue(target.Get16BitRegisters(0) == 0
                && target.Get16BitRegisters(2) == 0x1110
                && (target.F & Flag.Zero) == Flag.Zero
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && target.A == 0x3b
                && target.Memory[0x1111] == 0x3b, "Error CPD");
        }

        /// <summary>
        ///A test for CPDR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CPDRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1118);
            target.A = 0xf3;
            target.Set16BitRegisters(0, 0x0007);
            target.Memory[0x1118] = 0x52;
            target.Memory[0x1117] = 0x00;
            target.Memory[0x1116] = 0xf3;
            target.CPDR();
            target.CPDR();
            target.CPDR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1115
                && target.Get16BitRegisters(0) == 0x0004
            && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
            && (target.F & Flag.Zero) == Flag.Zero, "Error: CPDR");
        }

        /// <summary>
        ///A test for CPI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CPITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.Memory[0x1111] = 0x3b;
            target.A = 0x3b;
            target.Set16BitRegisters(0, 0x0001);
            target.CPI();
            Assert.IsTrue(target.Get16BitRegisters(0) == 0
                && target.Get16BitRegisters(2) == 0x1112
                && (target.F & Flag.Zero) == Flag.Zero
                && (target.F & Flag.ParityOverflow) != Flag.ParityOverflow
                && target.A == 0x3b
                && target.Memory[0x1111] == 0x3b, "Error: CPI");
        }

        /// <summary>
        ///A test for CPIR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CPIRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.A = 0xf3;
            target.Set16BitRegisters(0, 0x0007);
            target.Memory[0x1111] = 0x52;
            target.Memory[0x1112] = 0x00;
            target.Memory[0x1113] = 0xf3;
            target.CPIR();
            target.CPIR();
            target.CPIR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1114
                && target.Get16BitRegisters(0) == 0x0004
                && (target.F & Flag.ParityOverflow) == Flag.ParityOverflow
                && (target.F & Flag.Zero) == Flag.Zero, "Error: CPIR");
        }

        /// <summary>
        ///A test for CPL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CPLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0xb4;
            target.CPL();
            Assert.IsTrue(target.A == 0x4b, "Error: CPL");
        }

        /// <summary>
        ///A test for CP_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CP_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x63;
            target.Memory[0] = 0x60;
            target.CP_n();
            Assert.IsTrue((target.F & Flag.Zero) != Flag.Zero, "Error: CP n");
            target.Memory[1] = 0x63;
            target.CP_n();
            Assert.IsTrue((target.F & Flag.Zero) == Flag.Zero, "Error: CP n");
        }

        /// <summary>
        ///A test for CP_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void CP_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x63;
            target.Set16BitRegisters(2, 0x6000);
            target.Memory[0x6000] = 0x60;
            target.CP_r(6);
            Assert.IsTrue((target.F & Flag.Zero) != Flag.Zero, "Error: CP r");
        }

        /// <summary>
        ///A test for DAA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void DAATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x15;
            target.B = 0x27;
            target.ADD_A_r(0);
            target.DAA();
            Assert.IsTrue(target.A == 0x42, "Error: DAA");
        }

        /// <summary>
        ///A test for DEC_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void DEC_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.D = 0x2a;
            target.DEC_r(2);
            Assert.IsTrue(target.D == 0x29, "Error: DEC r");
        }

        /// <summary>
        ///A test for DEC_ss
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void DEC_ssTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1001);
            target.DEC_ss(2);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1000, "Error: DEC ss");
        }

        /// <summary>
        ///A test for DI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void DITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.IFF1 = true;
            target.IFF2 = true;
            target.DI();
            Assert.IsTrue(target.IFF1 == false && target.IFF2 == false, "Error: DI");
        }

        /// <summary>
        ///A test for DNJZ
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void DNJZTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);

            target.DJNZ();
            /// Hard to fully test this instruction in isolation
            /// 
        }

        /// <summary>
        ///A test for EI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void EITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.IFF1 = false;
            target.IFF2 = false;
            target.EI();
            Assert.IsTrue(target.IFF1 == true && target.IFF2 == true, "Error: EI");
        }

        /// <summary>
        ///A test for EXX
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void EXXTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(0, 0x0988);
            target.Set16BitRegisters(1, 0x9300);
            target.Set16BitRegisters(2, 0x00e7);
            target.EXX();
            target.Set16BitRegisters(0, 0x445a);
            target.Set16BitRegisters(1, 0x3da2);
            target.Set16BitRegisters(2, 0x8859);
            target.EXX();
            Assert.IsTrue(target.Get16BitRegisters(0) == 0x0988
                && target.Get16BitRegisters(1) == 0x9300
                && target.Get16BitRegisters(2) == 0x00e7, "Error: EXX");
        }

        /// <summary>
        ///A test for EX_AF_AF2
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void EX_AF_AF2Test()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x99;
            target.F = (target.F & 0x00);
            target.A2 = 0x59;
            target.F2 = Flag.Carry & Flag.HalfCarry & Flag.F3 & Flag.F5 & Flag.ParityOverflow & Flag.Sign & Flag.Subtract & Flag.Zero;
            target.EX_AF_AF2();
            Assert.IsTrue(target.A == 0x59
                && target.F == (Flag.Carry & Flag.HalfCarry & Flag.F3 & Flag.F5 & Flag.ParityOverflow & Flag.Sign & Flag.Subtract & Flag.Zero)
                && target.A2 == 0x99
                && target.F2 == 0, "Error: EX AF, AF'");
        }

        /// <summary>
        ///A test for EX_DE_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void EX_DE_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(1, 0x2822);
            target.Set16BitRegisters(2, 0x499a);
            target.EX_DE_HL();
            Assert.IsTrue(target.Get16BitRegisters(1) == 0x499a
                && target.Get16BitRegisters(2) == 0x2822, "Error: EX DE, HL");
        }

        /// <summary>
        ///A test for EX_SP_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void EX_SP_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x7012);
            target.SP = 0x8856;
            target.Memory[0x8856] = 0x11;
            target.Memory[0x8857] = 0x22;
            target.EX_SP_HL();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x2211
                && target.Memory[0x8856] == 0x12
                && target.Memory[0x8857] == 0x70
                && target.SP == 0x8856, "Error: EX (SP), HL");
        }

        /// <summary>
        ///A test for HALT
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void HALTTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.isHalted = false;
            target.HALT();
            Assert.IsTrue(target.isHalted == true, "Error: HALT");
        }

        /// <summary>
        ///A test for IM
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void IMTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.IM(2);
            Assert.IsTrue(target.interruptMode == 1, "Error: IM");
        }

        /// <summary>
        ///A test for INC_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INC_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.D = 0x28;
            target.INC_r(2);
            Assert.IsTrue(target.D == 0x29, "Error: INC r");
        }

        /// <summary>
        ///A test for INC_ss
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INC_ssTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1000);
            target.INC_ss(2);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1001, "Error: INC ss");
        }

        /// <summary>
        ///A test for IND
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INDTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.C = 0x07;
            target.B = 0x10;
            target.Set16BitRegisters(2, 0x1000);

            target.IND();
            Assert.IsTrue(target.Memory[0x1000] == 0xc1
                && target.Get16BitRegisters(2) == 0x0fff
                && target.B == 0x0f, "Error: IND");
        }

        /// <summary>
        ///A test for INDR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INDRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.C = 0x07;
            target.B = 0x03;
            target.Set16BitRegisters(2, 0x1000);
            target.INDR();
            target.INDR();
            target.INDR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x0ffd
                && target.B == 0
                && target.Memory[0x0ffe] == 0xc1
                && target.Memory[0x0fff] == 0xc1
                && target.Memory[0x1000] == 0xc1, "Error: INDR");
        }

        /// <summary>
        ///A test for INI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.C = 0x07;
            target.B = 0x10;
            target.Set16BitRegisters(2, 0x1000);
            target.INI();
            Assert.IsTrue(target.Memory[0x1000] == 0xc1
                && target.Get16BitRegisters(2) == 0x1001
                && target.B == 0x0f, "Error: INI");
        }

        /// <summary>
        ///A test for INIR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void INIRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.C = 0x07;
            target.B = 0x03;
            target.Set16BitRegisters(2, 0x1000);
            target.INIR();
            target.INIR();
            target.INIR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1003
                && target.B == 0
                && target.Memory[0x1000] == 0xc1
                && target.Memory[0x1001] == 0xc1
                && target.Memory[0x1002] == 0xc1, "Error: INIR");
        }

        /// <summary>
        ///A test for IN_A_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void IN_A_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x23;
            target.Memory[0] = 0x01;
            target.IN_A_n();
            Assert.IsTrue(target.A == 0xc1, "Error: IN A, n");
        }

        /// <summary>
        ///A test for IN_r_C
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void IN_r_CTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.C = 0x07;
            target.B = 0x10;
            target.IN_r_C(2);
            Assert.IsTrue(target.D == 0xc1, "Error: IN r, C");
        }

        /// <summary>
        ///A test for JP_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void JP_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.PC = 0x1000;
            target.Set16BitRegisters(2, 0x4800);
            target.JP_HL();
            Assert.IsTrue(target.PC == 0x4800, "Error: JP (HL)");
        }

        /// <summary>
        ///A test for JP_cc_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void JP_cc_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set(Flag.Carry);
            target.Memory[0x1520] = 0x03;
            target.Memory[0] = 0x20;
            target.Memory[1] = 0x15;
            target.JP_cc_nn(3);
            Assert.IsTrue(target.PC == 0x1520, "Error: JP cc, nn");
        }

        /// <summary>
        ///A test for JP_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void JP_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0] = 0x34;
            target.Memory[1] = 0x45;
            target.JP_nn();
            Assert.IsTrue(target.PC == 0x4534, "Error: JP nn");
        }

        /// <summary>
        ///A test for JR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void JRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.PC = 481;
            target.Memory[481] = 3;
            target.JR(-1);
            Assert.IsTrue(target.PC == 485, "Error JR e");
        }

        /// <summary>
        ///A test for LDD
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LDDTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.Memory[0x1111] = 0x88;
            target.Set16BitRegisters(1, 0x2222);
            target.Memory[0x2222] = 0x66;
            target.Set16BitRegisters(0, 0x7);
            target.LDD();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1110
                && target.Memory[0x1111] == 0x88
                && target.Get16BitRegisters(1) == 0x2221
                && target.Memory[0x2222] == 0x88
                && target.Get16BitRegisters(0) == 0x6, "Error: LDD");
        }

        /// <summary>
        ///A test for LDDR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LDDRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1114);
            target.Set16BitRegisters(1, 0x2225);
            target.Set16BitRegisters(0, 0x0003);
            target.Memory[0x1114] = 0xa5;
            target.Memory[0x1113] = 0x36;
            target.Memory[0x1112] = 0x88;
            target.Memory[0x2225] = 0xc5;
            target.Memory[0x2224] = 0x59;
            target.Memory[0x2223] = 0x66;
            target.LDDR();
            target.LDDR();
            target.LDDR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1111
                && target.Get16BitRegisters(1) == 0x2222
                && target.Get16BitRegisters(0) == 0x0000
                && target.Memory[0x1114] == 0xa5
                && target.Memory[0x1113] == 0x36
                && target.Memory[0x1112] == 0x88
                && target.Memory[0x2225] == 0xa5
                && target.Memory[0x2224] == 0x36
                && target.Memory[0x2223] == 0x88, "Error: LDDR");
        }

        /// <summary>
        ///A test for LDI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LDITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.Memory[0x1111] = 0x88;
            target.Set16BitRegisters(1, 0x2222);
            target.Memory[0x2222] = 0x66;
            target.Set16BitRegisters(0, 0x7);
            target.LDI();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1112
                && target.Memory[0x1111] == 0x88
                && target.Get16BitRegisters(1) == 0x2223
                && target.Memory[0x2222] == 0x88
                && target.Get16BitRegisters(0) == 0x6, "Error: LDI");
        }

        /// <summary>
        ///A test for LDIR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LDIRTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x1111);
            target.Set16BitRegisters(1, 0x2222);
            target.Set16BitRegisters(0, 0x0003);
            target.Memory[0x1111] = 0x88;
            target.Memory[0x1112] = 0x36;
            target.Memory[0x1113] = 0xa5;
            target.Memory[0x2222] = 0x66;
            target.Memory[0x2223] = 0x59;
            target.Memory[0x2224] = 0xc5;
            target.LDIR();
            target.LDIR();
            target.LDIR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1114
                && target.Get16BitRegisters(1) == 0x2225
                && target.Get16BitRegisters(0) == 0x0000
                && target.Memory[0x1111] == 0x88
                && target.Memory[0x1112] == 0x36
                && target.Memory[0x1113] == 0xa5
                && target.Memory[0x2222] == 0x88
                && target.Memory[0x2223] == 0x36
                && target.Memory[0x2224] == 0xa5, "Error: LDIR");
        }

        /// <summary>
        ///A test for LD_A_BC
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_A_BCTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the BC register pair contains the number 4747H, and memory address 
            //  4747H contains byte 12H, then the instruction LD A, (BC) results in byte 
            //  12H in register A.
            target.Reset();
            target.Set16BitRegisters(0, 0x4747);
            target.Memory[0x4747] = 0x12;
            target.LD_A_BC();
            Assert.IsTrue(target.A == 0x12, "Error: LD A, (BC)");
        }

        /// <summary>
        ///A test for LD_A_DE
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_A_DETest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the DE register pair contains the number 30A2H and memory address 
            //  30A2H contains byte 22H, then the instruction LD A, (DE) results in byte 
            //  22H in register A.
            target.Reset();
            target.Set16BitRegisters(1, 0x30a2);
            target.Memory[0x30a2] = 0x22;
            target.LD_A_DE();
            Assert.IsTrue(target.A == 0x22, "Error: LD A, (DE)");
        }

        /// <summary>
        ///A test for LD_A_I
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_A_ITest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.I = 126;
            target.LD_A_I();
            Assert.IsTrue(target.A == 126);
        }

        /// <summary>
        ///A test for LD_A_R
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_A_RTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.R = 129;
            target.LD_A_R();
            Assert.IsTrue(target.A == 131);
        }

        /// <summary>
        ///A test for LD_A_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_A_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the contents of nn is number 8832H, and the content of memory address 
            //  8832H is byte 04H, at instruction LD A, (nn) byte 04H is in the 
            //  Accumulator.
            target.Reset();
            target.Memory[0] = 0x32;
            target.Memory[1] = 0x88;
            target.Memory[0x8832] = 0x04;
            target.LD_A_nn();
            Assert.IsTrue(target.A == 0x04, "Error: LD A, (nn)");

        }

        /// <summary>
        ///A test for LD_BC_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_BC_ATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the Accumulator contains 7AH and the BC register pair contains 1212H
            //  the instruction LD (BC), A results in 7AH in memory location 1212H.
            target.Reset();
            target.A = 0x7a;
            target.Set16BitRegisters(0, 0x1212);
            target.LD_BC_A();
            Assert.IsTrue(target.Memory[0x1212] == 0x7a, "Error: LD (BC), A");
        }

        /// <summary>
        ///A test for LD_DE_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_DE_ATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the contents of register pair DE are 1128H, and the Accumulator contains 
            //  byte A0H, the instruction LD (DE), A results in A0H in memory location 
            //  1128H.
            target.Reset();
            target.Set16BitRegisters(1, 0x1128);
            target.A = 0xa0;
            target.LD_DE_A();
            Assert.IsTrue(target.Memory[0x1128] == 0xa0, "Error: LD (DE), A");
        }

        /// <summary>
        ///A test for LD_HL_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_HL_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0x4545] = 0x37;
            target.Memory[0x4546] = 0xa1;
            target.Memory[0] = 0x45;
            target.Memory[1] = 0x45;
            target.LD_HL_nn();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0xa137, "Error: LD HL, (nn)");
        }

        /// <summary>
        ///A test for LD_I_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_I_ATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 100;
            target.LD_I_A();
            Assert.IsTrue(target.I == 100, "Error: LD I, A");
        }

        /// <summary>
        ///A test for LD_R_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_R_ATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 24;
            target.LD_R_A();
            Assert.IsTrue(target.R == 22, "Error: LD R, A");
        }

        /// <summary>
        ///A test for LD_SP_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_SP_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x442e);
            target.LD_SP_HL();
            Assert.IsTrue(target.SP == 0x442e, "Error: LD SP, HL");
        }

        /// <summary>
        ///A test for LD_dd_nn
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_dd_nnTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0] = 0x00;
            target.Memory[1] = 0x50;
            target.LD_dd_nn(2);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x5000, "Error: LD dd, nn");
        }

        /// <summary>
        ///A test for LD_dd_nn2
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_dd_nn2Test()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0] = 0x30;
            target.Memory[1] = 0x21;
            target.Memory[0x2130] = 0x65;
            target.Memory[0x2131] = 0x78;
            target.LD_dd_nn2(0);
            Assert.IsTrue(target.Get16BitRegisters(0) == 0x7865, "Error: LD dd, (nn)");
        }

        /// <summary>
        ///A test for LD_nn_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_nn_ATest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the contents of the Accumulator are byte D7H, at execution of
            //  LD (3141H), AD 7H results in memory location 3141H.
            target.A = 0xd7;
            target.Memory[0] = 0x41;
            target.Memory[1] = 0x31;
            target.LD_nn_A();
            Assert.IsTrue(target.Memory[0x3141] == 0xd7, "Error: LD (nn), A");
        }

        /// <summary>
        ///A test for LD_nn_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_nn_HLTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(2, 0x483a);
            target.Memory[0] = 0x29;
            target.Memory[1] = 0xb2;
            target.LD_nn_HL();
            Assert.IsTrue(target.Memory[0xb229] == 0x3a
                && target.Memory[0xb22a] == 0x48, "Error: LD (nn), HL");
        }

        /// <summary>
        ///A test for LD_nn_dd
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_nn_ddTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Set16BitRegisters(0, 0x4644);
            target.Memory[0] = 0x00;
            target.Memory[1] = 0x10;
            target.LD_nn_dd(0);
            Assert.IsTrue(target.Memory[0x1000] == 0x44
                && target.Memory[0x1001] == 0x46, "Error: LD (nn), dd");
        }

        /// <summary>
        ///A test for LD_r_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_r_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  At execution of LD E, A5H the contents of register E are A5H.
            target.Memory[0] = 0xa5;
            target.LD_r_n(4);
            Assert.IsTrue(target.GetRegister(4) == 0xa5, "Error: LD E, n");
            //  If the HL register pair contains 4444H, the instructionLD (HL), 28H 
            //  results in the memory location 4444Hcontaining byte 28H.
            target.Reset();
            target.Set16BitRegisters(2, 0x4444);
            target.Memory[0] = 0x28;
            target.LD_r_n(6);
            Assert.IsTrue(target.Memory[0x4444] == 0x28, "Error: LD (HL), n");
            //  If the Index Register IX contains the number 219AH, the instruction
            //  LD (IX+5H), 5AH results in byte 5AHin the memory address 219AH.
            target.Reset();
            target.prefix = 0xdd;
            target.Set16BitRegisters(2, 0x219a);
            target.Memory[0] = 0x5;
            target.Memory[1] = 0x5a;
            target.LD_r_n(6);
            Assert.IsTrue(target.Memory[0x219a] != 0x5a, "Error: LD (IX+d), n");
            //  If the Index Register IY contains the number A940H, the instruction
            //  LD (IY+10H), 97H results in byte 97Hin memory location A940H.
            target.Reset();
            target.prefix = 0xfd;
            target.Set16BitRegisters(2, 0xa940);
            target.Memory[0] = 0x10;
            target.Memory[1] = 0x97;
            target.LD_r_n(6);
            Assert.IsTrue(target.Memory[0xa940] != 0x97, "Error: LD (IY+d), n");
        }

        /// <summary>
        ///A test for LD_r_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void LD_r_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            //  If the H register contains the number 8AH, and the E register contains 10H, 
            //  the instruction LD H, E results in both registers containing 10H.
            target.H = 0x8a;
            target.E = 0x10;
            target.LD_r_r(4, 3);
            Assert.IsTrue(target.GetRegister(4) == 0x10 && target.GetRegister(3) == 0x10, "Error: LD r, r");
            //  If register pair HL contains the number 75A1H, and memory address 
            //  75A1H contains byte 58H, the execution of LD C, (HL) results in 58H in 
            //  register C.
            target.Reset();
            target.Set16BitRegisters(2, 0x75a1);
            target.Memory[0x75a1] = 0x58;
            target.LD_r_r(1, 6);
            Assert.IsTrue(target.GetRegister(1) == 0x58, "Error: LD r, (HL)");
            //  If the Index Register IX contains the number 25AFH, the instruction LD B, 
            //  (IX+19H) causes the calculation of the sum 25AFH + 19H, which points 
            //  to memory location 25C8H. If this address contains byte 39H, the 
            //  instruction results in register B also containing 39H.
            target.Reset();
            target.prefix = 0xDD;
            target.Set16BitRegisters(2, 0x25AF);
            target.Memory[0] = 0x19;
            target.Memory[0x25C8] = 0x39;
            target.LD_r_r(0, 6);
            Assert.IsTrue(target.GetRegister(0) == 0x39, "Error: LD r, (IX+d)");
            //  If the Index Register IY contains the number 25AFH, the instruction 
            //  LD B, (IY+19H) causes the calculation of the sum 25AFH + 19H, which 
            //  points to memory location 25C8H. If this address contains byte 39H, the 
            //  instruction results in register B also containing 39H.
            target.Reset();
            target.prefix = 0xFD;
            target.Set16BitRegisters(2, 0x25AF);
            target.Memory[0] = 0x19;
            target.Memory[0x25C8] = 0x39;
            target.LD_r_r(0, 6);
            Assert.IsTrue(target.GetRegister(0) == 0x39, "Error: LD r, (IY+d)");
            //  If the contents of register pair HL specifies memory location 2146H, and 
            //  the B register contains byte 29H, at execution of LD (HL), B memory 
            //  address 2146H also contains 29H.
            target.Reset();
            target.Set16BitRegisters(2, 0x2146);
            target.B = 0x29;
            target.LD_r_r(6, 0);
            Assert.IsTrue(target.Memory[0x2146] == 0x29, "Error: LD (HL), r");
            //  If the C register contains byte 1CH, and the Index Register IX contains 
            //  3100H, then the instruction LID (IX+6H), C performs the sum 3100H+ 
            //  6H and loads 1CH to memory location 3106H.
            target.Reset();
            target.prefix = 0xDD;
            target.C = 0x1C;
            target.Set16BitRegisters(2, 0x3100);
            target.Memory[0] = 0x6;
            target.LD_r_r(6, 1);
            Assert.IsTrue(target.Memory[0x3106] == 0x1C, "Error: LD (IX+d), r");
            //  If the C register contains byte 48H, and the Index Register IY contains 
            //  2A11H, then the instruction LD (IY+4H), C performs the sum 2A11H+ 
            //  4H, and loads 48Hto memory location 2A15.
            target.Reset();
            target.prefix = 0xFD;
            target.C = 0x48;
            target.Set16BitRegisters(2, 0x2A11);
            target.Memory[0] = 0x4;
            target.LD_r_r(6, 1);
            Assert.IsTrue(target.Memory[0x2A15] == 0x48, "Error: LD (IY+d), r");
        }

        /// <summary>
        ///A test for NEG
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void NEGTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.A = 0x98;
            target.NEG();
            Assert.IsTrue(target.A == 0x68, "Error: NEG");
        }

        /// <summary>
        ///A test for OR_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OR_nTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.Memory[0] = 0x48;
            target.A = 0x12;
            target.OR_n();
            Assert.IsTrue(target.A == 0x5a, "Error: OR n");
        }

        /// <summary>
        ///A test for OR_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OR_rTest()
        {
            PrivateObject param0 = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(param0);
            target.H = 0x48;
            target.A = 0x12;
            target.OR_r(4);
            Assert.IsTrue(target.A == 0x5a, "Error: OR r");
        }

        /// <summary>
        ///A test for OTDR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OTDRTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.C = 0x07;
            target.B = 0x03;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x0ffe] = 0x51;
            target.Memory[0x0fff] = 0xa9;
            target.Memory[0x1000] = 0x03;
            //  Simulate PC-=2 (repeat instruction)
            target.OTDR();
            target.OTDR();
            target.OTDR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x0ffd
                    && target.B == 0, "Error:OTDR");
        }

        /// <summary>
        ///A test for OTIR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OTIRTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.C = 0x07;
            target.B = 0x03;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x1000] = 0x51;
            target.Memory[0x1001] = 0xa9;
            target.Memory[0x1002] = 0x03;
            //  Simulate PC-=2 (repeat instruction)
            target.OTIR();
            target.OTIR();
            target.OTIR();
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x1003 && target.B == 0, "Error: OTIR");
        }

        /// <summary>
        ///A test for OUTD
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OUTDTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.C = 0x07;
            target.B = 0x10;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x1000] = 0x59;
            target.OUTD();

            Assert.IsTrue(target.B == 0x0f && target.Get16BitRegisters(2) == 0x0fff, "Error: OUTD");
        }

        /// <summary>
        ///A test for OUTI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OUTITest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.C = 0x07;
            target.B = 0x10;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x1000] = 0x59;
            target.OUTI();
            Assert.IsTrue(target.B == 0x0f && target.Get16BitRegisters(2) == 0x1001, "Error: OUTI");
        }

        /// <summary>
        ///A test for OUT_C_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OUT_C_rTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.C = 0x01;
            target.D = 0x5a;
            target.OUT_C_r(3);
        }

        /// <summary>
        ///A test for OUT_n_A
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void OUT_n_ATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x23;
            target.Memory[0] = 0x01;
            target.OUT_n_A();
        }

        /// <summary>
        ///A test for POP_qq
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void POP_qqTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.SP = 0x1000;
            target.Memory[0x1000] = 0x55;
            target.Memory[0x1001] = 0x33;
            target.POP_qq(2);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x3355 && target.SP == 0x1002, "Error: POP qq");
        }

        /// <summary>
        ///A test for PUSH_qq
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void PUSH_qqTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x22;
            target.SetFlags(0x33);
            target.SP = 0x1007;
            target.PUSH_qq(3);
            Assert.IsTrue(target.Memory[0x1006] == 0x22 && target.Memory[0x1005] == 0x33 && target.SP == 0x1005, "Error: PUSH qq");
        }

        /// <summary>
        ///A test for RES
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RESTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.D = 255;
            target.RES(6, 2);
            Assert.IsTrue(target.D == 255 - 64, "Error: RES b, rn");
        }

        /// <summary>
        ///A test for RET
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RETTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.PC = 0x3535;
            target.SP = 0x2000;
            target.Memory[0x2000] = 0xb5;
            target.Memory[0x2001] = 0x18;
            target.RET();
            Assert.IsTrue(target.SP == 0x2002 && target.PC == 0x18b5, "Error: RET");
        }

        /// <summary>
        ///A test for RETI
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RETITest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.SP = 0x2000;
            target.RETI();
        }

        /// <summary>
        ///A test for RETN
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RETNTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.SP = 0x1000;
            target.PC = 0x1a45;

            target.RETN();
        }

        /// <summary>
        ///A test for RET_cc
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RET_ccTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set(Flag.Sign);
            target.PC = 0x3535;
            target.SP = 0x2000;
            target.Memory[0x2000] = 0xb5;
            target.Memory[0x2001] = 0x18;
            target.RET_cc(7);   //  RET M
            Assert.IsTrue(target.SP == 0x2002 && target.PC == 0x18b5, "Error: RET cc");
        }

        /// <summary>
        ///A test for RL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RLTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Reset(Flag.Carry);
            target.D = 128 + 15;
            target.RL(2);
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry && target.D == 30, "Error: RL");
        }

        /// <summary>
        ///A test for RLA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RLATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set(Flag.Carry);
            target.A = 118;
            target.RLA();
            Assert.IsTrue((target.F & Flag.Carry) != Flag.Carry && target.A == 237, "Error: RLA");
        }

        /// <summary>
        ///A test for RLC
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RLCTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set16BitRegisters(2, 0x2828);
            target.Memory[0x2828] = 0x88;
            target.RLC(6);
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry && target.Memory[0x2828] == 0x11, "Error: RLC(HL)");
        }

        /// <summary>
        ///A test for RLCA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RLCATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x88;
            target.RLCA();
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry && target.A == 0x11, "Error: RLCA");
        }

        /// <summary>
        ///A test for RLD
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RLDTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set16BitRegisters(2, 0x5000);
            target.A = 0x7a;
            target.Memory[0x5000] = 0x31;
            target.RLD();
            Assert.IsTrue(target.A == 0x73 && target.Memory[0x5000] == 0x1a, "Error: RLD");
        }

        /// <summary>
        ///A test for RR
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RRTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set16BitRegisters(2, 0x4343);
            target.Memory[0x4343] = 0xdd;
            target.Reset(Flag.Carry);
            target.RR(6);
            Assert.IsTrue(target.Memory[0x4343] == 0x6e && (target.F & Flag.Carry) == Flag.Carry, "Error: RR");
        }

        /// <summary>
        ///A test for RRA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RRATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0xe1;
            target.Reset(Flag.Carry);
            target.RRA();
            Assert.IsTrue(target.A == 0x70 && (target.F & Flag.Carry) == Flag.Carry, "Error: RRA");
        }

        /// <summary>
        ///A test for RRC
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RRCTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x31;
            target.RRC(7);
            Assert.IsTrue(target.A == 0x98 && (target.F & Flag.Carry) == Flag.Carry, "Error: RRC");
        }

        /// <summary>
        ///A test for RRCA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RRCATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x11;
            target.RRCA();
            Assert.IsTrue(target.A == 0x88 && (target.F & Flag.Carry) == Flag.Carry, "Error: RRCA");
        }

        /// <summary>
        ///A test for RRD
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RRDTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set16BitRegisters(2, 0x5000);
            target.A = 0x84;
            target.Memory[0x5000] = 0x20;
            target.RRD();
            Assert.IsTrue(target.A == 128 && target.Memory[0x5000] == 0x42, "Error: RRD");
        }

        /// <summary>
        ///A test for RST
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void RSTTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.PC = 0x15b3;
            target.RST(0x18);
            Assert.IsTrue(target.PC == 0x0018, "Error: RST");
        }

        /// <summary>
        ///A test for SBC_A_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SBC_A_nTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Memory[0] = 0x05;
            target.A = 0x16;
            target.Set(Flag.Carry);
            target.SBC_A_n();
            Assert.IsTrue(target.A == 0x10, "Error: SBC A n");
        }

        /// <summary>
        ///A test for SBC_A_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SBC_A_rTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x16;
            target.Set(Flag.Carry);
            target.Set16BitRegisters(2, 0x3433);
            target.Memory[0x3433] = 0x05;
            target.SBC_A_r(6);
            Assert.IsTrue(target.A == 0x10, "Error: SBC A r");
        }

        /// <summary>
        ///A test for SBC_HL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SBC_HLTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Set16BitRegisters(2, 0x9999);
            target.Set16BitRegisters(1, 0x1111);
            target.Set(Flag.Carry);
            target.SBC_HL(1);
            Assert.IsTrue(target.Get16BitRegisters(2) == 0x8887, "Error: SBC HL, ss");
        }

        /// <summary>
        ///A test for SCF
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SCFTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.Reset(Flag.Carry);
            target.SCF();
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry, "Error: SCF");
        }

        /// <summary>
        ///A test for SET
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SETTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0;
            target.SET(7, 7);
            Assert.IsTrue(target.A == 128, "Error: SET b, r");
        }

        /// <summary>
        ///A test for SLA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SLATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.L = 0xb1;
            target.SLA(5);
            Assert.IsTrue((target.F & Flag.Carry) == Flag.Carry && target.L == 0x62, "Error: SLA");
        }

        /// <summary>
        ///A test for SLL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SLLTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.B = 0x01;
            target.SLL(0);
            Assert.IsTrue(target.B == 0x03, "Error: SLL");
        }

        /// <summary>
        ///A test for SRA
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SRATest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.prefix = 0xdd;
            target.Set16BitRegisters(2, 0x1000);
            target.Memory[0x1003] = 0xb8;
            target.displacement = 3;
            target.SRA(6);
            Assert.IsTrue(target.Memory[0x1003] == 0xdc && (target.F & Flag.Carry) != Flag.Carry, "Error: SRA");
        }

        /// <summary>
        ///A test for SRL
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SRLTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.B = 0x8f;
            target.SRL(0);
            Assert.IsTrue(target.B == 0x47 && (target.F & Flag.Carry) == Flag.Carry, "Error: SRL");
        }

        /// <summary>
        ///A test for SUB_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SUB_nTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 123;
            target.Memory[0] = 11;
            target.SUB_n();
            Assert.IsTrue(target.A == 112, "Error: SUB n");
        }

        /// <summary>
        ///A test for SUB_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void SUB_rTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x29;
            target.D = 0x11;
            target.SUB_r(2);
            Assert.IsTrue(target.A == 0x18);
        }

        /// <summary>
        ///A test for XOR_n
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void XOR_nTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x96;
            target.Memory[0] = 0x5d;
            target.XOR_n();
            Assert.IsTrue(target.A == 0xcb, "Error: XOR n");
        }

        /// <summary>
        ///A test for XOR_r
        ///</summary>
        [TestMethod()]
        [DeploymentItem("EmuTest.exe")]
        public void XOR_rTest()
        {
            PrivateObject z = new PrivateObject(new Z80(3.5f, new int[65536]));
            Z80_Accessor target = new Z80_Accessor(z);
            target.A = 0x96;
            target.B = 0x5d;
            target.XOR_r(0);
            Assert.IsTrue(target.A == 0xcb, "Error: XOR r");
        }
    }
}
