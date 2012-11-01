using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXSpectrum.Z_80
{
    public partial class Z80
    {
        /// <summary>
        /// Console printout of a page of memory.
        /// </summary>
        public void PrintMemory(int page)
        {
            Console.WriteLine("Memory: Page " + page);
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                    Console.Write(Memory[(page * 256) + i * 16 + j].ToString("D3") + " ");

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Console printout of the current register state.
        /// </summary>
        public void PrintRegisters()
        {
            Console.WriteLine("Registers: ");
            Console.WriteLine("A'    SZ5H3PNC'  B'  C'    D'  E'    H'  L'    IX     IY");
            Console.Write(A2.ToString("D3") + "   ");
            if (F2.HasFlag(Flag.Sign))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.Zero))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.F5))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.HalfCarry))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.F3))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.ParityOverflow))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.Subtract))
                Console.Write(1);
            else
                Console.Write(0);
            if (F2.HasFlag(Flag.Carry))
                Console.Write(1);
            else
                Console.Write(0);
            Console.Write("   ");
            Console.Write(B2.ToString("D3") + " ");
            Console.Write(C2.ToString("D3") + "   ");
            Console.Write(D2.ToString("D3") + " ");
            Console.Write(E2.ToString("D3") + "   ");
            Console.Write(H2.ToString("D3") + " ");
            Console.Write(L2.ToString("D3") + "   ");
            Console.WriteLine((IXH * 256 + IXL).ToString("D5") + "  " + (IYH * 256 + IYL).ToString("D5") + "\n");
            Console.WriteLine("A     SZ5H3PNC   B   C     D   E     H   L     PC     SP");
            Console.Write(A.ToString("D3") + "   ");
            if (F.HasFlag(Flag.Sign))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.Zero))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.F5))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.HalfCarry))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.F3))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.ParityOverflow))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.Subtract))
                Console.Write(1);
            else
                Console.Write(0);
            if (F.HasFlag(Flag.Carry))
                Console.Write(1);
            else
                Console.Write(0);
            Console.Write("   "); ;
            Console.Write(B.ToString("D3") + " ");
            Console.Write(C.ToString("D3") + "   ");
            Console.Write(D.ToString("D3") + " ");
            Console.Write(E.ToString("D3") + "   ");
            Console.Write(H.ToString("D3") + " ");
            Console.Write(L.ToString("D3") + "   ");
            Console.Write(PC.ToString("D5") + "  ");
            Console.WriteLine(SP.ToString("D5") + "\n");
        }

    }
}
