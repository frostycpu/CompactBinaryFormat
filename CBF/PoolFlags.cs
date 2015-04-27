using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF
{
    enum PoolFlags:byte
    {
        HasStrings = 0x01,
        HasFloats = 0x02,
        HasDoubles = 0x04,
        HasDecimals = 0x08,
        HasDates = 0x10,
        HasTypes = 0x20,
    }
}
