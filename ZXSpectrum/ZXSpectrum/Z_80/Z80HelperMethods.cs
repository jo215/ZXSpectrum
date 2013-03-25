using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace ZXSpectrum.Z_80
{
    /// <summary>
    /// Defines the helper methods for working with Z80 opcodes.
    /// </summary>
    public partial class Z80
    {

        /// <summary>
        /// Reads in the optional displacement byte required by some opcodes.
        /// </summary>
        private void ReadDisplacementByte()
        {
            displacement = Memory[PC++];
            if (displacement > 127)
            {
                displacement = -256 + displacement;
            }
        }

        /// <summary>
        /// Sets the specified flag.
        /// </summary>
        /// <param name="flag"></param>
        internal void Set(Flag flag)
        {
            F = F | flag;
        }

        /// <summary>
        /// Unsets the specified flag.
        /// </summary>
        /// <param name="f"></param>
        internal void Reset(Flag flag)
        {
            F = F & ~flag;
        }

        /// <summary>
        /// Sets all flags according to given byte.
        /// </summary>
        /// <param name="value"></param>
        internal void SetFlags(int value)
        {
            F = F & 0x00;
            BitArray bits = new BitArray(new byte[] { (byte) value});
            if (bits[0]) F = F | Flag.Carry;
            if (bits[1]) F = F | Flag.Subtract;
            if (bits[2]) F = F | Flag.ParityOverflow;
            if (bits[3]) F = F | Flag.F3;
            if (bits[4]) F = F | Flag.HalfCarry;
            if (bits[5]) F = F | Flag.F5;
            if (bits[6]) F = F | Flag.Zero;
            if (bits[7]) F = F | Flag.Sign;
        }

        /// <summary>
        /// Sets all shadow flags according to given byte.
        /// </summary>
        /// <param name="value"></param>
        internal void SetShadowFlags(int value)
        {
            F2 = F2 & 0x00;
            BitArray bits = new BitArray(new byte[] { (byte)value });
            if (bits[0]) F2 = F2 | Flag.Carry;
            if (bits[1]) F2 = F2 | Flag.Subtract;
            if (bits[2]) F2 = F2 | Flag.ParityOverflow;
            if (bits[3]) F2 = F2 | Flag.F3;
            if (bits[4]) F2 = F2 | Flag.HalfCarry;
            if (bits[5]) F2 = F2 | Flag.F5;
            if (bits[6]) F2 = F2 | Flag.Zero;
            if (bits[7]) F2 = F2 | Flag.Sign;
        }

        /// <summary>
        /// Gets a byte representation of the Flags register.
        /// </summary>
        /// <returns></returns>
        internal int GetFlagsAsByte()
        {
            int val = 0;
            if ((F & Flag.Carry) == Flag.Carry) val++;
            if ((F & Flag.Subtract) == Flag.Subtract) val += 2;
            if ((F & Flag.ParityOverflow) == Flag.ParityOverflow) val += 4;
            if ((F & Flag.F3) == Flag.F3) val += 8;
            if ((F & Flag.HalfCarry) == Flag.HalfCarry) val += 16;
            if ((F & Flag.F5) == Flag.F5) val += 32;
            if ((F & Flag.Zero) == Flag.Zero) val += 64;
            if ((F & Flag.Sign) == Flag.Sign) val += 128;

            return val;
        }

        /// <summary>
        /// Gets a byte representation of the Flags register.
        /// </summary>
        /// <returns></returns>
        internal int GetShadowFlagsAsByte()
        {
            int val = 0;
            if ((F2 & Flag.Carry) == Flag.Carry) val++;
            if ((F2 & Flag.Subtract) == Flag.Subtract) val += 2;
            if ((F2 & Flag.ParityOverflow) == Flag.ParityOverflow) val += 4;
            if ((F2 & Flag.F3) == Flag.F3) val += 8;
            if ((F2 & Flag.HalfCarry) == Flag.HalfCarry) val += 16;
            if ((F2 & Flag.F5) == Flag.F5) val += 32;
            if ((F2 & Flag.Zero) == Flag.Zero) val += 64;
            if ((F2 & Flag.Sign) == Flag.Sign) val += 128;

            return val;
        }

        /// <summary>
        /// Sets the Carry Flag after an 8-bit addition if there was a carry, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyCarryFlag8(int initial, int addition)
        {
            if (initial + addition > 255 || initial + addition < 0)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);
        }

        /// <summary>
        /// Sets the Carry Flag after an addition if there was a carry, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyCarryFlag16(int initial, int addition)
        {
            if (initial + addition > 65535 || initial + addition < 0)
                Set(Flag.Carry);
            else
                Reset(Flag.Carry);
        }

        /// <summary>
        /// Sets the Half-carry/borrow flag after an addition/subtraction if bit 3 carried or bit 4 is borrowed, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyHalfCarryFlag8(int initial, int addition)
        {
            if (addition >= 0)
            {
                var hc = ((initial & 0xf) + (addition & 0xf)) & 0x10;
                //  Half-carry
                if (hc == 0x10)
                    Set(Flag.HalfCarry);
                else
                    Reset(Flag.HalfCarry);
            }
            else
            {
                var subtract = -addition;
                if ((((initial & 0x0f) - (subtract & 0x0f)) & 0x10) != 0)
                    Set(Flag.HalfCarry);
                else
                    Reset(Flag.HalfCarry);
            }
        }

        /// <summary>
        /// Sets the Half-carry/borrow flag after an addition/subtraction if bit 3 carried or bit 4 is borrowed, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyHalfCarryFlag16(int initial, int addition)
        {
            int carry = 0;
            if ((initial & 0xff) + (addition & 0xff) > 0xff)
            {
                carry = 1;
            }
            initial = initial >> 8;
            addition = (addition >> 8) + carry;

            ModifyHalfCarryFlag8(initial, addition);
        }

        /// <summary>
        /// Sets the Sign flag if result is negative, resets it otherwise.
        /// </summary>
        /// <param name="result"></param>
        private void ModifySignFlag8(int result)
        {
            if (result > 127)
                Set(Flag.Sign);
            else
                Reset(Flag.Sign);
        }

        /// <summary>
        /// Sets the Sign flag if result is negative, resets it otherwise.
        /// </summary>
        /// <param name="result"></param>
        private void ModifySignFlag16(int result)
        {
            result = result >> 8;
            ModifySignFlag8(result);
        }

        /// <summary>
        /// Sets the Zero flag if result is zero, resets it otherwise.
        /// </summary>
        /// <param name="result"></param>
        private void ModifyZeroFlag(int result)
        {
            if (result == 0)
                Set(Flag.Zero);
            else
                Reset(Flag.Zero);
        }

        /// <summary>
        /// Sets the Overflow flag if there was an overflow after addition or subtraction, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyOverflowFlag8(int initial, int addition, int result)
        {
            if (addition >= 0)
            {
                //  For addition, operands with like signs may cause overflow
                if (((initial & 0x80) ^ (addition & 0x80)) == 0)
                {
                    //  If the result has a different sign then overflow is caused
                    if (((result & 0x80) ^ (initial & 0x80)) == 0x80)
                    {
                        Set(Flag.ParityOverflow);
                        return;
                    }
                }
                Reset(Flag.ParityOverflow);
            }
            else
            {
                var subtract = -addition;
                if (((initial ^ subtract) & (initial ^ result) & 0x80) != 0)
                {
                    Set(Flag.ParityOverflow);
                }
                else
                {
                    Reset(Flag.ParityOverflow);
                } 
            }
        }

        /// <summary>
        /// Sets the Overflow flag if there was an overflow after addition or subtraction, resets it otherwise.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="addition"></param>
        private void ModifyOverflowFlag16(int initial, int addition, int result)
        {
            int carry = 0;
            if ((initial & 0xff) + (addition & 0xff) > 0xff)
            {
                carry = 1;
            }
            initial = initial >> 8;
            addition = (addition >> 8) + carry;
            result = result >> 8;

            ModifyOverflowFlag8(initial, addition, result);
        }

        /// <summary>
        /// Set the Parity flag if even number of bits in result, resets it otherwise.
        /// </summary>
        /// <param name="result"></param>
        private void ModifyParityFlagLogical(int result)
        {
            BitArray bits = new BitArray(new byte[] {(byte)result});

            var count = 0;
            for (var i = 0; i < 8; i++)
                if (bits[i]) count++;

            if ((count & 1) == 1)
                Reset(Flag.ParityOverflow);
            else
                Set(Flag.ParityOverflow);
        }

        /// <summary>
        /// Sets the undocumented bits 3 & 5 of the flags register.
        /// </summary>
        /// <param name="result"></param>
        private void ModifyUndocumentedFlags8(int result)
        {
            if ((result & 8) == 8)
                Set(Flag.F3);
            else
                Reset(Flag.F3);
            if ((result & 32) == 32)
                Set(Flag.F5);
            else
                Reset(Flag.F5);
        }

        /// <summary>
        /// Sets the undocumented bits 3 & 5 of the flags register.
        /// </summary>
        /// <param name="result"></param>
        private void ModifyUndocumentedFlags16(int result)
        {
            result = result >> 8;
            ModifyUndocumentedFlags8(result);
        }

        /// <summary>
        /// LDI / LDIR / LDD / LDIR
        /// </summary>
        private void ModifyUndocumentedFlagsLoadGroup()
        {
            //  Strange undocumented behavior
            if (((Memory[Get16BitRegisters(2)] + A) & 2) == 2)
            {
                Set(Flag.F5);
            }
            else
            {
                Reset(Flag.F5);
            }
            if (((Memory[Get16BitRegisters(2)] + A) & 8) == 8)
            {
                Set(Flag.F3);
            }
            else
            {
                Reset(Flag.F3);
            }
        }

        /// <summary>
        /// CPI / CPIR / CPD / CPDR
        /// </summary>
        private void ModifyUndocumentedFlagsCompareGroup(int n)
        {
            //  Strange undocumented behavior
            if ((n & 2) == 2)
            {
                Set(Flag.F5);
            }
            else
            {
                Reset(Flag.F5);
            }
            if ((n & 8) == 8)
            {
                Set(Flag.F3);
            }
            else
            {
                Reset(Flag.F3);
            }
        }

        /// <summary>
        /// Gets the value of a register.
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        private int GetRegister(int register)
        {
            switch (register)
            {
                case 0: return B;
                case 1: return C;
                case 2: return D;
                case 3: return E;
                case 4:
                    if (ignorePrefix)
                        return H;
                    switch (prefix) {
                        case 0xDD:
                            return IXH;
                        case 0xFD:
                            return IYH;
                        default:
                            return H;
                    } 
                case 5:
                    if (ignorePrefix)
                        return L;
                    switch (prefix)
                    {
                        case 0xDD:
                            return IXL;
                        case 0xFD:
                            return IYL;
                        default:
                            return L;
                    } 
                case 6:
                    CycleTStates += 3;  
                    switch (prefix) {
                        case 0xDD:
                        case 0xFD:
                            CycleTStates += 4; return (Memory[Get16BitRegisters(2) + displacement]);
                        default:
                            return Memory[Get16BitRegisters(2)];
                    } 
                case 7: return A;
                default: Console.WriteLine("Tried to get an unknown register"); return 0;
            }
        }

        /// <summary>
        /// Sets a register to the given 8-bit value.
        /// </summary>
        /// <param name="register"></param>
        /// <param name="value"></param>
        private void SetRegister(int register, int value)
        {
            value = value & 0xff;
            switch (register)
            {
                case 0: B = value; return;
                case 1: C = value; return;
                case 2: D = value; return;
                case 3: E = value; return;
                case 4:
                    if (ignorePrefix)
                    {
                        H = value; return;
                    }
                    switch (prefix) {
                        case 0xDD:
                            IXH = value; return;
                        case 0xFD:
                            IYH = value; return;
                        default:
                            H = value; return;
                    }
                case 5:
                    if (ignorePrefix)
                    {
                        L = value; return;
                    }
                    switch (prefix)
                    {
                        case 0xDD:
                            IXL = value; return;
                        case 0xFD:
                            IYL = value; return;
                        default:
                            L = value; return;
                    }
                case 6:
                    CycleTStates += 3; 
                    switch (prefix)
                    {
                        case 0xDD:
                        case 0xFD:
                            CycleTStates +=4; Memory[Get16BitRegisters(2) + displacement] = value; return;
                        default:
                            Memory[Get16BitRegisters(2)] = value; return;
                    }
                case 7: A = value; return;
                default: Console.WriteLine("Tried to set an unknown register"); return;
            }
        }

        /// <summary>
        /// Sets a register pair to the given 16-bit value.
        /// </summary>
        /// <param name="registerPair"></param>
        /// <param name="value"></param>
        internal void Set16BitRegisters(int registerPair, int value)
        {
            Set16BitRegisters(registerPair, value & 0xFF, (value & 0xFFFF) >> 8);
        }

        /// <summary>
        /// Sets a register pair to the given low and high bytes.
        /// </summary>
        /// <param name="registerPair"></param>
        /// <param name="lowByte"></param>
        /// <param name="highByte"></param>
        internal void Set16BitRegisters(int registerPair, int lowByte, int highByte)
        {
            switch (registerPair)
            {
                case 0: B = highByte; C = lowByte; return;
                case 1: D = highByte; E = lowByte; return;
                case 2:
                    switch (prefix)
                    {
                        case 0xDD:
                            IXH = highByte; IXL = lowByte; return;
                        case 0xFD:
                            IYH = highByte; IYL = lowByte; return;
                        default:
                            H = highByte; L = lowByte; return;
                    }
                case 3: SP = highByte; SP = (SP << 8); SP += lowByte; return;
                default: Console.WriteLine("Tried to set an unknown Register pair."); return;
            }
        }

        /// <summary>
        /// Gets the 16-bit value stored in a register pair.
        /// </summary>
        /// <param name="registerPair"></param>
        /// <returns></returns>
        internal int Get16BitRegisters(int registerPair, bool ignorePrefix = false)
        {
            int result;
            switch (registerPair)
            {
                case 0: result = B; result = (result << 8); result += C; return result;
                case 1: result = D; result = (result << 8); result += E; return result;
                case 2:
                    if (ignorePrefix)
                    {
                        result = H; result = (result << 8); result += L; return result;
                    }
                    switch (prefix)
                    {
                        case 0xDD:
                             result = IXH; result = (result << 8); result += IXL; return result;
                        case 0xFD:
                            result = IYH; result = (result << 8); result += IYL; return result;
                        default:
                            result = H; result = (result << 8); result += L; return result;
                    }
                case 3: return SP;
                default: Console.WriteLine("Tried to get an unknown register pair"); return 0;
            }
        }

        /// <summary>
        /// Checks if the given condition is true.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private bool CheckCondition(int condition)
        {
            switch (condition)
            {
                case -1: return true;
                case 0: if ((F & Flag.Zero) == Flag.Zero) return false; return true;
                case 1: if ((F & Flag.Zero) == Flag.Zero) return true; return false;
                case 2: if ((F & Flag.Carry) == Flag.Carry) return false; return true;
                case 3: if ((F & Flag.Carry) == Flag.Carry) return true; return false;
                case 4: if ((F & Flag.ParityOverflow) == Flag.ParityOverflow) return false; return true;
                case 5: if ((F & Flag.ParityOverflow) == Flag.ParityOverflow) return true; return false;
                case 6: if ((F & Flag.Sign) == Flag.Sign) return false; return true;
                case 7: if ((F & Flag.Sign) == Flag.Sign) return true; return false;
                default: Console.WriteLine("Unknown condition code."); HALT(); return false;
            }
        }

    }
}
