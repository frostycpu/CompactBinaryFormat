using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    struct DynamicStruct
    {
        public uint Type;
        public ValueStruct[] MemberValues;
        public uint[] DynamicMembers;
        public ValueStruct[] DynamicMemberValues;
    }
}
