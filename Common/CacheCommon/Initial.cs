using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZdCache.Common.CacheCommon
{
    public class CacheDataTypeList
    {
        /// <summary>
        /// 存储所有子类的类型： 类型  -- 标志
        /// </summary>
        public static Dictionary<string, byte> TypeList = new Dictionary<string, byte>();
        /// <summary>
        /// 存储所有子类的类型： 标志 -- 类型
        /// </summary>
        public static Dictionary<byte, string> TypeListReversed = new Dictionary<byte, string>();

        static CacheDataTypeList()
        {
            Type[] typeInfos = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type tpinfo in typeInfos)
            {
                if (tpinfo.IsClass)
                {
                    object[] rets = tpinfo.GetCustomAttributes(typeof(CacheDataTypeAttribute), false);
                    if (rets.Length > 0)
                    {
                        string key = tpinfo.FullName.ToString();
                        byte value = ((CacheDataTypeAttribute)rets[0]).Key;

                        TypeList.Add(key, value);
                        TypeListReversed.Add(value, key);
                    }
                }
            }
        }
    }
}
