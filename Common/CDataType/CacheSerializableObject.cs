using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using ZdCache.Common.CacheCommon;

namespace ZdCache.Common.CDataType
{
    /// <summary>
    /// 可序列化的缓存类型
    /// </summary>
    [CacheDataTypeAttribute(2)]
    public class CacheSerializableObject : BaseCacheDataType
    {
        private object obj;

        /// <summary>
        /// 构造方法，构造用于查询的 CacheData
        /// </summary>
        /// <param name="identify"></param>
        public CacheSerializableObject(string identify)
            : base(identify)
        {
        }

        /// <summary>
        /// 构造方法，构造默认 category=0 的 CacheData
        /// </summary>
        /// <param name="identify"></param>
        /// <param name="serialzableObj"></param>
        public CacheSerializableObject(string identify, object serialzableObj)
            : base(identify)
        {
            this.obj = serialzableObj;
            this.data = Function.GetBytesFromSerializableObj(this.obj, ConstParams.BufferBlockSize, ref this.totalSize);
        }

        /// <summary>
        /// 构造方法，构造默认 category=0 的 CacheData   
        /// todo. remove in further
        /// </summary>
        /// <param name="identify"></param>
        /// <param name="serialzableObj"></param>
        public CacheSerializableObject(string identify, object serialzableObj, List<byte[]> serializedData, long size)
            : base(identify)
        {
            this.obj = serialzableObj;
            this.data = serializedData;
            this.totalSize = size;
        }

        /// <summary>
        /// 构造方法，构造指定 category 的 CacheData
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="identify"></param>
        /// <param name="serialzableObj"></param>
        public CacheSerializableObject(byte categoryId, string identify, object serialzableObj)
            : base(categoryId, identify)
        {
            this.obj = serialzableObj;
            this.data = Function.GetBytesFromSerializableObj(this.obj, ConstParams.BufferBlockSize, ref this.totalSize);
        }

        /// <summary>
        /// 构造方法  
        /// todo. remove in further
        /// </summary>
        public CacheSerializableObject(byte categoryId, string identify, object serialzableObj, List<byte[]> serializedData, long size)
            : base(categoryId, identify)
        {
            this.obj = serialzableObj;
            this.data = serializedData;
            this.totalSize = size;
        }

        /// <summary>
        /// 构造方法， 用于从socket 数据构造 CacheData
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="identify"></param>
        /// <param name="dataList"></param>
        /// <param name="generateRealObj">是否创建 RealObj，对于 slave 而言，只需要存储其 byte[] 数组，无需进行转换为 RealObj，加快速度</param>
        public CacheSerializableObject(byte categoryId, string identify, List<byte[]> dataList, bool generateRealObj)
            : base(categoryId, identify, dataList)
        {
            if (generateRealObj)
                this.obj = Function.GetSerializableObjFromBytes(dataList, ref this.totalSize);
        }

        public override object RealObj
        {
            get { return this.obj; }
        }
    }
}
