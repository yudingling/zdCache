using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CacheCommon;

namespace ZdCache.Common.CDataType
{
    /// <summary>
    /// 空的缓存对象，用于占位填充
    /// </summary>
    [CacheDataTypeAttribute(3)]
    public class CacheNull : BaseCacheDataType
    {
        public CacheNull()
            : base(string.Empty)
        {
        }

        /// <summary>
        /// 构造方法， 用于从socket 数据构造 CacheData
        /// </summary>
        public CacheNull(byte categoryId, string identify, List<byte[]> dataList, bool generateRealObj)
            : base(categoryId, identify, dataList)
        {
        }

        public override object RealObj
        {
            get { return null; }
        }
    }
}
