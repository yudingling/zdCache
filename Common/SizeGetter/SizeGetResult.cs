using System;

namespace ZdCache.Common.SizeGetter
{
    /// <summary>
    /// 获取长度的结果 
    /// </summary>
    public enum SizeGetResult
    {
        /// <summary>
        /// 表示无长度标志位
        /// </summary>
        NoSizeFlag,

        /// <summary>
        /// 表示获取成功
        /// </summary>
        Success,

        /// <summary>
        /// 表示参数字节数组长度不够
        /// </summary>
        ParaBytesNotEnough,

        /// <summary>
        /// 表示此包忽略，不是所需要的数据
        /// </summary>
        Ignore
    }
}
