using System;

namespace HBus.Nodes.Common
{
    public class Serializer<T>
    {
        public static byte[] Serialize(T instance)
        {
            throw new NotImplementedException();
        }

        public static T DeSerialize(byte[] stream,ref T instance, int index = 0)
        {
            throw new NotImplementedException();
        }
    }
}