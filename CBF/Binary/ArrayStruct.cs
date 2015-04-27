using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    struct ArrayStruct
    {
        public TypeMarker NativeElementType;
        public uint ElementType;
        public ValueStruct[] Values;
    }
}
