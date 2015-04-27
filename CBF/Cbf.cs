using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF
{
    public class Cbf
    {
        public static int Version { get { return CbfConstants.Version; } }

        public static T Read<T>(byte[] data)
        {
            return Read<T>(new MemoryStream(data));
        }

        public static T ReadFile<T>(string file)
        {
            return Read<T>(File.OpenRead(file), true);
        }

        public static T Read<T>(Stream stream, bool autoClose = false)
        {
            return (T)Convert.ChangeType(new CbfReader(stream, autoClose).Read(), typeof(T));
        }

        public static object Read(byte[] data)
        {
            return Read(new MemoryStream(data));
        }

        public static object ReadFile(string file)
        {
            return Read(File.OpenRead(file), true);
        }

        public static object Read(Stream stream, bool autoClose = false)
        {
            return new CbfReader(stream, autoClose).Read();
        }
        
        public static byte[] Write(object o)
        {
            MemoryStream ms=new MemoryStream();
            Write(ms, o);
            return ms.ToArray();
        }

        public static void WriteFile(string file, object o)
        {
            new CbfWriter(file, o).Write();
        }

        public static void Write(Stream str, object o,bool autoClose=false)
        {
            new CbfWriter(str, o, autoClose).Write();
        }
    }
}
