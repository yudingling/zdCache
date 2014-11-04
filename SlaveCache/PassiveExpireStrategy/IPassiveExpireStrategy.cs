using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache.PassiveExpireStrategy
{
    /// <summary>
    /// 被动缓存失效策略
    /// </summary>
    public interface IPassiveExpireStrategy
    {
        /// <summary>
        /// 边界监测，确定是否需要执行主动 expire
        /// </summary>
        /// <param name="info"></param>
        /// <param name="isForceToReduceSize">是否强制进行缓存大小清理</param>
        /// <returns></returns>
        bool ExecStrategy(StatusInfo info, out bool isForceToReduceSize);
    }
}
