﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace ZXSpectrum.Z_80
{
    /// <summary>
    /// Z80 CPU emulator. This is the main part of the class.
    /// </summary>
    public partial class Z80
    {
        //  Registers
        internal int A;                              //  Accumulator
        internal Flag F;                             //  Flags    
        internal int B, C, D, E, H, L;               //  General purpose 8-bit registers
        internal int SP, PC;                         //  Stack Pointer & Program Counter

        internal int IXH, IXL, IYH, IYL;             //  Index registers

        internal int A2, B2, C2, D2, E2, H2, L2;     //  Alternate register set
        internal Flag F2;                            //  Alternate flags

        internal int R;                              //  Memory Refresh

        internal int I;                              //  Interupt Control Vector
        internal int interruptMode;                  //  Interrupt mode
        internal bool IFF1, IFF2;                    //  Interrupt flip-flops

        internal Memory Memory;                            //  ROM / RAM

        internal int CycleTStates;                      //  For counting T-states
        int previousTStates;                                

        internal bool isHalted;                      //  True if machine is in the HALT state
        
        IOController io;                //  Connected I/O devices

        internal int opcode = 0, prefix = 0, prefix2 = 0, displacement = 0;
        bool ignorePrefix;                  //  Prefixed opcodes which use (HL)

        internal bool fastLoad = false;     //  Set to trap tape-loading routines

        /// <summary>
        /// Constructor.
        /// </summary>
        public Z80(Memory Memory)
        {
            this.Memory = Memory;
            //  Set the memory to know about this CPU (for working out contention)
            this.Memory.CPU = this;
            io = new MockIODevice();
            Reset();
        }

        /// <summary>
        /// Adds an I/O device at a specific port address.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="port"></param>
        public void AddDevice(IOController device)
        {
            io = device;
        }

        /// <summary>
        /// Sets power-on defaults.
        /// </summary>
        public void Reset()
        {
            PC = 0;
            //  SP always set to FFFFh after a reset (other registers undefined).
            A = 0;
            SP = 0xffff;
            B = 0; C = 0; D = 0; E = 0; H = 0; L = 0; I = 0; R = 0; IXH = 0; IXL = 0; IYH = 0; IYL = 0;
            SetFlags(0);
            F2 = F2 & 0;
            isHalted = false;
            IFF1 = false;
            IFF2 = false;
            interruptMode = 0;
            
            //  Sanity
            opcode = 0;
            prefix = 0;
            prefix2 = 0;
            displacement = 0;
            CycleTStates = 0;
        }

        /// <summary>
        /// Simulates an interrupt occuring.
        /// </summary>
        public void Interrupt(bool nonMaskable = false)
        {
            if (isHalted)
            {
                isHalted = false;
                PC++;
            }
            if (nonMaskable)
            {
                R++;
                SP = (SP - 1) & 0xffff;
                Memory[SP] = PC >> 8;
                SP = (SP - 1) & 0xffff;
                Memory[SP] = PC & 0xff;
                PC = 0x0066;
                return;
            }
            if (IFF1)
            {
                R++;
                switch (interruptMode)
                {
                    case 0:
                        //  The device supplies an instruction on the data bus : not used in ZX Spectrum
                        break;
                    case 1:
                        //  The processor restarts at location 0x0038
                        SP = (SP - 1) & 0xffff;
                        Memory[SP] = PC >> 8;
                        SP = (SP - 1) & 0xffff;
                        Memory[SP] = PC & 0xff;
                        PC = 0x0038;
                        break;
                    case 2:
                        //  The device supplies the LSByte of the routine pointer, HSByte in register I.
                        //  Not used
                        break;
                }
            }
        }

        /// <summary>
        /// The Fetch-Decode-Execute cycle.
        /// </summary>
        public void Run(int maxTStates = 0)
        {
            //  For contention, we need to know where we are in the cycle
            CycleTStates = CycleTStates % 69888;
            previousTStates = CycleTStates;
            bool running = true;
            while (running)
            {

                //  Trap tape load
                if (PC == 0x056c || PC == 0x0112)
                {
                    fastLoad = true;
                    return;
                }

                //  Fetch
                opcode = Memory[PC];
                PC = (PC + 1) & 0xffff;
                prefix = 0;
                prefix2 = 0;
                displacement = 0;

                //  Check for prefix bytes
                if (opcode == 0xCB || opcode == 0xDD || opcode == 0xED || opcode == 0xFD)
                {
                    //  This instruction has a prefix byte
                    prefix = opcode;
                    opcode = Memory[PC];
                    PC = (PC + 1) & 0xffff;
                    if (prefix == 0xDD || prefix == 0xFD)
                    {
                        CycleTStates += 4;
                        if (opcode == 0xCB)
                        {
                            //  This instruction has 2 prefix bytes and displacement byte
                            prefix2 = 0xCB;
                            ReadDisplacementByte();
                            opcode = Memory[PC];
                            PC = (PC + 1) & 0xffff;
                        }
                        else if (opcode == 0xDD || opcode == 0xED || opcode == 0xFD)
                        {
                            //  Ignore current prefix and continue;
                            NONI();
                            R += 2;
                            R = R & 0xff;
                            continue;
                        }
                        else
                        {
                            //  HL, H & L are replaced by IX, IXH, IXL and (HL) by (IX+d) for DD opcodes

                            //  HL, H & L are replaced by IY, IYH, IYL and (HL) by (IY+d) for FD opcodes

                            //  ~EXCEPTIONS~
                            //  EX DE, HL is unaffected by these rules

                        }
                    }
                }

                //  Decode
                var x = opcode >> 6;                //  1st octal (bits 7-6)
                var y = (opcode & 0x3F) >> 3;       //  2nd octal (bits 5-3)
                var z = opcode & 0x07;              //  3rd octal (bits 2-0)
                var p = y >> 1;                     //  y right-shifted 1 (bits 5-4)
                var q = y % 2;                      //  y modulo 2 (bit 3)

                //  Execute
                switch (prefix)
                {
                    case 0:
                    case 0xDD:
                    case 0xFD:
                        //  DDCB & FDCB double-prefixed opcodes:
                        if (prefix2 == 0xCB)
                        {     
                            if (x == 0)
                            {
                                //  Rotate / Shift memory location and copy result to register
                                if (y == 0) { RLC(6); }
                                if (y == 1) { RRC(6); }
                                if (y == 2) { RL(6); }
                                if (y == 3) { RR(6); }
                                if (y == 4) { SLA(6); }
                                if (y == 5) { SRA(6); }
                                if (y == 6) { SLL(6); }
                                if (y == 7) { SRL(6); }
                                //  Copy result to register ?
                                if (z != 6)
                                {
                                    ignorePrefix = true;
                                    SetRegister(z, GetRegister(6));
                                    ignorePrefix = false;
                                    CycleTStates -= 11;
                                }
                                else
                                {
                                    CycleTStates -= 4;
                                }
                                break;
                            }
                            //  Test bit at memory loc
                            if (x == 1) { CycleTStates -= 8; BIT(y, 6); break; }
                            //  Reset bit and copy to register
                            if (x == 2)
                            { 
                                RES(y, 6);
                                //  Copy result to register ?
                                if (z != 6)
                                {
                                    ignorePrefix = true;
                                    SetRegister(z, GetRegister(6));
                                    ignorePrefix = false;
                                    CycleTStates -= 11;
                                }
                                else
                                {
                                    CycleTStates -= 4;
                                }
                                break;
                            }
                            //  Set bit and copy to register
                            if (x == 3)
                            {
                                SET(y, 6);
                                //  Copy result to register ?
                                if (z != 6)
                                {
                                    ignorePrefix = true;
                                    SetRegister(z, GetRegister(6));
                                    ignorePrefix = false;
                                    CycleTStates -= 11;
                                }
                                else
                                {
                                    CycleTStates -= 4;
                                }
                                break;
                            }
                            break;
                        }
                        //  Unprefixed opcodes (& DD & FD prefixed opcodes w/o CB 2nd prefix)
                        if (x == 0)
                        {
                            //  Relative jumps & misc. operations
                            if (z == 0)
                            {
                                if (y == 0) { NOP(); break; }
                                if (y == 1) { EX_AF_AF2(); break; }
                                if (y == 2) { DJNZ(); break; }
                                if (y > 2 && y < 8) { JR(y - 4); break; }
                            }

                            //  16-bit immediate load & add
                            if (z == 1)
                            {  
                                if (q == 0) { LD_dd_nn(p); break; }
                                if (q == 1) { ADD_HL_ss(p); break; }
                            }

                            //  Indirect loading
                            if (z == 2)
                            {
                                if (q == 0)
                                {
                                    if (p == 0) { LD_BC_A(); break; }
                                    if (p == 1) { LD_DE_A(); break; }
                                    if (p == 2) { LD_nn_HL(); break; }
                                    if (p == 3) { LD_nn_A(); break; }
                                }
                                if (q == 1)
                                {
                                    if (p == 0) { LD_A_BC(); break; }
                                    if (p == 1) { LD_A_DE(); break; }
                                    if (p == 2) { LD_HL_nn(); break; }
                                    if (p == 3) { LD_A_nn(); break; }
                                }
                            }

                            //  16-bit INC, DEC
                            if (z == 3)
                            {
                                if (q == 0) { INC_ss(p); break; }
                                if (q == 1) { DEC_ss(p); break; }
                            }

                            //  8-bit INC, DEC
                            if (z == 4) { INC_r(y); break; }
                            if (z == 5) { DEC_r(y); break; }
                            
                            //  8-bit Load Immediate
                            if (z == 6) { LD_r_n(y); break; }

                            //  Misc. Flag & Accumulator ops
                            if (z == 7)
                            {
                                if (y == 0) { RLCA(); break; }
                                if (y == 1) { RRCA(); break; }
                                if (y == 2) { RLA(); break; }
                                if (y == 3) { RRA(); break; }
                                if (y == 4) { DAA(); break; }
                                if (y == 5) { CPL(); break; }
                                if (y == 6) { SCF(); break; }
                                if (y == 7) { CCF(); break; }
                            }
                        }

                        if (x == 1)
                        {
                            //  Halt
                            if (y == 6 && z == 6) { HALT(); break; }

                            //  8-bit loading
                            if (y < 8 && z < 8) { LD_r_r(y, z); break; }
                        }

                        if (x == 2)
                        {
                            //  Operations on Accumulator and register/memory location
                            if (y == 0) { ADD_A_r(z); break; }
                            if (y == 1) { ADC_A_r(z); break; }
                            if (y == 2) { SUB_r(z); break; }
                            if (y == 3) { SBC_A_r(z); break; }
                            if (y == 4) { AND_r(z); break; }
                            if (y == 5) { XOR_r(z); break; }
                            if (y == 6) { OR_r(z); break; }
                            if (y == 7) { CP_r(z); break; }
                        }

                        if (x == 3)
                        {
                            //  Conditional return
                            if (z == 0) { RET_cc(y); break; }

                            //  POP & misc operations
                            if (z == 1)
                            {
                                if (q == 0) { POP_qq(p); break; }
                                if (q == 1)
                                {
                                    if (p == 0) { RET(); break; }
                                    if (p == 1) { EXX(); break; }
                                    if (p == 2) { JP_HL(); break; }
                                    if (p == 3) { LD_SP_HL(); break; }
                                }
                            }

                            //  Conditional jump
                            if (z == 2) { JP_cc_nn(y); break; }

                            //  Misc ops.
                            if (z == 3)
                            {
                                if (y == 0) { JP_nn(); break; }
                                if (y == 1) { Console.WriteLine("CB Prefix incorrectly handled."); break; }
                                if (y == 2) { OUT_n_A(); break; }
                                if (y == 3) { IN_A_n(); break; }
                                if (y == 4) { EX_SP_HL(); break; }
                                if (y == 5) { EX_DE_HL(); break; }
                                if (y == 6) { DI(); break; }
                                if (y == 7) { EI(); break; }
                            }

                            //  Conditional call
                            if (z == 4) { CALL_cc_nn(y); break; }

                            //  Push & call
                            if (z == 5)
                            {
                                if (q == 0) { PUSH_qq(p); break; }
                                if (q == 1)
                                {
                                    if (p == 0) { CALL_nn(); break; }
                                    if (p == 1) { Console.WriteLine("DD Prefix incorrectly handled."); break; }
                                    if (p == 2) { Console.WriteLine("ED Prefix incorrectly handled."); break; }
                                    if (p == 3) { Console.WriteLine("FD Prefix incorrectly handled."); break; }
                                }
                            }

                            //  Operations on Accumulator and immediate operand
                            if (z == 6)
                            { 
                                if (y == 0) { ADD_A_n(); break; }
                                if (y == 1) { ADC_A_n(); break; }
                                if (y == 2) { SUB_n(); break; }
                                if (y == 3) { SBC_A_n(); break; }
                                if (y == 4) { AND_n(); break; }
                                if (y == 5) { XOR_n(); break; }
                                if (y == 6) { OR_n(); break; }
                                if (y == 7) { CP_n(); break; }
                            }

                            //  Restart
                            if (z == 7) { RST(y * 8); break; }

                        }
                        Console.WriteLine("Unknown opcode: " + opcode);
                        break;
                    case 0xCB:
                        //  CB prefixed opcodes
                        if (x == 0)
                        {
                            //  Rotate / Shift register/memory
                            if (y == 0) { RLC(z); break; }
                            if (y == 1) { RRC(z); break; }
                            if (y == 2) { RL(z); break; }
                            if (y == 3) { RR(z); break; }
                            if (y == 4) { SLA(z); break; }
                            if (y == 5) { SRA(z); break; }
                            if (y == 6) { SLL(z); break; }
                            if (y == 7) { SRL(z); break; }
                        }

                        //  Test / reset / set bit
                        if (x == 1) { BIT(y, z); break; }
                        if (x == 2) { RES(y, z); break; }
                        if (x == 3) { SET(y, z); break; }
                        Console.WriteLine("Unknown opcode: " + opcode);
                        break;
                    case 0xED:
                        //  ED prefixed opcodes
                        if (x == 0 || x == 3) { NONI(); NOP();  break; }

                        if (x == 1)
                        {
                            //  Port I/O
                            if (z == 0) { IN_r_C(y); break; }
                            if (z == 1) { OUT_C_r(y); break; }

                            //  16-bit add/subtract with Carry
                            if (z == 2)
                            {
                                if (q == 0) { SBC_HL(p); break; }
                                if (q == 1) { ADC_HL(p); break; }
                            }

                            //  Load register pair from/to immediate address
                            if (z == 3)
                            {
                                if (q == 0) { LD_nn_dd(p); break; }
                                if (q == 1) { LD_dd_nn2(p); break; }
                            }

                            //  Negation
                            if (z == 4) { NEG(); break; }

                            //  Return from interrupt
                            if (z == 5)
                            {
                                if (y == 1) { RETI(); break; }
                                RETN(); break;
                            }

                            //  Set interrupt mode
                            if (z == 6) { IM(y); break; }

                            //  Misc. operations
                            if (z == 7)
                            {
                                if (y == 0) { LD_I_A(); break; }
                                if (y == 1) { LD_R_A(); break; }
                                if (y == 2) { LD_A_I(); break; }
                                if (y == 3) { LD_A_R(); break; }
                                if (y == 4) { RRD(); break; }
                                if (y == 5) { RLD(); break; }
                                if (y == 6 || y == 7) { NOP(); break; }
                            }
                        }
                        if (x == 2)
                        {
                            //  Block instructions
                            if (y == 4)
                            {
                                if (z == 0) { LDI(); break; }
                                if (z == 1) { CPI(); break; }
                                if (z == 2) { INI(); break; }
                                if (z == 3) { OUTI(); break; }
                            }
                            if (y == 5)
                            {
                                if (z == 0) { LDD(); break; }
                                if (z == 1) { CPD(); break; }
                                if (z == 2) { IND(); break; }
                                if (z == 3) { OUTD(); break; }
                            }
                            if (y == 6)
                            {
                                if (z == 0) { LDIR(); break; }
                                if (z == 1) { CPIR(); break; }
                                if (z == 2) { INIR(); break; }
                                if (z == 3) { OTIR(); break; }
                            }
                            if (y == 7)
                            {
                                if (z == 0) { LDDR(); break; }
                                if (z == 1) { CPDR(); break; }
                                if (z == 2) { INDR(); break; }
                                if (z == 3) { OTDR(); break; }
                            }
                            NONI(); NOP();
                        }
                        break;
                    default:
                        //  Unknown opcode
                        Console.WriteLine("Unknown prefix: " + prefix);
                        break;
                }

                //  Increment r; twice for prefixed instructions
                bool bigR = false;
                if (R > 127)
                {
                    bigR = true;
                }
                R++;
                if (prefix != 0)
                {
                    R++;
                }

                R = (R & 0x7f);
                if (bigR)
                {
                    R += 128;
                }

                //  If we have defined a max amount of tStates to run for then check if we should return
                if (maxTStates > 0 && CycleTStates - previousTStates >= maxTStates)
                {
                    running = false;
                }
            }
        }

        /// <summary>
        /// OTDR
        /// (HL) is written to port BC. B and HL are decremented. If B != 0 then PC -= 2
        /// </summary>
        private void OTDR()
        {
            int written = Memory[Get16BitRegisters(2)];
            io.Write(Get16BitRegisters(0), written);
            B = (B - 1) & 0xff;
            Set16BitRegisters(2, Get16BitRegisters(2) - 1);
            if (B != 0)
            {
                PC = (PC - 2) & 0xffff;
                CycleTStates += 21;
            }
            else
            {
                CycleTStates += 16;
            }
            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((written & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (written + L > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = ((written + L) & 7) ^ B;
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// INDR
        /// 1 byte from port BC is written to (HL). HL and BC are decremented. If B != 0, PC -=2.
        /// </summary>
        private void INDR()
        {
            int read = io.Read(Get16BitRegisters(0));
            Memory[Get16BitRegisters(2)] = read;
            Set16BitRegisters(2, Get16BitRegisters(2) - 1);
            B = (B - 1) & 0xff;
            if (B != 0)
            {
                PC = (PC - 2) & 0xffff;
                CycleTStates += 21;
            }
            else
            {
                CycleTStates += 16;
            }
            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((read & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (read + ((C - 1) & 255) > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = (((read + ((C - 1) & 255)) & 7) ^ B);
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// CPDR
        /// (HL) compared with Accumulator. If true Z is set. HL and BC decremented. If BC != 0 and A = (HL), PC -= 2. 
        /// </summary>
        private void CPDR()
        {
            var compare = Memory[Get16BitRegisters(2)];
            if (compare == A)
                Set(Flag.Zero);
            else
                Reset(Flag.Zero);

            Set16BitRegisters(2, Get16BitRegisters(2) - 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);

            if (compare != A && Get16BitRegisters(0) != 0)
            {
                PC = (PC - 2) & 0xffff;
                CycleTStates += 21;
            }
            else
            {
                CycleTStates += 16;
            }

            ModifySignFlag8((A - compare) & 0xff);
            ModifyHalfCarryFlag8(A, -compare);
            int n = (A - compare) & 0xff;
            if ((F & Flag.HalfCarry) == Flag.HalfCarry)
            {
                n = (n-1) & 0xff;
            }
            ModifyUndocumentedFlagsCompareGroup(n);
            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);

            Set(Flag.Subtract);
        }

        /// <summary>
        /// LDDR
        /// (HL) is written to (DE). HL, DE and BC all decremented. If BC != 0 then PC -= 2.
        /// </summary>
        private void LDDR()
        {
            Memory[Get16BitRegisters(1)] = Memory[Get16BitRegisters(2)];

            if (Get16BitRegisters(0) - 1 == 0)
            {
                CycleTStates += 16;
            }
            else
            {
                CycleTStates += 21;
                PC = (PC - 2) & 0xffff;
            }
            Reset(Flag.HalfCarry);

            Reset(Flag.Subtract);

            ModifyUndocumentedFlagsLoadGroup();

            Set16BitRegisters(0, Get16BitRegisters(0) - 1);
            Set16BitRegisters(1, Get16BitRegisters(1) - 1);
            Set16BitRegisters(2, Get16BitRegisters(2) - 1);

            Reset(Flag.ParityOverflow);
        }

        /// <summary>
        /// OTIR
        /// (HL) is written to IO port BC. HL incremented, B decremented. If B != 0 then PC -= 2.
        /// </summary>
        private void OTIR()
        {
            int written = Memory[Get16BitRegisters(2)];

            io.Write(Get16BitRegisters(0), written);
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            B = (B - 1) & 0xff;
            if (B != 0)
            {
                CycleTStates += 21;
                PC = (PC - 2) & 0xffff;
            }
            else
            {
                CycleTStates += 16;
            }
            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((written & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (written + L > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = ((written + L) & 7) ^ B;
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// INIR
        /// BC selects an IO port. 1 byte from port written to (HL). HL incremented, B decremented. If B !=0 then PC -= 2.
        /// </summary>
        private void INIR()
        {
            int read = io.Read(Get16BitRegisters(0));
            Memory[Get16BitRegisters(2)] = read;
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            B = (B - 1) & 0xff;
            if (B != 0)
            {
                PC = (PC - 2) & 0xffff;
                CycleTStates += 21;
            }
            else
            {
                CycleTStates += 16;
            }
            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((read & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (read + ((C + 1) & 255) > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = (((read + ((C + 1) & 255)) & 7) ^ B);
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// CPIR
        /// (HL) compared with Accumulator. If true compare, Z is set. HL incremented, BC decremented. Repeat if BC !=0 and A != (HL)
        /// </summary>
        private void CPIR()
        {
            var compare = Memory[Get16BitRegisters(2)];

            if (compare == A)
                Set(Flag.Zero);
            else
                Reset(Flag.Zero);

            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);

            if (compare != A && Get16BitRegisters(0)  != 0)
            {
                PC = (PC - 2) & 0xffff;
                CycleTStates += 21;
            }
            else
            {
                CycleTStates += 16;
            }

            ModifySignFlag8((A - compare) & 0xff);

            ModifyHalfCarryFlag8(A, -compare);
            int n = (A - compare) & 0xff;
            if ((F & Flag.HalfCarry) == Flag.HalfCarry)
            {
                n = (n-1) & 0xff;
            }
            ModifyUndocumentedFlagsCompareGroup(n);

            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);

            Set(Flag.Subtract);
        }

        /// <summary>
        /// LDIR
        /// (HL) is transferred to (DE). HL and DE are incremented, BC decremented. If BC !=0, PC is decremented by 2.
        /// </summary>
        private void LDIR()
        {
            Memory[Get16BitRegisters(1)] = Memory[Get16BitRegisters(2)];
            
            if (Get16BitRegisters(0) -1 == 0)
            {
                CycleTStates += 16;
            }
            else
            {
                CycleTStates += 21;
                PC = (PC - 2) & 0xffff;
            }     
       
            Reset(Flag.HalfCarry);
                       
            Reset(Flag.Subtract);

            ModifyUndocumentedFlagsLoadGroup();

            Set16BitRegisters(1, Get16BitRegisters(1) + 1);
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);


            Reset(Flag.ParityOverflow);
        }

        /// <summary>
        /// OUTD
        /// BC selects a port to which (HL) is written. B and HL are decremented.
        /// </summary>
        private void OUTD()
        {
            CycleTStates += 16;
            int written = Memory[Get16BitRegisters(2)];
            io.Write(Get16BitRegisters(0), written);
            B = (B - 1) & 0xff;
            Set16BitRegisters(2, Get16BitRegisters(2) - 1);

            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((written & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (written + L > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = ((written + L) & 7) ^ B;
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// IND
        /// BC selects a port which writes 1 byte to (HL). B and HL are decremented.
        /// </summary>
        private void IND()
        {
            CycleTStates += 16;
            B = (B - 1) & 0xff;
            
            int read = io.Read(Get16BitRegisters(0));
            Memory[Get16BitRegisters(2)] = read;
            Set16BitRegisters(2, (Get16BitRegisters(2) - 1) & 0xffff);

            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((read & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (read + ((C - 1) & 255) > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = (((read + ((C - 1) & 255)) & 7) ^ B);
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// CPD
        /// (HL) compared with Accumulator. If true compare, Z is set. HL and BC are decremented.
        /// </summary>
        private void CPD()
        {
            CycleTStates += 16;

            var compare = Memory[Get16BitRegisters(2)];
            if (compare == A)
                Set(Flag.Zero);
            else
                Reset(Flag.Zero);

            Set16BitRegisters(2, Get16BitRegisters(2) - 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);

            ModifySignFlag8((A - compare) & 0xff);
            ModifyHalfCarryFlag8(A, -compare);
            
            int n = (A - compare) & 0xff;
            if ((F & Flag.HalfCarry) == Flag.HalfCarry)
            {
                n = (n-1) & 0xff;
            }

            ModifyUndocumentedFlagsCompareGroup(n);
            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);

            Set(Flag.Subtract);
        }

        /// <summary>
        /// LDD
        /// (HL) is written to (DE). These register pairs and BC are all decremented.
        /// </summary>
        private void LDD()
        {
            CycleTStates += 16;

            Memory[Get16BitRegisters(1)] = Memory[Get16BitRegisters(2)];

            Reset(Flag.HalfCarry);

            Reset(Flag.Subtract);

            ModifyUndocumentedFlagsLoadGroup();

            Set16BitRegisters(0, Get16BitRegisters(0) - 1);
            Set16BitRegisters(1, Get16BitRegisters(1) - 1);
            Set16BitRegisters(2, Get16BitRegisters(2) - 1);

            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);
        }

        /// <summary>
        /// OUTI
        /// BC selects a port to which (HL) is written. B is decremented, HL is incremented.
        /// </summary>
        private void OUTI()
        {
            CycleTStates += 16;

            int written = Memory[Get16BitRegisters(2)];

            io.Write(Get16BitRegisters(0), written);
            B = (B - 1) & 0xff;
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);

            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((written & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (written + L > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = ((written + L)  & 7) ^ B;
            ModifyParityFlagLogical(result);
        }

        /// <summary>
        /// INI
        /// BC selects a port which writes 1 byte to (HL). B is decremented, HL is incremented.
        /// </summary>
        private void INI()
        {
            CycleTStates += 16;

            int read = io.Read(Get16BitRegisters(0));
            Memory[Get16BitRegisters(2)] = read;
            B = (B - 1) & 0xff;
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);

            ModifySignFlag8(B);
            ModifyZeroFlag(B);

            if ((read & 128) == 128)
            {
                Set(Flag.Subtract);
            }
            else
            {
                Reset(Flag.Subtract);
            }
            ModifyUndocumentedFlags8(B);

            if (read + ((C + 1) & 255) > 255)
            {
                Set(Flag.Carry);
                Set(Flag.HalfCarry);
            }
            else
            {
                Reset(Flag.Carry);
                Reset(Flag.HalfCarry);
            }

            int result = (((read + ((C + 1) & 255)) & 7) ^ B);
            ModifyParityFlagLogical(result);

        }

        /// <summary>
        /// CPI
        /// (HL) compared with Accumulator. Z flag set if true compare. HL incremented, BC decremented.
        /// </summary>
        private void CPI()
        {
            CycleTStates += 16;

            var compare = Memory[Get16BitRegisters(2)];
            if (compare == A)
                Set(Flag.Zero);
            else
                Reset(Flag.Zero);

            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);

            ModifySignFlag8((A - compare) & 0xff);
            ModifyHalfCarryFlag8(A, -compare);
            int n = A - compare;
            if ((F & Flag.HalfCarry) == Flag.HalfCarry)
            {
                n -= 1;
            }
            ModifyUndocumentedFlagsCompareGroup(n);
            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);

            Set(Flag.Subtract);
        }

        /// <summary>
        /// LDI
        /// 1 Byte transferred from (HL) to (DE). HE and DE incremented, BC decremented.
        /// </summary>
        private void LDI()
        {
            CycleTStates += 16;

            Memory[Get16BitRegisters(1)] = Memory[Get16BitRegisters(2)];

            Reset(Flag.HalfCarry);

            Reset(Flag.Subtract);

            ModifyUndocumentedFlagsLoadGroup();

            Set16BitRegisters(1, Get16BitRegisters(1) + 1);
            Set16BitRegisters(2, Get16BitRegisters(2) + 1);
            Set16BitRegisters(0, Get16BitRegisters(0) - 1);

            if (Get16BitRegisters(0) != 0)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);
        }

        /// <summary>
        /// RLD
        /// Low nibble of (HL) -> high nibble of (HL). High nibble of (HL) -> low nibble of Accumulator. Low nibble of Accumulator -> low nibble of (HL).
        /// </summary>
        private void RLD()
        {
            CycleTStates += 18;

            var loc = Get16BitRegisters(2);
            var mLow = Memory[loc] & 0xf;
            var mHi = (Memory[loc] >> 4) & 0xf;
            var aLow = A & 0xf;

            A = (A & 0xf0) + mHi;
            Memory[loc] = aLow + (mLow << 4);

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// RRD
        /// Low nibble of (HL) -> low nibble of Accumulator. Low nibble of Accumulator -> high nibble of (HL). High nibble of (HL) -> low nibble of (HL).
        /// </summary>
        private void RRD()
        {
            CycleTStates += 18;

            var loc = Get16BitRegisters(2);
            var mLow = Memory[loc] & 0xf;
            var mHi = (Memory[loc] >> 4) & 0xf;
            var aLow = A & 0xf;

            A = (A & 0xf0) + mLow;
            Memory[loc] = mHi + (aLow << 4);

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// LD A, R
        /// The contents of R are loaded to the Accumulator.
        /// </summary>
        private void LD_A_R()
        {
            CycleTStates += 9;

            A = (R + 2) & 0xff;

            ModifySignFlag8(R);
            ModifyZeroFlag(R);
            Reset(Flag.HalfCarry);
            if (IFF2 == true)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(R);
        }

        /// <summary>
        /// LD A, I
        /// The contents of I are loaded to the Accumulator.
        /// </summary>
        private void LD_A_I()
        {
            CycleTStates += 9;

            A = I;

            ModifySignFlag8(I);
            ModifyZeroFlag(I);
            Reset(Flag.HalfCarry);
            if (IFF2 == true)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// LD R, A
        /// The contents of the Accumulator are loaded to R.
        /// </summary>
        private void LD_R_A()
        {
            CycleTStates += 9;
            R = (A - 2) & 0xff;
        }

        /// <summary>
        /// LD I, A
        /// The contents of the Accumulator are loaded to I.
        /// </summary>
        private void LD_I_A()
        {
            CycleTStates += 9;
            I = A;
        }

        /// <summary>
        /// IM i
        /// Sets interrupt mode.
        /// </summary>
        /// <param name="y"></param>
        private void IM(int y)
        {
            CycleTStates += 8;

            if (y == 2 || y == 6)
                interruptMode = 1;
            else if (y == 3 || y == 7)
                interruptMode = 2;
            else
                interruptMode = 0;
        }

        /// <summary>
        /// RETI
        /// Return from interrupt.
        /// </summary>
        private void RETI()
        {
            CycleTStates += 14;

            PC = Memory[SP];
            SP = (SP + 1) & 0xffff;
            PC += (Memory[SP] << 8);
            SP = (SP + 1) & 0xffff;
        }

        /// <summary>
        /// RETN
        /// Restores the PC at the end of a non-maskable interrupts service.
        /// </summary>
        internal void RETN()
        {
            CycleTStates += 14;

            IFF1 = IFF2;
            PC = Memory[SP];
            SP = (SP + 1) & 0xffff;
            PC += (Memory[SP] << 8);
            SP = (SP + 1) & 0xffff;
        }

        /// <summary>
        /// NEG
        /// The contents of the accumulator are negated (2's complement).
        /// </summary>
        private void NEG()
        {
            CycleTStates += 8;

            if (A == 0x80)
                Set(Flag.ParityOverflow);
            else
                Reset(Flag.ParityOverflow);

            if (A == 0)
                Reset(Flag.Carry);
            else
                Set(Flag.Carry);

            var addition = -A;
            A = (byte)(0 + addition);
            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyHalfCarryFlag8(0, addition);
            Set(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// LD dd, (nn)
        /// The contents of address nn are loaded to low byte of register pair dd, next highest address to high byte.
        /// </summary>
        /// <param name="dd"></param>
        private void LD_dd_nn2(int dd)
        {
            CycleTStates += 20;

            var address = Memory[PC];
            PC = (PC + 1) & 0xffff;
            address += (Memory[PC] << 8);
            PC = (PC + 1) & 0xffff;
            Set16BitRegisters(dd, Memory[address] + (Memory[(address + 1) & 0xffff] << 8));
        }

        /// <summary>
        /// LD (nn), dd
        /// Low order byte of register pair dd is loaded to memory address nn, upper byte to nn+1.
        /// </summary>
        /// <param name="dd"></param>
        private void LD_nn_dd(int dd)
        {
            CycleTStates += 20;

            var address = Memory[PC];
            PC = (PC + 1) & 0xffff;
            address += (Memory[PC] << 8);
            PC = (PC + 1) & 0xffff;
            Memory[address] = Get16BitRegisters(dd) & 0xff;
            Memory[(address + 1) & 0xffff] = Get16BitRegisters(dd) >> 8;
        }

        /// <summary>
        /// ADC HL, ss
        /// The contents of register pair ss are added with the Carry flag to the contents of HL.
        /// </summary>
        /// <param name="ss"></param>
        private void ADC_HL(int ss)
        {
            CycleTStates += 15;

            var initial = Get16BitRegisters(2);
            var addition = Get16BitRegisters(ss);

            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xffff;
            }          

            var result = (initial + addition) & 0xffff;
            Set16BitRegisters(2, result);

            //  Carry / Half-carry flags set dependent on high-byte

            //  16-bit Carry
            ModifyCarryFlag16(initial, addition);
            ModifyHalfCarryFlag16(initial, addition);

            ModifySignFlag16(result);
            ModifyZeroFlag(result);

            ModifyOverflowFlag16(initial, addition, result);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags16(result);
        }

        /// <summary>
        /// SBC HL, ss
        /// The contents of register pair ss and the Carry flag are subtracted from HL.
        /// </summary>
        /// <param name="ss"></param>
        private void SBC_HL(int ss)
        {
            CycleTStates += 15;

            var initial = Get16BitRegisters(2);
            var addition = Get16BitRegisters(ss);
            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xffff;
            }
            addition = -addition;

            var result = (initial + addition) & 0xffff;
            Set16BitRegisters(2, result);

            //  Carry / Half-carry flags set dependent on high-byte

            //  16-bit Carry
            ModifyCarryFlag16(initial, addition);
            ModifyHalfCarryFlag16(initial, addition);

            ModifySignFlag16(result);
            ModifyZeroFlag(result);

            ModifyOverflowFlag16(initial, addition, result);
            Set(Flag.Subtract);

            ModifyUndocumentedFlags16(result);
        }

        /// <summary>
        /// OUT (C), r
        /// Contents of register BC are used to a select an I/O port. 1 byte is written to this port from register r
        /// </summary>
        /// <param name="r"></param>
        private void OUT_C_r(int r)
        {
            CycleTStates += 12;

            if (r == 6)
                io.Write(Get16BitRegisters(0), 0);
            else
                io.Write(Get16BitRegisters(0), GetRegister(r));
        }

        /// <summary>
        /// IN r, (C)
        /// Contents of register BC are used to a select an I/O port. 1 byte is written from this port to register r
        /// </summary>
        /// <param name="r"></param>
        private void IN_r_C(int r)
        {
            CycleTStates += 12;

            var input = io.Read(Get16BitRegisters(0)) & 0xff;

            if (r != 6 && opcode != 0xed70)
                SetRegister(r, input);

            ModifySignFlag8(input);
            ModifyZeroFlag(input);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(input);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(input);
        }

        /// <summary>
        /// Invalid Instruction.
        /// </summary>
        private void NONI()
        {
            NOP();
        }

        /// <summary>
        /// SET b, m
        /// Bit b in register m is set.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="r"></param>
        private void SET(int b, int r)
        {
            if (r == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            SetRegister(r, (GetRegister(r) | (1 << b)));
        }

        /// <summary>
        /// RES b, m
        /// Bit b in register m is reset.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="r"></param>
        private void RES(int b, int r)
        {
            if (r == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            SetRegister(r, (GetRegister(r) & ~(1 << b)));
        }

        /// <summary>
        /// BIT b, r
        /// Tests bit b in register r and sets Zero flag accordingly.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="r"></param>
        private void BIT(int b, int r)
        {
            if (r == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            r = GetRegister(r);

            if ((r & (1 << b)) == (1 << b))
            {
                Reset(Flag.Zero);
                Reset(Flag.ParityOverflow);
            }
            else
            {
                Set(Flag.Zero);
                Set(Flag.ParityOverflow);
            }
            Set(Flag.HalfCarry);
            if (b == 7 && (r & 128) == 128)
            {
                Set(Flag.Sign);
            }
            else
            {
                Reset(Flag.Sign);
            }
            Reset(Flag.Subtract);

            if (prefix2 == 0)
            {
                ModifyUndocumentedFlags8(r);
            }
            else
            {
                CycleTStates += 8;
                ModifyUndocumentedFlags8((Get16BitRegisters(2) + displacement) >> 8);
            }
        }

        /// <summary>
        /// SRL m
        /// Contents of register m are shifted right 1 position. Bit 0 is copied to carry flag, bit 7 is reset.
        /// </summary>
        /// <param name="m"></param>
        private void SRL(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);
            if ((val & 1) == 1)
            {
                Set(Flag.Carry);
            }
            else
            {
                Reset(Flag.Carry);
            }

            val = (val >> 1) & 0xff;

            SetRegister(m, val);
            Reset(Flag.Sign);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// SLL m
        /// Contents of register m are shifted left 1 position. Bit 7 is copied to carry flag, bit 0 is *set*.
        /// </summary>
        /// <param name="m"></param>
        private void SLL(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);

            if ((val & 128) == 128)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);

            //  Z80 bug - sets bit 0 to 1 instead of 0
            val = ((val << 1) & 0xff) + 1;

            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// Arithmetic shift right 1 bit on register m. Bit 0 copied to carry flag and bit 7 unchanged.
        /// </summary>
        /// <param name="m"></param>
        private void SRA(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);

            if ((val & 1) == 1)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);

            if ((val & 128) == 128)
            {
                val = (val >> 1) & 0xff;
                val = val | 128;
            }
            else
            {
                val = (val >> 1) & 0xff;
            }

            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// SLA m
        /// Arithmetic shift left 1 bit is performed on register m. Bit 7 copied to Carry flag.
        /// </summary>
        /// <param name="m"></param>
        private void SLA(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);
            if ((val & 128) == 128)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);
            val = (val << 1) & 0xff;

            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// RR m
        /// Register m is rotated right 1 bit. Bit 0 is copied to the Carry flag and the previous contents of Carry are copied to bit 7.
        /// </summary>
        /// <param name="m"></param>
        private void RR(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);
            if ((F & Flag.Carry) == Flag.Carry)
            {
                if ((val & 1) != 1) Reset(Flag.Carry);
                val = ((val >> 1) + 128) & 0xff;
            }
            else
            {
                if ((val & 1) == 1) Set(Flag.Carry);
                val = (val >> 1) & 0xff;
            }

            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// RL m
        /// Register m is rotated left 1 bit. Bit 7 is copied to the Carry flag and the previous contents of Carry are copied to bit 0.
        /// </summary>
        /// <param name="m"></param>
        private void RL(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);
            if ((F & Flag.Carry) == Flag.Carry)
            {
                if ((val & 128) != 128) Reset(Flag.Carry);
                val = ((val << 1) + 1) & 0xff;
            }
            else
            {
                if ((val & 128) == 128) Set(Flag.Carry);
                val = (val << 1) & 0xff;
            }    

            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// RRC m
        /// The contents of register m are rotated right 1 bit. Bit 0 is copied to bit 7 and Carry flag.
        /// </summary>
        /// <param name="m"></param>
        private void RRC(int m)
        {
            if (m == 6)
                CycleTStates += 9;
            else
                CycleTStates += 8;

            var val = GetRegister(m);
            if ((val & 1) == 1)
            {
                Set(Flag.Carry);
                val = ((val >> 1) + 128) & 0xff;
            }
            else
            {
                Reset(Flag.Carry);
                val = (val >> 1) & 0xff;
            }
            SetRegister(m, val);
            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// RLC r
        /// Register r is rotated left 1 bit. Bit 7 copied to bit 0 and Carry flag.
        /// </summary>
        /// <param name="r"></param>
        private void RLC(int r)
        {
            if (r == 6)
                CycleTStates += 9;
            else 
                CycleTStates += 8;
            
            var val = GetRegister(r);
            if ((val & 128) == 128)
            {
                Set(Flag.Carry);
                val = ((val << 1) + 1) & 0xff;
            }
            else
            {
                Reset(Flag.Carry);
                val = (val << 1) & 0xff;
            }
            SetRegister(r, val);

            ModifySignFlag8(val);
            ModifyZeroFlag(val);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(val);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(val);
        }

        /// <summary>
        /// RST p
        /// The PC is pushed to the stack and the page-zero memory location given by operand p is loaded to the PC.
        /// </summary>
        /// <param name="p"></param>
        private void RST(int p)
        {
            CycleTStates += 11;
            SP = (SP - 1) & 0xffff;
            Memory[SP] = PC >> 8;
            SP = (SP - 1) & 0xffff;
            Memory[SP] = PC & 0xff;
            PC = p;
        }


        /// <summary>
        /// CP r
        /// If the Accumulator is a true comparison with Register r, Z is set.
        /// CP is just SUB with result thrown away.
        /// </summary>
        /// <param name="r"></param>
        private void CP_r(int r)
        {
            CycleTStates += 4;

            var initial = A;
            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }
            var reg = GetRegister(r);
            var addition = -reg;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);

            ModifyHalfCarryFlag8(initial, addition);
            ModifyCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Set(Flag.Subtract);

            A = initial;

            ModifyUndocumentedFlags8(-addition);
        }

        /// <summary>
        /// CP n
        /// If there is a true compare between operand n and the Accumulator, the Z flag is set.
        /// CP is just SUB with the result thrown away.
        /// </summary>
        private void CP_n()
        {
            CycleTStates += 7;

            var initial = A;
            var addition = -Memory[PC];
            PC = (PC + 1) & 0xffff;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);

            ModifyHalfCarryFlag8(initial, addition);
            ModifyCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Set(Flag.Subtract);

            A = initial;

            ModifyUndocumentedFlags8(-addition);
        }

        /// <summary>
        /// OR n
        /// Logical OR between operand n and the Accumulator.
        /// </summary>
        private void OR_n()
        {
            CycleTStates += 7;

            A = A | Memory[PC];
            PC = (PC + 1) & 0xffff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// XOR n
        /// Logical XOR between operand n and the Accumulator.
        /// </summary>
        private void XOR_n()
        {
            CycleTStates += 7;

            A = A ^ Memory[PC];
            PC = (PC + 1) & 0xffff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// AND n
        /// Logical AND between operand n and the Accumulator.
        /// </summary>
        private void AND_n()
        {
            CycleTStates += 7;

            A = A & Memory[PC];
            PC = (PC + 1) & 0xffff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Set(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// SBC A, n
        /// The contents of the Carry Flag along with operand n are subtracted from the Accumulator.
        /// </summary>
        private void SBC_A_n()
        {
            CycleTStates += 7;

            var initial = A;
            var addition = Memory[PC];
            PC = (PC + 1) & 0xffff;
            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xff;
            }
            addition = -addition;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyHalfCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Set(Flag.Subtract);
            ModifyCarryFlag8(initial, addition);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// SUB n
        /// Operand n is subtracted from the Accumulator.
        /// </summary>
        private void SUB_n()
        {
            CycleTStates += 7;

            var initial = A;
            var addition = -Memory[PC];
            PC = (PC + 1) & 0xffff;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyCarryFlag8(initial, addition);

            ModifyHalfCarryFlag8(initial, addition);

            ModifyOverflowFlag8(initial, addition, A);

            Set(Flag.Subtract);
            ModifyUndocumentedFlags8(A);

        }

        /// <summary>
        /// ADC A, n
        /// The contents of the Carry Flag along with operand n are added to the Accumulator.
        /// </summary>
        private void ADC_A_n()
        {
            CycleTStates += 7;

            var initial = A;
            var addition = Memory[PC];
            PC = (PC + 1) & 0xffff;

            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xff;
            }
            A = (initial + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyHalfCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Reset(Flag.Subtract);
            ModifyCarryFlag8(initial, addition);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// ADD A, n
        /// Operand n is added to the Accumulator.
        /// </summary>
        private void ADD_A_n()
        {
            CycleTStates += 7;

            var initial = A;
            var addition = Memory[PC];
            PC = (PC + 1) & 0xffff;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyHalfCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Reset(Flag.Subtract);
            ModifyCarryFlag8(initial, addition);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// CALL nn
        /// The PC is pushed to the stack and nn loaded to the PC.
        /// </summary>
        private void CALL_nn()
        {
            CycleTStates += 17;
            var low = Memory[PC];
            PC = (PC + 1) & 0xffff;
            var high = Memory[PC];
            PC = (PC + 1) & 0xffff;

            SP = (SP - 1) & 0xffff;
            Memory[SP] = PC >> 8;
            SP = (SP - 1) & 0xffff;
            Memory[SP] = PC & 0xff;

            PC = (high << 8) + low;
        }

        /// <summary>
        /// PUSH qq
        /// The contents of register pair qq are pushed to the stack.
        /// </summary>
        /// <param name="qq"></param>
        private void PUSH_qq(int qq)
        {
            CycleTStates += 11;

            switch (qq)
            {
                case 0: 
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = B;
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = C;
                    break;
                case 1:
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = D;
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = E;
                    break;
                case 2:
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = GetRegister(4);
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = GetRegister(5);
                    break;
                case 3:
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = A;
                    SP = (SP - 1) & 0xffff;
                    Memory[SP] = GetFlagsAsByte();
                    break;
            }
        }

        /// <summary>
        /// CALL cc,nn
        /// If condition cc is true then push PC to the stack and load PC with nn.
        /// </summary>
        /// <param name="cc"></param>
        private void CALL_cc_nn(int cc)
        {
            var low = Memory[PC];
            PC = (PC + 1) & 0xffff;
            var high = Memory[PC];
            PC = (PC + 1) & 0xffff;

            if (CheckCondition(cc))
            {
                CycleTStates += 17;
                SP = (SP - 1) & 0xffff;
                Memory[SP] = PC >> 8;
                SP = (SP - 1) & 0xffff;
                Memory[SP] = PC & 0xff;
                PC = (high << 8) + low;
            }
            else
            {
                CycleTStates += 10;
            }
        }

        /// <summary>
        /// EI
        /// Enables maskable interrupts.
        /// </summary>
        private void EI()
        {
            CycleTStates += 4;
            IFF1 = true;
            IFF2 = true;
        }

        /// <summary>
        /// DI
        /// Disables maskable interrupts.
        /// </summary>
        private void DI()
        {
            CycleTStates += 4;
            IFF1 = false;
            IFF2 = false;
        }

        /// <summary>
        /// EX DE, HL
        /// The 2-byte contents of register pairs DE & HL are exchanged.
        /// This command is unaffected by DD / FD prefix rule.
        /// </summary>
        private void EX_DE_HL()
        {
            CycleTStates += 4;

            D = D ^ H;
            H = D ^ H;
            D = D ^ H;

            E = E ^ L;
            L = E ^ L;
            E = E ^ L;
        }

        /// <summary>
        /// EX (SP), HL
        /// HL is exchanged with the contents of the memory address specified by SP.
        /// </summary>
        private void EX_SP_HL()
        {
            CycleTStates += 19;

            var low = Memory[SP];
            var high = Memory[(SP + 1) & 0xffff];
            var hl = Get16BitRegisters(2);
            Memory[SP] = hl & 0xff;
            Memory[(SP + 1) & 0xffff] = hl >> 8;
            SetRegister(5, low);
            SetRegister(4, high);
        }

        /// <summary>
        /// IN A, n
        /// One byte from the port specified by operand n is written to the Accumulator.
        /// Like all I/O, uses undocumented 16-bit addressing.
        /// </summary>
        private void IN_A_n()
        {
            CycleTStates += 11;
            var port = Memory[PC];
            PC = (PC + 1) & 0xffff;
            A = io.Read((A << 8) + port);
        }

        /// <summary>
        /// OUT (n), A
        /// The Accumulator is written to the I/O port specified by operand n.
        /// Like all I/O, uses undocumented 16-bit addressing.
        /// </summary>
        private void OUT_n_A()
        {
            CycleTStates += 11;
            var port = Memory[PC];
            PC = (PC + 1) & 0xffff;
            io.Write((A << 8) + port, A);
        }

        /// <summary>
        /// JP nn
        /// Operand nn is loaded to the PC.
        /// </summary>
        private void JP_nn()
        {
            CycleTStates += 10;
            PC = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
        }

        /// <summary>
        /// JP cc, nn
        /// if condition cc is true, jump 
        /// </summary>
        /// <param name="cc"></param>
        private void JP_cc_nn(int cc)
        {
            CycleTStates += 10;

            if (CheckCondition(cc))
                PC = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
            else
                PC = (PC + 2) & 0xffff;
        }

        /// <summary>
        /// LD SP, HL
        /// The SP is loaded with HL
        /// </summary>
        private void LD_SP_HL()
        {
            CycleTStates += 6;
            SP = Get16BitRegisters(2);
        }

        /// <summary>
        /// JP (HL)
        /// The PC is loaded with the contents of HL
        /// </summary>
        private void JP_HL()
        {
            CycleTStates += 4;
            PC = Get16BitRegisters(2);
        }

        /// <summary>
        /// EXX
        /// Register pairs BC, DE & HL are exchanged with BC', DE' & HL'
        /// Not affected by DD / FD prefix rule.
        /// </summary>
        private void EXX()
        {
            CycleTStates += 4;

            B = B ^ B2;
            B2 = B ^ B2;
            B = B ^ B2;

            C = C ^ C2;
            C2 = C ^ C2;
            C = C ^ C2;

            D = D ^ D2;
            D2 = D ^ D2;
            D = D ^ D2;

            E = E ^ E2;
            E2 = E ^ E2;
            E = E ^ E2;

            H = H ^ H2;
            H2 = H ^ H2;
            H = H ^ H2;

            L = L ^ L2;
            L2 = L ^ L2;
            L = L ^ L2;
        }

        /// <summary>
        /// RET
        /// The PC is popped off the stack.
        /// </summary>
        private void RET()
        {
            CycleTStates += 10;
            PC = Memory[SP];
            SP = (SP + 1) & 0xffff;
            PC += (Memory[SP] << 8);
            SP = (SP + 1) & 0xffff;
        }

        /// <summary>
        /// POP qq
        /// The top two bytes of the stack are popped to register pair qq.
        /// </summary>
        /// <param name="qq"></param>
        private void POP_qq(int qq)
        {
            CycleTStates += 10;
            if (qq == 3)
            {
                //  Pop to AF
                SetFlags(Memory[SP]);
                SP = (SP + 1) & 0xffff;
                A = Memory[SP];
                SP = (SP + 1) & 0xffff;
            }
            else
            {
                //  Pop to standard register pair
                var low = Memory[SP];
                SP = (SP + 1) & 0xffff;
                var high = Memory[SP];
                SP = (SP + 1) & 0xffff;
                Set16BitRegisters(qq, low, high);
            }
        }

        /// <summary>
        /// If condition cc is true, update the PC to the location specified by contents of SP.
        /// </summary>
        /// <param name="cc"></param>
        private void RET_cc(int cc)
        {
            if (CheckCondition(cc))
            {
                CycleTStates += 11;
                PC = Memory[SP];
                SP = (SP + 1) & 0xffff;
                PC += (Memory[SP] << 8);
                SP = (SP + 1) & 0xffff;
            }
            else
            {
                CycleTStates += 5;
            }
        }

        /// <summary>
        /// OR r
        /// Logical OR between Accumulator and Register r
        /// </summary>
        /// <param name="r"></param>
        private void OR_r(int r)
        {
            CycleTStates += 4;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            A = A | GetRegister(r);

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// XOR s
        /// Logical XOR between Accumulator and Register r
        /// </summary>
        /// <param name="r"></param>
        private void XOR_r(int r)
        {
            CycleTStates += 4;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            A = A ^ GetRegister(r);

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Reset(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// AND r
        /// Logical AND between Accumulator and Register r
        /// </summary>
        /// <param name="r"></param>
        private void AND_r(int r)
        {
            CycleTStates += 4;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            A = A & GetRegister(r);

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            Set(Flag.HalfCarry);
            ModifyParityFlagLogical(A);
            Reset(Flag.Subtract);
            Reset(Flag.Carry);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// SBC A, r
        /// The contents of register r along with the Carry flag are subtracted from the Accumulator.
        /// </summary>
        /// <param name="r"></param>
        private void SBC_A_r(int r)
        {
            CycleTStates += 4;

            var initial = A;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            var addition = GetRegister(r);

            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xff;
            }

            addition = -addition;

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);

            ModifyHalfCarryFlag8(initial, addition);
            ModifyCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Set(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// SUB r
        /// The contents of register r are subtracted from the Accumulator.
        /// </summary>
        /// <param name="r"></param>
        private void SUB_r(int r)
        {
            CycleTStates += 4;

            var initial = A;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            var addition = -GetRegister(r);

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            ModifyCarryFlag8(initial, addition);

            ModifyHalfCarryFlag8(initial, addition);

            ModifyOverflowFlag8(initial, addition, A);

            Set(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// ADC A, r
        /// The contents of register r along with the Carry flag are added to the accumulator.
        /// </summary>
        /// <param name="z"></param>
        private void ADC_A_r(int r)
        {
            CycleTStates += 4;

            var initial = A;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            var addition = GetRegister(r);

            if ((F & Flag.Carry) == Flag.Carry)
            {
                addition++;
                addition = addition & 0xff;
            }

            A = (initial + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);

            ModifyHalfCarryFlag8(initial, addition);
            ModifyCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// ADD A, r
        /// The contents of register r are added to the accumulator.
        /// </summary>
        /// <param name="z"></param>
        private void ADD_A_r(int r)
        {
            CycleTStates += 4;

            var initial = A;

            if (r == 6 && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
            }

            var addition = GetRegister(r);

            A = (A + addition) & 0xff;

            ModifySignFlag8(A);
            ModifyZeroFlag(A);
            
            ModifyHalfCarryFlag8(initial, addition);
            ModifyCarryFlag8(initial, addition);
            ModifyOverflowFlag8(initial, addition, A);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// HALT
        /// Suspends the CPU until a subsequent interrupt or reset is received.
        /// </summary>
        private void HALT()
        {
            CycleTStates += 4;
            isHalted = true;
            PC = (PC - 1) & 0xffff;
        }

        /// <summary>
        /// LD r, r'
        /// The contents of register r' are loaded to r
        /// </summary>
        /// <param name="y"></param>
        /// <param name="z"></param>
        private void LD_r_r(int r, int r2)
        {
            CycleTStates += 4;

            if ((r == 6 || r2 == 6) && prefix != 0)
            {
                CycleTStates += 4;
                ReadDisplacementByte();
                if (r == 6)
                {
                    ignorePrefix = true;
                    int t = GetRegister(r2);
                    ignorePrefix = false;
                    SetRegister(r, t);
                }
                else
                {
                    int t = GetRegister(r2);
                    ignorePrefix = true;
                    SetRegister(r, t);
                    ignorePrefix = false;
                }
            }
            else
            {
                SetRegister(r, GetRegister(r2));
            }
        }

        /// <summary>
        /// CCF
        /// The Carry Flag is inverted
        /// </summary>
        private void CCF()
        {
            CycleTStates += 4;

            //  Previous carry copied to half-carry?
            //  confirmed yes 24/03/2012
            if ((F & Flag.Carry) == Flag.Carry)
            {
                Set(Flag.HalfCarry);
                Reset(Flag.Carry);
            }
            else
            {
                Reset(Flag.HalfCarry);
                Set(Flag.Carry);
            }

            Reset(Flag.Subtract);
            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// SCF
        /// The Carry Flag is set.
        /// </summary>
        private void SCF()
        {
            CycleTStates += 4;

            Set(Flag.Carry);
            Reset(Flag.HalfCarry);
            Reset(Flag.Subtract);
            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// CPL
        /// The contents of the accumulator are inverted.
        /// </summary>
        private void CPL()
        {
            CycleTStates += 4;

            A = (~A) & 0xff;
            Set(Flag.HalfCarry);
            Set(Flag.Subtract);
            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// DAA
        /// Conditionally adjusts the Accumulator for BCD addition/subtraction operations.
        /// </summary>
        private void DAA()
        {
            CycleTStates += 4;

            var CF = ((F & Flag.Carry) == Flag.Carry) ? 1 : 0;
            var HF = ((F & Flag.HalfCarry) == Flag.HalfCarry) ? 1 : 0;
            var NF = ((F & Flag.Subtract) == Flag.Subtract) ? 1 : 0;

            var hiNibble = (A >> 4) & 0xf;
            var loNibble = A & 0xf;
            
            var diff = 0;

            if (CF == 0 && hiNibble < 10 && HF == 0 && loNibble < 10) diff = 0;
            else if (CF == 0 && hiNibble < 10 && HF == 1 && loNibble < 10) diff = 6;
            else if (CF == 0 && hiNibble < 9 && loNibble > 9) diff = 6;
            else if (CF == 0 && hiNibble > 9 && HF == 0 && loNibble < 10) diff = 0x60;
            else if (CF == 1 && HF == 0 && loNibble < 10) diff = 0x60;
            else if (CF == 1 && HF == 1 && loNibble < 10) diff = 0x66;
            else if (CF == 1 && loNibble > 9) diff = 0x66;
            else if (CF == 0 && hiNibble > 8 && loNibble > 9) diff = 0x66;
            else if (CF == 0 && hiNibble > 9 && HF == 1 && loNibble < 10) diff = 0x66;

            if (CF == 0 && hiNibble < 10 && loNibble < 10) Reset(Flag.Carry);
            else if (CF == 0 && hiNibble < 9 && loNibble > 9) Reset(Flag.Carry);
            else if (CF == 0 && hiNibble > 8 && loNibble > 9) Set(Flag.Carry);
            else if (CF == 0 && hiNibble > 9 && loNibble < 10) Set(Flag.Carry);
            else if (CF == 1) Set(Flag.Carry);

            if (NF == 0 && loNibble < 10) Reset(Flag.HalfCarry);
            else if (NF == 0 && loNibble > 9) Set(Flag.HalfCarry);
            else if (NF == 1 && HF == 0) Reset(Flag.HalfCarry);
            else if (NF == 1 && HF == 1 && loNibble > 5) Reset(Flag.HalfCarry);
            else if (NF == 1 && HF == 1 && loNibble < 6) Set(Flag.HalfCarry);

            if (NF == 0)
                A = (A + diff) & 0xff;
            else
                A = (A - diff) & 0xff;

            ModifySignFlag8(A);
            ModifyParityFlagLogical(A);
            ModifyUndocumentedFlags8(A);
            ModifyZeroFlag(A);
        }

        /// <summary>
        /// RRA
        /// The contents of the Accumulator are rotated right 1 bit through the Carry flag. The previous Carry flag is copied to bit 7.
        /// </summary>
        private void RRA()
        {
            CycleTStates += 4;

            bool newCarry;
            if (A % 2 == 1)
                newCarry = true;
            else
                newCarry = false;

            A = (A >> 1) & 0xff;

            if ((F & Flag.Carry) == Flag.Carry)
                A += 128;

            if (newCarry)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);

            Reset(Flag.HalfCarry);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// RLA
        /// The contents of the Accumulator are rotated left 1 bit through the Carry flag. The previous Carry flag is copied to bit 0.
        /// </summary>
        private void RLA()
        {
            CycleTStates += 4;

            bool newCarry;
            if (A > 127)
                newCarry = true;
            else
                newCarry = false;

            A = (A << 1) & 0xff;

            if ((F & Flag.Carry) == Flag.Carry)
                A = (A+1) & 0xff;

            if (newCarry)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);

            Reset(Flag.HalfCarry);
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// RRCA
        /// The contents of the Accumulator are rotated right 1 bit. Bit 0 is copied to the Carry flag and bit 7.
        /// </summary>
        private void RRCA()
        {
            CycleTStates += 4;

            if (A % 2 == 1)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);

            Reset(Flag.HalfCarry);
            Reset(Flag.Subtract);

            A = (A >> 1) & 0xff;

            if ((F & Flag.Carry) == Flag.Carry)
                A += 128;

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// RLCA
        /// The contents of the Accumulator are rotated left 1 bit. Bit 7 is copied to the Carry flag and bit 0.
        /// </summary>
        private void RLCA()
        {
            CycleTStates += 4;           

            if (A > 127)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);
            
            Reset(Flag.HalfCarry);
            Reset(Flag.Subtract);

            A = (A << 1) & 0xff;

            if ((F & Flag.Carry) == Flag.Carry)
                A = (A + 1) & 0xff;

            ModifyUndocumentedFlags8(A);
        }

        /// <summary>
        /// LD r, n
        /// The 8-bit immediate integer n is loaded to register r.
        /// </summary>
        /// <param name="r"></param>
        private void LD_r_n(int r)
        {
            CycleTStates += 7;

            if (r == 6 && prefix != 0) {
                ReadDisplacementByte();
                CycleTStates++;          
            }
            SetRegister(r, Memory[PC]);
            PC = (PC + 1) & 0xffff;
        }

        /// <summary>
        /// DEC r
        /// Register r is decremented.
        /// </summary>
        /// <param name="r"></param>
        private void DEC_r(int r)
        {
            CycleTStates += 4;

            if (r == 6)
            {
                CycleTStates++;
                if (prefix != 0)
                    ReadDisplacementByte();
            }

            var initial = GetRegister(r);

            var result = (initial - 1) & 0xff;

            SetRegister(r, result);

            ModifySignFlag8(result);
            ModifyZeroFlag(result);

            ModifyHalfCarryFlag8(initial, -1);


            ModifyOverflowFlag8(initial, -1, result);


            Set(Flag.Subtract);
            ModifyUndocumentedFlags8(result);
        }

        /// <summary>
        /// INC r
        /// Register r is incremented.
        /// </summary>
        /// <param name="r"></param>
        internal void INC_r(int r)
        {
            CycleTStates += 4;

            if (r == 6)
            {
                CycleTStates++;
                if (prefix !=0)
                    ReadDisplacementByte();
            }

            var initial = GetRegister(r);
            var result = (initial + 1) & 0xff;

            SetRegister(r, result);

            ModifySignFlag8(result);
            ModifyZeroFlag(result);

            ModifyHalfCarryFlag8(initial, 1);
            ModifyOverflowFlag8(initial, 1, result);
            Reset(Flag.Subtract);
            ModifyUndocumentedFlags8(result);
        }

        /// <summary>
        /// DEC ss
        /// The contents of register pair ss are decremented
        /// </summary>
        /// <param name="ss"></param>
        private void DEC_ss(int ss)
        {
            CycleTStates += 6;
            Set16BitRegisters(ss, (Get16BitRegisters(ss) - 1) & 0xffff);
        }

        /// <summary>
        /// INC ss
        /// The contents of register pair ss are incremented.
        /// </summary>
        /// <param name="ss"></param>
        private void INC_ss(int ss)
        {
            CycleTStates += 6;
            Set16BitRegisters(ss, (Get16BitRegisters(ss) + 1) & 0xffff);
        }

        /// <summary>
        /// LD A, (nn)
        /// The contents of the memory address specified by operand nn is loaded to the accumulator.
        /// </summary>
        private void LD_A_nn()
        {
            CycleTStates += 13;
            var address = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
            PC = (PC + 2) & 0xffff;
            A = Memory[address];
        }

        /// <summary>
        /// LD HL, (nn)
        /// The contents of memory address nn are loaded to L and the contents of nn+1 are loaded to H.
        /// </summary>
        private void LD_HL_nn()
        {
            CycleTStates += 16;
            var address = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
            PC = (PC + 2) & 0xffff;
            SetRegister(5, Memory[address]);
            SetRegister(4, Memory[(address + 1) & 0xffff]);
        }

        /// <summary>
        /// LD A, (BC)
        /// The contents of the memory location specified by BC are loaded to the accumulator.
        /// </summary>
        private void LD_A_BC()
        {
            CycleTStates += 7;
            A = Memory[Get16BitRegisters(0)];
        }

        /// <summary>
        /// LD A, (DE)
        /// The contents of the memory location specified by DE are loaded to the accumulator.
        /// </summary>
        private void LD_A_DE()
        {
            CycleTStates += 7;
            A = Memory[Get16BitRegisters(1)];
        }

        /// <summary>
        /// LD (nn), A
        /// The contents of the accumulator are loaded to the memory address nn.
        /// </summary>
        private void LD_nn_A()
        {
            CycleTStates += 13;
            var address = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
            PC = (PC + 2) & 0xffff;
            Memory[address] = A;
        }

        /// <summary>
        /// LD (nn), HL
        /// L is loaded to memory address nn and H is loaded to memory address nn+1.
        /// </summary>
        private void LD_nn_HL()
        {
            CycleTStates += 16;
            var address = Memory[PC] + (Memory[(PC + 1) & 0xffff] << 8);
            PC = (PC + 2) & 0xffff;
            Memory[address] = GetRegister(5);
            Memory[(address + 1) & 0xffff] = GetRegister(4);
        }

        /// <summary>
        /// LD (DE), A
        /// The contents of the accumulator are loaded to the memory location specified by the contents of the register pair DE.
        /// </summary>
        private void LD_DE_A()
        {
            CycleTStates += 7;
            Memory[Get16BitRegisters(1)] = A;
        }

        /// <summary>
        /// LD (BC), A
        /// The contents of the accumulator are loaded to the memory location specified by the contents of the register pair BC.
        /// </summary>
        private void LD_BC_A()
        {
            CycleTStates += 7;
            Memory[Get16BitRegisters(0)] = A;
        }

        /// <summary>
        /// ADD HL, ss
        /// Adds HL and register pair ss. (16-bit)
        /// </summary>
        /// <param name="ss"></param>
        private void ADD_HL_ss(int ss)
        {
            CycleTStates += 11;

            var HL = Get16BitRegisters(2);
            var SS = Get16BitRegisters(ss);

            //  Bitmask to force max 16 bits
            Set16BitRegisters(2, (HL + SS) & 0xffff);
            //  16-bit Carry
            ModifyCarryFlag16(HL, SS);
            //  Half-carry flags set dependent on high-bit
            ModifyHalfCarryFlag16(HL, SS);
            //  N is reset
            Reset(Flag.Subtract);

            ModifyUndocumentedFlags16(Get16BitRegisters(2));
        }

        /// <summary>
        /// LD dd, nn
        /// Load register pair dd with immediate data nn.
        /// </summary>
        /// <param name="dd"></param>
        private void LD_dd_nn(int dd)
        {
            var low = Memory[PC];
            PC = (PC + 1) & 0xffff;
            var high = Memory[PC];
            PC = (PC + 1) & 0xffff;
            Set16BitRegisters(dd, low, high);
            CycleTStates += 10;
        }

        /// <summary>
        /// JR cc, e
        /// JR e
        /// Jump relative (with or without condition).
        /// Uses 2's complement arithmetic for both forward and backward jumps.
        /// </summary>
        /// <param name="condition"></param>
        private void JR(int condition)
        {
            if (CheckCondition(condition))
            {
                CycleTStates += 12;
                PC += ((sbyte)Memory[PC] + 1);
            }
            else
            {
                PC++;
                CycleTStates += 7;
            }
            PC = PC & 0xffff;
        }

        /// <summary>
        /// DNJZ e
        /// Decrement B and jump e relative on not zero.
        /// Uses 2's complement arithmetic for both forward and backward jumps.
        /// </summary>
        private void DJNZ()
        {
            B = (B - 1) & 0xff;
            if (B != 0)
            {
                CycleTStates += 13;
                PC += ((sbyte)Memory[PC]) + 1;
            }
            else
            {
                PC++;
                CycleTStates += 8;
            }
            PC = PC & 0xffff;
        }

        /// <summary>
        /// EX AF, AF'
        /// Exchanges accumulator and flags with alternate registers.
        /// </summary>
        private void EX_AF_AF2()
        {
            CycleTStates += 4;

            A = A ^ A2;
            A2 = A ^ A2;
            A = A ^ A2;

            F = F ^ F2;
            F2 = F ^ F2;
            F = F ^ F2;
        }

        /// <summary>
        /// NOP
        /// No operation.
        /// </summary>
        private void NOP()
        {
            CycleTStates += 4;
        }
    }
}