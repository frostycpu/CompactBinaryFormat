using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    struct ListStruct
    {
        public TypeMarker ElementType;
        public uint Type;
        public ValueStruct[] MemberValues;
        public ValueStruct[] Values;
    }
}
