using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXSpectrum.Z_80;
using System.IO;

namespace ZXSpectrum
{
    /// <summary>
    /// This interface defines an emulation of ROM/RAM for a Z80.
    /// </summary>
    public interface Memory
    {
        //  Indexer declaration: 
        int this[int index, bool priority=false]
        {
            get;
            set;
        }
        //  Length of array == size of memory
        int Length {get;}
        //  The CPU
        Z80 CPU { get; set; }
    }

    /// <summary>
    /// Event args for a beep (loudspeaker event)
    /// </summary>
    public class BeepEventArgs : EventArgs
    {
        public int DE;
        public int HL;

        public BeepEventArgs(int de, int hl)
        {
            DE = de;
            HL = hl;
        }
    }

    /// <summary>
    /// Represents the ZX Spectrum 48K's ROM + RAM.
    /// This implementation corrects the Z80 emulator's timing values for the Spectrum ULA's memory contention effect.
    /// More information @ http://www.worldofspectrum.org/faq/reference/48kreference.htm#Contention
    /// </summary>
    public class Memory48K : Memory
    {
        //  Underlying memory array
        internal int[] mem = new int[65536];

        private int romStart = 0, romEnd = 16383, contentionStart = 0x4000, contentionEnd = 0x7fff;

        /// <summary>
        /// Constructor - empty memory (no ROM).
        /// </summary>
        public Memory48K()
        { }

        /// <summary>
        /// Constructor - loads the given ROM.
        /// </summary>
        /// <param name="romPath"></param>
        public Memory48K(string romPath)
        {
            LoadROM(romPath);
        }
        
        /// <summary>
        /// Indexers for accessing underlying memory.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int this[int index, bool ulaAccess=false]
        {
            get
            {
                if (!ulaAccess) CorrectForContention(index);
                return mem[index];
            }
            set
            {
                ////  Ignore attempts to write to ROM
                //if (romStart <= index && index <= romEnd)
                //{
                //    return;
                //}
                if (!ulaAccess) CorrectForContention(index);
                mem[index] = value;
            }
        }

        /// <summary>
        /// Performs any adjustments necessary to Z80 timing due to contention.
        /// </summary>
        /// <param name="index"></param>
        private void CorrectForContention(int index)
        {
            if (CPU == null) return;
            if (contentionStart <= index && index <= contentionEnd)
            {
                if (CPU.CycleTStates >= 14335 && CPU.CycleTStates < 57343)
                {
                    int line = (CPU.CycleTStates - 14335) / 224;
                    if (line < 192)
                    {
                        int pos = (CPU.CycleTStates - 14335) % 224;
                        int delay = 6 - (pos % 8);
                        if (delay < 0) delay = 0;
                        CPU.CycleTStates += delay;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the size of the memory in bytes.
        /// </summary>
        public int Length { get { return mem.Length; } }

        /// <summary>
        /// Loads a ROM file into low memory.
        /// </summary>
        internal void LoadROM(string romPath)
        {
            using (FileStream fs = File.Open(romPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] rom = br.ReadBytes((int)fs.Length);
                    for (int i = 0; i < rom.Length; i++)
                    {
                        mem[i] = rom[i];
                    }
                }
            }
        }

        /// <summary>
        /// CPU getter / setter
        /// </summary>
        private Z80 _cpu;
        public Z80 CPU { get { return _cpu; } set { _cpu = value; } }
    }
}
