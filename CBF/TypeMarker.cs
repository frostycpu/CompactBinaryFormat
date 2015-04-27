using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF
{
    enum TypeMarker:byte
    {
        Null=0,
        SByte,
        Byte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Decimal,
        Bool,
        Char,
        String,
        Date,
        Array,
        IList,
        IDictionary,
        Dynamic,
        Object
    }

    static class TypeMarkerUtil
    {
        public static bool IsFinal(TypeMarker type)
        {
            return !(type == TypeMarker.Array || type == TypeMarker.IList || type == TypeMarker.IDictionary || type == TypeMarker.Dynamic || type == TypeMarker.Object);
        }
    }
}
