using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Diagnostics;

namespace ZdCache.Common
{
    public class Function
    {
        /// <summary>
        /// byte 数组转换为 hex string
        /// </summary>
        public static string GetHexStringFromBytes(List<byte[]> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte[] singleData in data)
            {
                for (int i = 0; i < singleData.Length; i++)
                {
                    sb.Append(singleData[i].ToString("X2"));
                }
            }
            return sb.ToString();
        }

        public static string GetHexStringFromBytes(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 返回32位整形转byte数组，返回4个长度字节数组
        /// </summary>
        public static byte[] GetBytesFormInt32(Int32 val)
        {
            return BitConverter.GetBytes(val);
        }

        /// <summary>
        /// 从byte数组中获取int32
        /// </summary>
        public static Int32 GetInt32FromBytes(byte[] array, int startIndex)
        {
            byte[] x1 = new byte[4];
            Array.Copy(array, startIndex, x1, 0, 4);
            return BitConverter.ToInt32(x1, 0);
        }

        /// <summary>
        /// 获取字符串的bytes 数组
        /// </summary>
        public static byte[] GetBytesFromStr(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// 从 bytes 数组获取字符串
        /// </summary>
        public static string GetStrFromBytes(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// 从 bytes 数组获取字符串
        /// </summary>
        public static string GetStrFromBytes(byte[] data, int index, int count)
        {
            return Encoding.UTF8.GetString(data, index, count);
        }

        #region SerializableObj

        /// <summary>
        /// 序列化
        /// </summary>
        public static List<byte[]> GetBytesFromSerializableObj(object obj, int bufferBlockSize, ref long totalSize)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            totalSize = 0;
            try
            {
                bf.Serialize(ms, obj);
                ms.Position = 0;
                totalSize = ms.Length;
                return GetListByteFormStream(ms, bufferBlockSize);
            }
            finally
            {
                ms.Dispose();
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public static object GetSerializableObjFromBytes(List<byte[]> data, ref long totalSize)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            totalSize = 0;
            try
            {
                foreach (byte[] singleB in data)
                {
                    totalSize += (long)singleB.Length;
                    ms.Write(singleB, 0, singleB.Length);
                }
                ms.Position = 0;
                return bf.Deserialize(ms);
            }
            finally
            {
                ms.Dispose();
            }
        }

        /// <summary>
        /// 分块读取 stream 中的内容
        /// </summary>
        private static List<byte[]> GetListByteFormStream(Stream stream, int bufferBlockSize)
        {
            List<byte[]> retList = new List<byte[]>();
            long totalReadCount = 0;

            while (totalReadCount < stream.Length)
            {
                byte[] data = new byte[bufferBlockSize];
                int byteCount = stream.Read(data, 0, bufferBlockSize);
                totalReadCount += byteCount;

                if (byteCount < bufferBlockSize)
                {
                    byte[] dataLast = new byte[byteCount];
                    Array.Copy(data, dataLast, byteCount);
                    retList.Add(dataLast);
                }
                else
                    retList.Add(data);
            }
            return retList;
        }

        #endregion
    }
}
