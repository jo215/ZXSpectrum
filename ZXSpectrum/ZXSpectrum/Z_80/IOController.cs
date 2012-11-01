using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXSpectrum.Z_80
{
    public interface IOController
    {
        void Write(int port, int dataByte);
        int Read(int port);
    }
}
