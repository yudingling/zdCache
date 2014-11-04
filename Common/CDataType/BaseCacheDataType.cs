using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common.CDataType
{
    /// <summary>
    /// 缓存的数据类型基类
    /// </summary>
    public abstract class BaseCacheDataType : ICacheDataType
    {
        protected byte category = 0;
        protected string id = string.Empty;
        protected List<byte[]> data;
        protected long totalSize = 0;

        /// <summary>
        /// 构造方法，构造用于查询的 CacheData
        /// </summary>
        public BaseCacheDataType(string identify)
        {
            this.id = identify;
        }

        /// <summary>
        /// 构造函数， 构造指定 category 的 CacheData
        /// </summary>
        public BaseCacheDataType(byte categoryId, string identify)
        {
            this.category = categoryId;
            this.id = identify;
        }

        /// <summary>
        /// 构造方法， 用于从socket 数据构造 CacheData
        /// </summary>
        public BaseCacheDataType(byte categoryId, string identify, List<byte[]> dataList)
        {
            this.category = categoryId;
            this.id = identify;
            this.data = dataList;

            foreach (byte[] singleB in dataList)
                this.totalSize += (long)singleB.Length;
        }

        /// <summary>
        /// override equals 实现，只要比较 category 及 id
        /// </summary>
        public override bool Equals(object obj)
        {
            var cmpObj = obj as BaseCacheDataType;
            if (cmpObj == null)
                return false;

            return (cmpObj.category == this.category && cmpObj.id == this.id);
        }

        public override int GetHashCode()
        {
            return (this.category + "_" + this.id).GetHashCode();
        }

        #region ICacheDataType 成员

        public byte Category
        {
            get { return this.category; }
        }

        public string ID
        {
            get { return this.id; }
        }

        public abstract object RealObj
        {
            //抽象化，子类实现
            get;
        }

        public virtual List<byte[]> BytesData
        {
            get { return this.data; }
        }

        public long TotalSize
        {
            get { return this.totalSize; }
        }

        public virtual bool ByteMergeEffectRealObj
        {
            get { return false; }
        }

        #endregion
    }
}
