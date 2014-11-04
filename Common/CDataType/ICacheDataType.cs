using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common.CDataType
{
    public interface ICacheDataType
    {
        /// <summary>
        /// 缓存目录， 同一个 Category 下，ID 必须唯一
        /// </summary>
        byte Category { get; }

        /// <summary>
        /// 缓存的标识
        /// </summary>
        string ID { get; }

        /// <summary>
        /// 实体
        /// </summary>
        object RealObj { get; }

        /// <summary>
        /// 实体对应的字节list
        ///      实体作为内存中的对象，此处为什么要用 List<byte[]> 去存储，而不是直接搞个 byte[]。
        ///      因为内存是不连续的，比如有一个 500M 的 realobj， 直接分配一个 500M 的 byte[]，会因为连续空间不够而导致失败。
        ///      但如果换成500 个 1M 的byte[] 则失败的几率就小了， csdn 建议一次申请的内存应该不大于64KB
        /// </summary>
        List<byte[]> BytesData { get; }

        /// <summary>
        /// 实体总大小(字节数)
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// 字节数组的合并是否影响 RealObj 的生成。如果返回 true，则需要自行控制byte[] 数组的生成以及 RealObj 的生成 
        ///     比如从 socket 接收并创建 ICacheDataType (List<String>)对象时，我们不知道 List<String> 与 List<byte[]> 并不是一一对应的, 
        ///     每个 byte[] 可能包括了多个 string（同时还有可能一个 string 被拆分到两个 byte[] 中的情况），针对这种情况，需要做特殊处理，ByteMergeEffectRealObj 标记为 true
        /// </summary>
        bool ByteMergeEffectRealObj { get; }
    }
}
