using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CBF.Binary;

namespace CBF
{
    class CbfWriter
    {

        object obj;
        BinaryWriter writer;
        bool autoClose;

        Pool<string> strings;
        Pool<float> floats;
        Pool<double> doubles;
        Pool<decimal> decimals;
        Pool<DateTime> dates;
        Pool<TypeStruct> types;

        ValueStruct val;

        public CbfWriter(string filename, object obj) : this(File.Open(filename, FileMode.Create), obj) { }

        public CbfWriter(Stream stream, object obj, bool autoClose=true)
        {
            this.obj = obj;
            writer = new BinaryWriter(stream);
            this.autoClose = autoClose;
        }

        public void Close()
        {
            writer.Close();
        }

        public void Write()
        {
            strings = new Pool<string>();
            floats = new Pool<float>();
            doubles = new Pool<double>();
            decimals = new Pool<decimal>();
            dates=new Pool<DateTime>();
            types = new Pool<TypeStruct>();
            Convert();

            writer.Write(CbfConstants.MagicNumber);
            writer.Write(CbfConstants.Version);
            PoolFlags flags=GetFlags();
            writer.Write((byte)flags);

            if (flags.HasFlag(PoolFlags.HasStrings))
                WriteVSInt(strings.Count);
            if (flags.HasFlag(PoolFlags.HasFloats))
                WriteVSInt(floats.Count);
            if (flags.HasFlag(PoolFlags.HasDoubles))
                WriteVSInt(doubles.Count);
            if (flags.HasFlag(PoolFlags.HasDecimals))
                WriteVSInt(decimals.Count);
            if (flags.HasFlag(PoolFlags.HasDates))
                WriteVSInt(dates.Count);
            if (flags.HasFlag(PoolFlags.HasTypes))
                WriteVSInt(types.Count);


            if (flags.HasFlag(PoolFlags.HasStrings))
                foreach (var elem in strings)
                    WriteString(elem);
            if (flags.HasFlag(PoolFlags.HasFloats))
                foreach (var elem in floats)
                    writer.Write(elem);
            if (flags.HasFlag(PoolFlags.HasDoubles))
                foreach (var elem in doubles)
                    writer.Write(elem);
            if (flags.HasFlag(PoolFlags.HasDecimals))
                foreach (var elem in decimals)
                    writer.Write(elem);
            if (flags.HasFlag(PoolFlags.HasDates))
                foreach (var elem in dates)
                    WriteVSInt(elem.Ticks);
            if (flags.HasFlag(PoolFlags.HasTypes))
                foreach (var elem in types)
                    WriteType(elem);

            WriteValue(val);
            if (autoClose)
                writer.Close();
        }

        void WriteVInt(ulong v)
        {
            ulong temp = v;
            for (int i = 0; i < 9; i++)
            {
                if (temp == 0 && i != 0) break;
                if (i == 8)
                {
                    writer.Write((byte)temp);
                    break;
                }
                byte b7 = (byte)((temp & 0x7F) | (byte)(temp >> 7 == 0 ? 0 : 0x80));
                temp >>= 7;
                writer.Write((byte)(b7 | (temp == 0 ? 0 : 0x80)));
            }
        }

        void WriteVSInt(long v)
        {
            ulong temp = (ulong)v;
            if (v >= 0)
                for (int i = 0; i < 9; i++)
                {
                    if (i == 8)
                    {
                        writer.Write((byte)temp);
                        break;
                    }
                    byte b6 = (byte)(temp & 0x7F);
                    temp >>= 7;
                    bool c = (temp == 0 && (b6 & 0x40) == 0x40) || temp != 0;
                    if (c)
                        b6 |= 0x80;
                    writer.Write(b6);
                    if (!c)
                        break;
                }
            else
                for (int i = 0; i < 9; i++)
                {
                    if (i == 8)
                    {
                        writer.Write((byte)v);
                        break;
                    }
                    byte b6 = (byte)(v & 0x7F);
                    v >>= 7;
                    bool c = ((b6 & 0x40) != 0x40) || v != -1;
                    if (c)
                        b6 |= 0x80;
                    writer.Write(b6);
                    if (!c)
                        break;
                }
        }

        void WriteString(string s)
        {
            byte[] b = Encoding.UTF8.GetBytes(s);
            WriteVInt((uint)b.Length);
            writer.Write(b);
        }

        void WriteType(TypeStruct type)
        {
            WriteVInt(type.TypeName);
            WriteVInt(type.AssemblyName);
            writer.Write(type.IsValueType);
            WriteVInt((uint)type.Members.Length);
            foreach (var x in type.Members)
                WriteVInt(x);
        }

        void WriteValue(ValueStruct ts, bool writetype = true)
        {
            if(writetype)
                writer.Write((byte)ts.Type);
            switch (ts.Type)
            {
                case TypeMarker.Null:
                    break;
                case TypeMarker.SByte:
                    writer.Write((sbyte)ts.Value);
                    break;
                case TypeMarker.Byte:
                    writer.Write((byte)ts.Value);
                    break;
                case TypeMarker.Short:
                    WriteVSInt((short)ts.Value);
                    break;
                case TypeMarker.UShort:
                    WriteVInt((ushort)ts.Value);
                    break;
                case TypeMarker.Int:
                    WriteVSInt((int)ts.Value);
                    break;
                case TypeMarker.UInt:
                    WriteVInt((uint)ts.Value);
                    break;
                case TypeMarker.Long:
                    WriteVSInt((long)ts.Value);
                    break;
                case TypeMarker.ULong:
                    WriteVInt((ulong)ts.Value);
                    break;
                case TypeMarker.Float:
                case TypeMarker.Double:
                case TypeMarker.Decimal:
                case TypeMarker.String:
                case TypeMarker.Date:
                    WriteVInt((uint)ts.Value);
                    break;
                case TypeMarker.Bool:
                    writer.Write((bool)ts.Value);
                    break;
                case TypeMarker.Char:
                    WriteVInt((char)ts.Value);
                    break;
                case TypeMarker.Array:
                    WriteArray((ArrayStruct)ts.Value);
                    break;
                case TypeMarker.IList:
                    WriteList((ListStruct)ts.Value);
                    break;
                case TypeMarker.IDictionary:
                    WriteDictionary((DictionaryStruct)ts.Value);
                    break;
                case TypeMarker.Object:
                    WriteObject((ObjectStruct)ts.Value);
                    break;
            }
        }

        void WriteArray(ArrayStruct array)
        {
            TypeStruct ts = types.GetValue(array.ElementType);
            writer.Write((byte)array.NativeElementType);
            WriteVInt(array.ElementType);
            WriteVInt((uint)array.Values.Length);
            bool b = !TypeMarkerUtil.IsFinal(array.NativeElementType);
            foreach(var x in array.Values)
            {
                WriteValue(x, b);
            }
        }

        void WriteList(ListStruct list)
        {
            bool b = !TypeMarkerUtil.IsFinal(list.ElementType);
            WriteVInt(list.Type);
            foreach(var m in list.MemberValues)
            {
                WriteValue(m);
            }
            writer.Write((byte)list.ElementType);
            WriteVInt((uint)list.Values.Length);
            foreach (var m in list.Values)
            {
                WriteValue(m, b);
            }
        }

        void WriteDictionary(DictionaryStruct dict)
        {
            WriteVInt(dict.Type);
            foreach (var m in dict.MemberValues)
            {
                WriteValue(m);
            }

            WriteVInt((uint)dict.Keys.Length);
            foreach (var m in dict.Keys)
            {
                WriteValue(m);
            }
            foreach (var m in dict.Values)
            {
                WriteValue(m);
            }
        }

        void WriteObject(ObjectStruct obj)
        {
            WriteVInt(obj.Type);
            foreach (var m in obj.Values)
            {
                WriteValue(m);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlag(ushort flags, ushort flag)
        {
            return (flags & flag) == flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        PoolFlags GetFlags()
        {
            return (strings.Count==0?0:PoolFlags.HasStrings)
                 | (floats.Count==0?0:PoolFlags.HasFloats)
                 | (doubles.Count==0?0:PoolFlags.HasDoubles)
                 | (decimals.Count==0?0:PoolFlags.HasDecimals)
                 | (dates.Count==0?0:PoolFlags.HasDates)
                 | (types.Count==0?0:PoolFlags.HasTypes);
        }

        void Convert()
        {
            val = Convert(obj);
        }

        ValueStruct Convert(object obj)
        {
            ValueStruct val = new ValueStruct();
            val.Type=GetTypeMarker(obj);
            val.Value = GetValue(obj,val.Type);
            return val;
        }

        object GetValue(object obj,TypeMarker type)
        {
            switch (type)
            {
                case TypeMarker.Null:
                    return null;
                case TypeMarker.SByte:
                case TypeMarker.Byte:
                case TypeMarker.Short:
                case TypeMarker.UShort:
                case TypeMarker.Bool:
                case TypeMarker.Char:
                case TypeMarker.Int:
                case TypeMarker.UInt:
                case TypeMarker.Long:
                case TypeMarker.ULong:
                    return obj;
                case TypeMarker.Float:
                    return GetFloatRef((float)obj);
                case TypeMarker.Double:
                    return GetDoubleRef((double)obj);
                case TypeMarker.Decimal:
                    return GetDecimalRef((decimal)obj);
                case TypeMarker.String:
                    return GetStringRef((string)obj);
                case TypeMarker.Date:
                    return GetDateRef((DateTime) obj);
                case TypeMarker.Array:
                    return ConvertArray((Array)obj);
                case TypeMarker.IList:
                    return ConvertList((IEnumerable)obj);
                case TypeMarker.IDictionary:
                    return ConvertDictionary(obj);
                case TypeMarker.Dynamic:
                    throw new NotSupportedException("DynamicObjects are not supported");
                case TypeMarker.Object:
                    return ConvertObject(obj);
                default:
                    throw new NotSupportedException("Type <" + type + "> is not supported");
            }
        }


        uint GetFloatRef(float val)
        {
            return (uint)floats.GetReferenceId(val);
        }

        uint GetDoubleRef(double val)
        {
            return (ushort)doubles.GetReferenceId(val);
        }

        uint GetDecimalRef(decimal val)
        {
            return (uint)decimals.GetReferenceId(val);
        }

        uint GetStringRef(string val)
        {
            return (uint)strings.GetReferenceId(val);
        }

        uint GetDateRef(DateTime date)
        {
            return (uint)dates.GetReferenceId(date);
        }

        uint GetTypeRef(Type t)
        {
            TypeStruct ts = new TypeStruct();
            ts.AssemblyName = GetStringRef(t.Assembly.FullName);
            ts.TypeName = GetStringRef(t.FullName);
            ts.IsValueType = t.IsValueType;
            int id = types.GetReferenceIdIfAvailable(ts);
            List<string> names=new List<string>();
            if(id<0)
            {
                foreach (var pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if(pi.CanRead&&pi.CanWrite&&!pi.IsDefined(typeof(NonSerializedAttribute))&&pi.GetIndexParameters().Length==0)
                        names.Add(pi.Name);
                }
                foreach(var fi in t.GetFields(BindingFlags.Instance|BindingFlags.Public))
                {
                    if (!fi.IsInitOnly && !fi.IsNotSerialized && !fi.IsPinvokeImpl && !fi.IsLiteral)
                        names.Add(fi.Name);
                }
                ts.Members = new uint[names.Count];
                for(int i=0;i<names.Count;i++)
                {
                    ts.Members[i] = GetStringRef(names[i]);
                }
                id = types.GetReferenceId(ts);
            }
            checked
            {
                return (uint)id;
            }
        }

        ArrayStruct ConvertArray(Array arr)
        {
            ArrayStruct array = new ArrayStruct();
            Type t=arr.GetType().GetElementType();
            array.NativeElementType = GetTypeMarker(t);
            array.ElementType = GetTypeRef(t);
            array.Values=new ValueStruct[arr.Length];
            for(int i=0;i<arr.Length;i++)
            {
                array.Values[i] = Convert(arr.GetValue(i));
            }
            return array;
        }

        ListStruct ConvertList(IEnumerable obj)
        {
            Type t = obj.GetType();
            List<object> elements=obj.Cast<object>().ToList();
            ListStruct list = new ListStruct();
            list.Type = GetTypeRef(t);
            list.ElementType = GetTypeMarker(obj);
            list.Values=new ValueStruct[elements.Count];
            for(int i=0;i<elements.Count;i++)
            {
                list.Values[i] = Convert(elements[i]);
            }


            TypeStruct ts = types.GetValue(list.Type);
            list.MemberValues = new ValueStruct[ts.Members.Length];
            int slot = 0;
            foreach (var member in ts.Members)
            {
                string name = strings.GetValue(member);
                var p = t.GetProperty(name);
                if (p != null)
                    list.MemberValues[slot] = Convert(p.GetValue(obj));
                else
                    list.MemberValues[slot] = Convert(t.GetField(name).GetValue(obj));
                slot++;
            }

            return list;
        }



        DictionaryStruct ConvertDictionary(object obj)
        {
            DictionaryStruct dict = new DictionaryStruct();
            Type t=obj.GetType();
            dict.Type = GetTypeRef(t);

            ICollection keys = ((ICollection)t.GetProperty("Keys").GetValue(obj));
            PropertyInfo indexer = t.GetProperty("Item");

            //sometimes the indexer is not named "Item", we need to find it manually in that case
            if(indexer==null)
            {
                foreach(var property in t.GetProperties(BindingFlags.Public|BindingFlags.Instance))
                {
                    if(property.GetIndexParameters().Length==1)
                    {
                        indexer = property;
                        break;
                    }
                }
            }

            dict.Keys = new ValueStruct[keys.Count];
            dict.Values = new ValueStruct[keys.Count];

            int i = 0;
            foreach (var key in keys)
            {
                dict.Keys[i] = Convert(key);
                dict.Values[i] = Convert(indexer.GetValue(obj, new[]{key}));
                i++;
            }


            TypeStruct ts = types.GetValue(dict.Type);
            dict.MemberValues = new ValueStruct[ts.Members.Length];
            int slot = 0;
            foreach (var member in ts.Members)
            {
                string name = strings.GetValue(member);
                var p = t.GetProperty(name);
                if (p != null)
                    dict.MemberValues[slot] = Convert(p.GetValue(obj));
                else
                    dict.MemberValues[slot] = Convert(t.GetField(name).GetValue(obj));
                slot++;
            }

            return dict;
        }
        /*
        DynamicStruct ConvertDynamic(DynamicObject dyn)
        {
            Type t = dyn.GetType();
            DynamicStruct ret = new DynamicStruct();
            ret.Type = GetTypeRef(t);
            TypeStruct ts = types.GetValue(ret.Type);
            ret.MemberValues = new ValueStruct[ts.Members.Length];
            int slot = 0;
            foreach (var member in ts.Members)
            {
                string name = strings.GetValue(member);
                var p = t.GetProperty(name);
                if (p != null)
                {
                    ret.MemberValues[slot] = Convert(p.GetValue(dyn));
                }
                else
                    ret.MemberValues[slot] = Convert(t.GetField(name).GetValue(dyn));
                slot++;
            }
            string[] dynnames=dyn.GetDynamicMemberNames().ToArray();
            ret.DynamicMembers = new ushort[dynnames.Length];
            ret.DynamicMemberValues = new ValueStruct[dynnames.Length];
            for(int i=0;i<dynnames.Length;i++)
            {
                ret.DynamicMembers[i] = GetStringRef(dynnames[i]);
                
                object obj;

            }
            return ret;
        }
        */
        ObjectStruct ConvertObject(object dyn)
        {
            Type t = dyn.GetType();
            ObjectStruct ret = new ObjectStruct();
            ret.Type = GetTypeRef(t);
            TypeStruct ts = types.GetValue(ret.Type);
            ret.Values = new ValueStruct[ts.Members.Length];
            int slot = 0;
            foreach (var member in ts.Members)
            {
                string name = strings.GetValue(member);
                var p = t.GetProperty(name);
                if (p != null)
                    ret.Values[slot] = Convert(p.GetValue(dyn));
                else
                    ret.Values[slot] = Convert(t.GetField(name).GetValue(dyn));
                slot++;
            }
            return ret;
        }
        TypeMarker GetTypeMarker(Type t)
        {

            if (t == null)
                return TypeMarker.Null;
            else if (t == typeof(sbyte))
                return TypeMarker.SByte;
            else if (t == typeof(byte))
                return TypeMarker.Byte;
            else if (t == typeof(short))
                return TypeMarker.Short;
            else if (t == typeof(ushort))
                return TypeMarker.UShort;
            else if (t == typeof(int))
                return TypeMarker.Int;
            else if (t == typeof(uint))
                return TypeMarker.UInt;
            else if (t == typeof(long))
                return TypeMarker.Long;
            else if (t == typeof(ulong))
                return TypeMarker.ULong;
            else if (t == typeof(float))
                return TypeMarker.Float;
            else if (t == typeof(double))
                return TypeMarker.Double;
            else if (t == typeof(decimal))
                return TypeMarker.Decimal;
            else if (t == typeof(bool))
                return TypeMarker.Bool;
            else if (t == typeof(char))
                return TypeMarker.Char;
            else if (t == typeof(string))
                return TypeMarker.String;
            else if (t == typeof(DateTime))
                return TypeMarker.Date;
            else if (t.IsArray)
                return TypeMarker.Array;
            else if (t.GetInterfaces().Where(x => x.IsGenericType).Select(x => x.GetGenericTypeDefinition()).Any(x => x.Equals(typeof(IList<>))))
                return TypeMarker.IList;
            else if (t.GetInterfaces().Where(x => x.IsGenericType).Select(x => x.GetGenericTypeDefinition()).Any(x => x.Equals(typeof(IDictionary<,>))))
                return TypeMarker.IDictionary;
            else if (t == typeof(DynamicObject))
                return TypeMarker.Dynamic;
            else
                return TypeMarker.Object;
        }

        TypeMarker GetTypeMarker(object obj)
        {
            if (obj == null)
                return TypeMarker.Null;
            else if (obj is sbyte)
                return TypeMarker.SByte;
            else if (obj is byte)
                return TypeMarker.Byte;
            else if (obj is short)
                return TypeMarker.Short;
            else if (obj is ushort)
                return TypeMarker.UShort;
            else if (obj is int)
                return TypeMarker.Int;
            else if (obj is uint)
                return TypeMarker.UInt;
            else if (obj is long)
                return TypeMarker.Long;
            else if (obj is ulong)
                return TypeMarker.ULong;
            else if (obj is float)
                return TypeMarker.Float;
            else if (obj is double)
                return TypeMarker.Double;
            else if (obj is decimal)
                return TypeMarker.Decimal;
            else if (obj is bool)
                return TypeMarker.Bool;
            else if (obj is char)
                return TypeMarker.Char;
            else if (obj is string)
                return TypeMarker.String;
            else if (obj is DateTime)
                return TypeMarker.Date;
            else if (obj is Array)
                return TypeMarker.Array;
            else if (obj.GetType().GetInterfaces().Where(t => t.IsGenericType).Select(t => t.GetGenericTypeDefinition()).Any(t => t.Equals(typeof(IList<>))))
                return TypeMarker.IList;
            else if (obj.GetType().GetInterfaces().Where(t => t.IsGenericType).Select(t => t.GetGenericTypeDefinition()).Any(t => t.Equals(typeof(IDictionary<,>))))
                return TypeMarker.IDictionary;
            else if (obj is DynamicObject)
                return TypeMarker.Dynamic;
            else
                return TypeMarker.Object;
        }
    }
}
