using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common.CacheCommon
{
    /// <summary>
    /// 数据类型标识属性。只用于类，并不能被继承（因为需要对每个 DataType 进行单独处理，保证Key的唯一性）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CacheDataTypeAttribute : Attribute
    {
        private static List<byte> allKeys = new List<byte>();
        private byte key;

        public CacheDataTypeAttribute(byte keyValue)
        {
            if (allKeys.Contains(keyValue))
                throw new Exception("key不唯一，存在冲突!");
            else
            {
                this.key = keyValue;
                allKeys.Add(keyValue);
            }
        }

        public byte Key
        {
            get { return this.key; }
        }
    }
}
