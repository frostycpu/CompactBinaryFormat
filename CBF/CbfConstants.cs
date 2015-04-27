using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF
{
    static class CbfConstants
    {
        public static readonly byte[] MagicNumber = new byte[] { 0x76, 0xDA, 0xFB, 0x80 };//0x76DAFB80
        public const byte Version = 2;
    }
}
