using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CBF.Binary;

namespace CBF
{
    class CbfReader
    {
        
        static Dictionary<Type, Func<object, object>> dictAddCache = new Dictionary<Type, Func<object, object>>();
        static Dictionary<Type, Func<object, int>> listAddCache = new Dictionary<Type, Func<object, int>>();

        BinaryReader reader;

        List<string> strings;
        List<float> floats;
        List<double> doubles;
        List<decimal> decimals;
        List<DateTime> dates;
        List<TypeStruct> typeStructs;
        List<Type> types;

        internal CbfReader(Stream input, bool autoClose=true)
        {
            reader = new BinaryReader(input);
        }

        internal CbfReader(string filename) : this(File.OpenRead(filename)) { }

        internal object Read()
        {
            byte[] magic=reader.ReadBytes(4);
            for(int i=0;i<magic.Length;i++)
            {
                if (magic[i] != CbfConstants.MagicNumber[i])
                    throw new NotSupportedException("File format not supported.");
            }
            byte version = reader.ReadByte();
            if (version != CbfConstants.Version)
                throw new NotSupportedException("File version <" + version + "> is not supported. (Supported version: " + CbfConstants.Version + ")");

            PoolFlags flags = (PoolFlags) reader.ReadByte();

            bool hasStrings, hasFloats, hasDoubles, hasDecimals, hasDates, hasTypes;
            hasStrings = flags.HasFlag(PoolFlags.HasStrings);
            hasFloats = flags.HasFlag(PoolFlags.HasFloats);
            hasDoubles = flags.HasFlag(PoolFlags.HasDoubles);
            hasDecimals = flags.HasFlag(PoolFlags.HasDecimals);
            hasDates = flags.HasFlag(PoolFlags.HasDates);
            hasTypes = flags.HasFlag(PoolFlags.HasTypes);

            if (hasStrings) strings = new List<string>((int)ReadVInt());
            if (hasFloats) floats = new List<float>((int)ReadVInt());
            if (hasDoubles) doubles = new List<double>((int)ReadVInt());
            if (hasDecimals) decimals = new List<decimal>((int)ReadVInt());
            if (hasDates) dates = new List<DateTime>((int)ReadVInt());
            if (hasTypes)
            {
                int typelen=(int)ReadVInt();
                typeStructs = new List<TypeStruct>(typelen);
                types = new List<Type>(typelen);
            }

            if (hasStrings)
                for (int i = 0; i < strings.Capacity; i++)
                    strings.Add(ReadString());
            if (hasFloats)
                for (int i = 0; i < floats.Capacity; i++)
                    floats.Add(reader.ReadSingle());
            if (hasDoubles)
                for (int i = 0; i < doubles.Capacity; i++)
                    doubles.Add(reader.ReadDouble());
            if (hasDecimals)
                for (int i = 0; i < decimals.Capacity; i++)
                    decimals.Add(reader.ReadDecimal());
            if (hasDates)
                for (int i = 0; i < dates.Capacity; i++)
                    dates.Add(new DateTime(ReadVSInt()));
            if (hasTypes)
                for (int i = 0; i < types.Capacity; i++)
                {
                    TypeStruct t = ReadTypeStruct();
                    typeStructs.Add(t);
                    types.Add(GetTypeByName(strings[(int)t.TypeName], strings[(int)t.AssemblyName]));
                }
            return ReadValue();
        }

        ulong ReadVInt()
        {
            ulong val = 0;
            for(int i=0;i<9;i++)
            {
                byte b = reader.ReadByte();
                byte mask = (byte)(i == 8 ? 0xFF : 0x7F);
                val |= (ulong)(b & mask) << (i * 7);
                if ((b & 0x80) == 0)
                    break;
            }
            return val;
        }

        long ReadVSInt()
        {
            long val = 0;
            for(int i=0;i<9;i++)
            {
                byte b = reader.ReadByte();

                byte mask = (byte)(i == 8 ? 0xFF : 0x7F);
                val |= (long)(b & mask) << (i * 7);
                if (i != 8 && (b & 0x80) == 0)
                {
                    byte shift = (byte)((7 - i) * 8 + i+1);
                    val = val<<shift>>shift;
                    break;
                }
            }
            return val;
        }

        string ReadString()
        {
            int i=(int)ReadVInt();
            return Encoding.UTF8.GetString(reader.ReadBytes(i));
        }

        TypeStruct ReadTypeStruct()
        {
            TypeStruct t = new TypeStruct();
            t.TypeName = (uint)ReadVInt();
            t.AssemblyName = (uint) ReadVInt();
            t.IsValueType = reader.ReadBoolean();
            t.Members = new uint[(int)ReadVInt()];
            for(int i=0;i<t.Members.Length;i++)
            {
                t.Members[i] = (uint)ReadVInt();
            }
            return t;
        }

        object ReadValue()
        {
            return ReadValue((TypeMarker)reader.ReadByte());
        }

        object ReadValue(TypeMarker t)
        {
            switch(t)
            {
                case TypeMarker.Null:
                    return null;
                case TypeMarker.SByte:
                    return (sbyte)reader.ReadSByte();
                case TypeMarker.Byte:
                    return reader.ReadByte();
                case TypeMarker.Short:
                    return (short)ReadVSInt();
                case TypeMarker.UShort:
                    return (ushort)ReadVInt();
                case TypeMarker.Int:
                    return (int)ReadVSInt();
                case TypeMarker.UInt:
                    return (uint)ReadVInt();
                case TypeMarker.Long:
                    return (long)ReadVSInt();
                case TypeMarker.ULong:
                    return (ulong)ReadVInt();
                case TypeMarker.Float:
                    return floats[(int)ReadVInt()];
                case TypeMarker.Double:
                    return doubles[(int)ReadVInt()];
                case TypeMarker.Decimal:
                    return decimals[(int)ReadVInt()];
                case TypeMarker.Bool:
                    return reader.ReadBoolean();
                case TypeMarker.Char:
                    return (char)ReadVInt();
                case TypeMarker.String:
                    return strings[(int)ReadVInt()];
                case TypeMarker.Date:
                    return dates[(int)ReadVInt()];
                case TypeMarker.Array:
                    return ReadArray();
                case TypeMarker.IList:
                    return ReadList();
                case TypeMarker.IDictionary:
                    return ReadDictionary();
                case TypeMarker.Object:
                    return ReadObject();
                default:
                    throw new NotSupportedException("Type <" + t + "> is not supported");
            }
        }

        Array ReadArray()
        {
            TypeMarker elemt = (TypeMarker)reader.ReadByte();
            Type t = types[(int)ReadVInt()];
            Array arr = Array.CreateInstance(t, (int)ReadVInt());
            if (TypeMarkerUtil.IsFinal(elemt))
                for (int i = 0; i < arr.Length; i++)
                    arr.SetValue(ReadValue(elemt), i);
            else
                for (int i = 0; i < arr.Length; i++)
                    arr.SetValue(ReadValue(), i);
            return arr;
        }

        object ReadList()
        {
            
            object ret = ReadObject();

            Type t = ret.GetType().GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>)).First();

            DynamicMethodDelegate dmd = DynamicMethodDelegateFactory.CreateDelegate(t.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, t.GetGenericArguments(), null));

            TypeMarker elemt = (TypeMarker)reader.ReadByte();
            int len = (int)ReadVInt();
            if (TypeMarkerUtil.IsFinal(elemt))
                for (int i = 0; i < len; i++)
                    dmd(ret, new[] { ReadValue(elemt) });
            else
                for (int i = 0; i < len; i++)
                    dmd(ret, new[] { ReadValue() });
            return ret;
        }

        object ReadDictionary()
        {

            object ret = ReadObject();

            Type t=ret.GetType().GetInterfaces().Where(x=>x.IsGenericType&&x.GetGenericTypeDefinition()==typeof(IDictionary<,>)).First();

            DynamicMethodDelegate dmd = DynamicMethodDelegateFactory.CreateDelegate(t.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, t.GetGenericArguments(), null));

            int len = (int)ReadVInt();
            object[] keys=new object[len];
            for (int i = 0; i < len; i++)
                keys[i] = ReadValue();
            for (int i = 0; i < len; i++)
                dmd(ret,new[]{keys[i], ReadValue()});
            return ret;
        }

        object ReadObject()
        {
            int typeref = (int)ReadVInt();
            TypeStruct ts = typeStructs[typeref];
            Type t = types[typeref];

            object obj= Activator.CreateInstance(t);

            foreach(var x in ts.Members)
            {
                var name=strings[(int)x];
                var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                    prop.SetValue(obj, ReadValue());
                else
                    t.GetField(name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj,ReadValue());
            }
            return obj;
        }

        Type GetTypeByName(string typename, string assemblyname)
        {
            return Type.GetType(typename + ", " + assemblyname,
                                ResolveAssembly,
                                (assem, name, ignore) => assem == null ? Type.GetType(name,ResolveAssembly,null) : assem.GetType(name));
        }

        Assembly ResolveAssembly(AssemblyName name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == name.FullName);
        }
        
    }
}
