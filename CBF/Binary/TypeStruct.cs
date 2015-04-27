using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    struct TypeStruct
    {
        public uint TypeName;
        public uint AssemblyName;
        public bool IsValueType;
        public uint[] Members;

        public override bool Equals(object obj)
        {
            if (!(obj is TypeStruct)) return false;
            TypeStruct ts = (TypeStruct)obj;

            return TypeName == ts.TypeName && AssemblyName == ts.AssemblyName;
        }
    }


}
