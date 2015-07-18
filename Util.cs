using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staticar
{
    internal class Util
    {
        public static object Clone(object source)
        {
            var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, source);
                ms.Position = 0;
                var dest = bf.Deserialize(ms);
                return dest;
            }
        }

        public static object Deserialize(string filepath, System.Runtime.Serialization.SerializationBinder binder = null)
        {
            try
            {
                var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bf.Binder = binder;
                using (var fs = new FileStream(filepath, FileMode.Open))
                {
                    return bf.Deserialize(fs);
                }
            }
            catch (System.Exception ex)
            {
                throw new System.ApplicationException("Deserialization of '" + filepath + "' failed", ex);
            }
        }

        public static void Serialize(object obj, string filepath, System.Runtime.Serialization.SerializationBinder binder = null)
        {
            try
            {
                var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bf.Binder = binder;
                using (var fs = new FileStream(filepath, FileMode.Create))
                {
                    bf.Serialize(fs, obj);
                }
            }
            catch (System.Exception ex)
            {
                throw new System.ApplicationException("Serialization of '" + filepath + "' failed", ex);
            }
        }
    }
}