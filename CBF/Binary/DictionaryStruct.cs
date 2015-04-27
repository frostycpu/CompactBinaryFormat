using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    struct DictionaryStruct
    {
        public uint Type;
        public ValueStruct[] MemberValues;
        public ValueStruct[] Keys;
        public ValueStruct[] Values;
    }
}
