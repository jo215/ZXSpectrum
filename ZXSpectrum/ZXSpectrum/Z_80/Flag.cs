using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXSpectrum.Z_80
{
    /// <summary>
    /// Represents the flags (F) register on the Z80.
    /// </summary>
    [Flags]
    public enum Flag
    {
        Carry = 0x1,
        Subtract = 0x2,
        ParityOverflow = 0x4,
        F3 = 0x8,
        HalfCarry = 0x10,
        F5 = 0x20,
        Zero = 0x40,
        Sign = 0x80,
    }
}
