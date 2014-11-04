using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZdCache.SlaveCache.LocalAction;

namespace ZdCache.SlaveCache
{
    public class DataPool : IDisposable
    {
        //数据容器
        private ConcurrentDictionary<byte, ConcurrentDictionary<string, ICacheDataType>> container = new ConcurrentDictionary<byte, ConcurrentDictionary<string, ICacheDataType>>();

        private CacheHitter hitter;
        private LocalActionPool localActionPool;

        //缓存总大小(byte)
        private long totalSize = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="expireTM">过期时间(ms)， 小于0则永不过期</param>
        /// <param name="localActionExecThreadCount">本地化操作线程数</param>
        public DataPool(int expireTM, int localActionExecThreadCount)
        {
            this.hitter = new CacheHitter(expireTM, new ExpireCachedItems(KillCache));
            this.localActionPool = new LocalActionPool(localActionExecThreadCount, new DefaultLocalAction());
        }

        /// <summary>
        /// 缓存的总大小(byte)
        /// </summary>
        public long CachedMemSize
        {
            get
            {
                return this.totalSize;
            }
        }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate { get { return this.hitter.HitRate; } }

        #region 针对缓存的 public 方法  set/get/delete/update/ExpireInBoundary

        public bool Set(ICacheDataType data)
        {
            this.hitter.IncActionCount();

            if (data != null)
            {
                ConcurrentDictionary<string, ICacheDataType> destDic = GetCategory(data.Category, true);
                if (destDic != null)
                {
                    if (destDic.TryAdd(data.ID, data))
                    {
                        //增加数据的时候 hit
                        this.hitter.Hit(data);
                        //本地化操作
                        this.localActionPool.Push(LocalActionType.Set, data);

                        this.totalSize += data.TotalSize;
                        return true;
                    }
                }
            }

            return false;
        }

        public ICacheDataType Get(ICacheDataType key)
        {
            this.hitter.IncActionCount();

            if (key != null)
            {
                ConcurrentDictionary<string, ICacheDataType> destDic = GetCategory(key.Category, false);
                if (destDic != null)
                {
                    ICacheDataType retValue;
                    destDic.TryGetValue(key.ID, out retValue);

                    if (retValue != null)
                    {
                        //获取数据的时候 hit
                        this.hitter.Hit(retValue);
                        //本地化操作
                        this.localActionPool.Push(LocalActionType.Get, retValue);
                    }

                    return retValue;
                }
            }

            return null;
        }

        public bool Delete(ICacheDataType key)
        {
            this.hitter.IncActionCount();

            if (key != null)
            {
                ConcurrentDictionary<string, ICacheDataType> destDic = GetCategory(key.Category, false);
                if (destDic != null)
                {
                    ICacheDataType retValue;
                    if (destDic.TryRemove(key.ID, out retValue))
                    {
                        //删除数据时的hit，传递 null
                        this.hitter.Hit(null);
                        //本地化操作
                        this.localActionPool.Push(LocalActionType.Delete, retValue);
                        this.totalSize -= retValue.TotalSize;

                        return true;
                    }
                }
            }

            return false;
        }

        public bool Update(ICacheDataType data)
        {
            this.hitter.IncActionCount();

            if (data != null)
            {
                //update，如果不存在，则创建
                ConcurrentDictionary<string, ICacheDataType> destDic = GetCategory(data.Category, true);
                if (destDic != null)
                {
                    ICacheDataType oldValue;
                    if (destDic.TryRemove(data.ID, out oldValue))
                        this.totalSize -= oldValue.TotalSize;

                    if (destDic.TryAdd(data.ID, data))
                    {
                        this.totalSize += data.TotalSize;
                        //hit
                        this.hitter.Hit(data);
                        //本地化操作
                        this.localActionPool.Push(LocalActionType.Update, data);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 执行缓存的被动失效策略。 当 isForceToReduceSize=true 时，将衰减当前缓存大小的 5%
        /// </summary>
        /// <param name="isForceToReduceSize">是否强制删除不常用的缓存，以保持内存的大小</param>
        public void ExpireFromStrategy(bool isForceToReduceSize)
        {
            if (isForceToReduceSize)
                this.hitter.ExpireWithSize((long)(this.totalSize * 0.95));
            else
                this.hitter.ExpireWithTM();
        }

        #endregion

        private ConcurrentDictionary<string, ICacheDataType> GetCategory(byte categoryID, bool createWhenNotExist)
        {
            ConcurrentDictionary<string, ICacheDataType> destDic = null;

            if (!this.container.TryGetValue(categoryID, out destDic)
                && createWhenNotExist)
            {
                //注意此处的写法，一定要保证数据来自于字典中, 但不要采用 tryadd 紧接着再 tryget，那样在最坏的情况下效率减半
                destDic = new ConcurrentDictionary<string, ICacheDataType>();
                if (!this.container.TryAdd(categoryID, destDic))
                {
                    destDic = null;
                    this.container.TryGetValue(categoryID, out destDic);
                }
            }

            return destDic;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <param name="categorayID"></param>
        /// <param name="availIds">有效 id</param>
        private void KillCache(byte categorayID, List<string> availIds)
        {
            ICacheDataType tempCache;
            ConcurrentDictionary<string, ICacheDataType> tempDic;
            if (this.container.TryGetValue(categorayID, out tempDic))
            {
                Parallel.ForEach<string>(tempDic.Keys,
                    curId =>
                    {
                        //如果不在 availIds 中则删除
                        if (!availIds.Contains(curId) && tempDic.TryRemove(curId, out tempCache))
                        {
                            //本地化操作
                            this.localActionPool.Push(LocalActionType.Expire, tempCache);
                            this.totalSize -= tempCache.TotalSize;
                        }
                    }
                );
            }
        }

        #region IDisposable 成员

        public void Dispose()
        {
            //释放本地化操作池
            this.localActionPool.Dispose();
        }

        #endregion
    }
}
