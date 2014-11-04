using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common.CacheCommon
{
    /// <summary>
    /// 固定常量
    /// </summary>
    public class ConstParams
    {
        /// <summary>
        /// 缓存块大小(4KB)
        /// </summary>
        public const int BufferBlockSize = 4096;

        /// <summary>
        /// Call 的超时时间(2秒)
        /// </summary>
        public const int CallTimeOut = 2000;

        /// <summary>
        /// slave 获取状态信息的间隔(ms)
        /// </summary>
        public const int SlaveStatusInterval = 1000;

        /// <summary>
        /// slave 未报状态的允许时长。超过此时长，master 将会删除此客户端。
        /// 注意，此值要比 slave 获取状态信息的间隔要长（考虑到网络延时，以及计算点的不一致，应为: 最大延时长+2倍获取状态间隔时长）
        /// </summary>
        public const int MaxIntervalOfStatusWhenSlaveErrorOccured = 4000;

        /// <summary>
        /// PackageDataContainer 中无效数据的超时时间（清除因接收异常而导致无法接收完全的数据）
        /// </summary>
        public const int UnusePackageDataExpireTM = CallTimeOut + 1000;

        #region 数据协议

        /// <summary>
        /// 整个数据包所占长度
        /// </summary>
        public const int Protocol_PackageLength = 4;

        /// <summary>
        /// 包的总数 所占长度
        /// </summary>
        public const int Protocol_PackageCount = 4;

        /// <summary>
        /// 包的序号 所占长度
        /// </summary>
        public const int Protocol_PackageOrder = 4;

        /// <summary>
        /// callID 所占长度，GUID为16个字节整数
        /// </summary>
        public const int Protocol_ActionCallIDLength = 16;

        /// <summary>
        /// 操作类型 所占长度
        /// </summary>
        public const int Protocol_ActionKindLength = 1;

        /// <summary>
        /// 操作结果 所占长度
        /// </summary>
        public const int Protocol_ActionResultLength = 1;

        /// <summary>
        /// 数据类型(ICacheDataType 的 CacheDataTypeAttribute 值) 所占长度
        /// </summary>
        public const int Protocol_DataTypeLength = 1;

        /// <summary>
        /// category 所占长度
        /// </summary>
        public const int Protocol_CategoryLength = 1;

        /// <summary>
        /// 数据内容的唯一标识 ICacheDataType.ID 所占长度
        /// </summary>
        public const int Protocol_ArgIDLength = 4;

        #endregion
    }
}
