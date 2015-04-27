using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBF.Binary
{
    class Pool<T>:IEnumerable<T>
    {
        Dictionary<T, int> dict;
        List<T> list;

        public int Count { get { return dict.Count; } }

        public Pool()
        {
            dict = new Dictionary<T, int>();
            list = new List<T>();
        }
        public Pool(int capacity)
        {
            dict = new Dictionary<T, int>(capacity);
            list = new List<T>(capacity);
        }

        public int GetReferenceId(T value)
        {
            if (dict.ContainsKey(value))
                return dict[value];


            int f = dict.Count;
            dict.Add(value, f);
            list.Insert(f, value);
            return f;
        }

        public int GetReferenceIdIfAvailable(T value)
        {

            if (dict.ContainsKey(value))
                return dict[value];
            else
                return -1;
        }

        public T GetValue(uint index)
        {
            return list[(int)index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
