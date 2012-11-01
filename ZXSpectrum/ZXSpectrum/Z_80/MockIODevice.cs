using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXSpectrum.Z_80
{
    /// <summary>
    /// This is a mock I/O device for testing purposes.
    /// </summary>
    public class MockIODevice : IOController
    {
        int[] numbers = {0xc1, 0xc1, 0xc1, 0xc1, 0x29, 0x7d, 0xbb, 0x40, 0x0d, 0x62, 0xf7, 0xf2, 
                            0x9a, 0x02, 0x56, 0xab, 0xd7, 0x01, 0x56, 0xab, 0x0a, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 
                            0x01, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01};
        int currentNumber = 0;
        public void Write(int port, int dataByte)
        {
            Console.WriteLine(dataByte + " was written to port " + port);
        }

        public int Read(int port)
        {
            int a = numbers[currentNumber++];
            Console.WriteLine(a + " was read from port " + port);
            return a;
        }
    }
}
