using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache.PassiveExpireStrategy
{
    /// <summary>
    /// 被动缓存失效策略的默认实现
    /// </summary>
    public class DefaultPassiveExpireStrategy : IPassiveExpireStrategy
    {
        //最大缓存大小(字节)
        private long maxBufferMemory = 1024 * 1024 * 1024;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCacheSize">最大缓存大小(字节)</param>
        public DefaultPassiveExpireStrategy(long maxCacheSize)
        {
            this.maxBufferMemory = maxCacheSize;
        }

        #region IExpireStrategy 成员

        public bool ExecStrategy(StatusInfo info, out bool isForceToReduceSize)
        {
            isForceToReduceSize = false;
            if (info != null)
            {
                //1、如果内存使用率超过了 95% 或者可用内存不足50M，则 ExpireInBoundary(true)，强制释放内存
                //2、如果超过了 maxBufferMemory，则 ExpireInBoundary(false)
                //上面的条件优先顺序依次从高到低
                if (info.MemUseRate > (decimal)0.95 || info.FreeMem < 1024 * 1024 * 50)
                {
                    isForceToReduceSize = true;
                    return true;
                }
                else if (info.CachedMem >= this.maxBufferMemory)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        #endregion
    }
}
