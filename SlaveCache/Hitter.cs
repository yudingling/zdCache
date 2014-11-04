using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZdCache.Common.CDataType;
using System.Threading;

namespace ZdCache.SlaveCache
{
    /// <summary>
    /// 委托，缓存失效时将调用
    /// </summary>
    /// <param name="categoray"></param>
    /// <param name="availIds"></param>
    internal delegate void ExpireCachedItems(byte categorayID, List<string> availIds);

    /// <summary>
    /// 处理缓存命中、失效
    /// </summary>
    internal class CacheHitter
    {
        #region innerClass，存储缓存最新的访问时间标识

        private class HitInfo
        {
            public String ID { get; set; }
            public long Size { get; set; }
            public DateTime TM { get; set; }
        }

        #endregion

        //操作次数、命中次数
        private long actionCount = 1, hitCount = 1;

        //过期时间(ms)
        private int expireTM = 3600 * 1000;

        //存储每个缓存的最新访问时间的顺序，LIFO 存储
        private ConcurrentDictionary<byte, ConcurrentStack<HitInfo>> myHitter = new ConcurrentDictionary<byte, ConcurrentStack<HitInfo>>();

        private ExpireCachedItems expireKiller;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="buffExpireTM">过期时间(ms)，小于0则永不过期</param>
        /// <param name="cacheKiller"></param>
        public CacheHitter(int buffExpireTM, ExpireCachedItems cacheKiller)
        {
            this.expireKiller = cacheKiller;
            this.expireTM = buffExpireTM;

            if (this.expireTM > 0)
            {
                AsyncCall call = new AsyncCall(new AsyncMethod(ExpireAuto), null, true, null);
            }
        }

        /// <summary>
        /// 缓存自动过期
        /// </summary>
        /// <param name="arg"></param>
        private void ExpireAuto(AsyncArgs arg)
        {
            while (true)
            {
                try
                {
                    ExpireWithTM();
                }
                catch
                {
                }

                SleepHelper.Sleep(this.expireTM);
            }
        }


        private object lockObj = new object();
        /// <summary>
        /// 通过hit时间执行过期
        /// </summary>
        public void ExpireWithTM()
        {
            //注意，这里必须lock，因为主动调用和被动调用都是执行这个方法
            lock (lockObj)
            {
                DateTime cmpTM = DateTime.Now.AddMilliseconds(-1 * this.expireTM);

                //并行计算
                Parallel.ForEach<byte>(this.myHitter.Keys,
                    categoryID =>
                    {
                        ConcurrentStack<HitInfo> tempStack;

                        //超时的动作对于时间的及时性要求比较低，对业务影响小，所以此处就暂时不寻找更好的算法去快速处理，直接遍历，找出有效的 id   
                        //todo. 如果以后找想到更好的方式再修改
                        this.myHitter.TryGetValue(categoryID, out tempStack);
                        if (tempStack != null)
                        {
                            List<string> ids = new List<string>();

                            //stack 是 LIFO，所以第一个hitinfo 的时间是最新的
                            HitInfo item;
                            while (tempStack.TryPop(out item))
                            {
                                if (item.TM >= cmpTM)
                                {
                                    if (!ids.Contains(item.ID))
                                        ids.Add(item.ID);
                                }
                                else
                                    break;
                            }

                            //调用 expireKiller
                            this.expireKiller(categoryID, ids);

                            //执行完后，清空 stack。
                            //   此处能这么做是因为后续将会睡眠 expireTM， 如果这段时间都没有访问，则代表其肯定是要被 expire 掉，
                            //   所以 tempStack 剩余的记录不用再考虑了。
                            tempStack.Clear();
                        }
                    });
            }
        }

        /// <summary>
        /// 通过 pool size 执行过期
        /// </summary>
        /// <param name="remainSize"></param>
        public void ExpireWithSize(long remainSize)
        {
            lock (lockObj)
            {
                long sumSize = 0;

                //并行计算
                Parallel.ForEach<byte>(this.myHitter.Keys,
                    categoryID =>
                    {
                        ConcurrentStack<HitInfo> tempStack;

                        this.myHitter.TryGetValue(categoryID, out tempStack);
                        if (tempStack != null)
                        {
                            List<string> ids = new List<string>();

                            HitInfo item;
                            while (tempStack.TryPop(out item))
                            {
                                if (!ids.Contains(item.ID))
                                {
                                    ids.Add(item.ID);
                                    sumSize += item.Size;
                                }

                                if (sumSize >= remainSize)
                                    break;
                            }

                            //调用 expireKiller
                            this.expireKiller(categoryID, ids);

                            //注意，此处不能 clear tempStack
                        }
                    });
            }
        }

        /// <summary>
        /// 命中一个缓存。 注意这里的实现，之所以使用 stack 去存，而不是用字典之来保证唯一性(以便后续判断的方便), 是为了提高速度，不查询字典，空间换时间
        /// </summary>
        public void Hit(ICacheDataType data)
        {
            //此处不考虑环绕，由 IncActionCount 进行考虑，因为 actionCount >= hitCount
            Interlocked.Increment(ref this.hitCount);

            if (data != null)
            {
                ConcurrentStack<HitInfo> queue = GetQueue(data.Category, true);
                if (queue != null)
                {
                    queue.Push(new HitInfo() { ID = data.ID, Size = data.TotalSize, TM = DateTime.Now });
                }
            }
        }

        /// <summary>
        /// 增加操作次数
        /// </summary>
        public void IncActionCount()
        {
            //为避免超过最大值，如果环绕了，则都初始化为1
            if (Interlocked.Increment(ref this.actionCount) <= 0)
            {
                this.actionCount = 1;
                this.hitCount = 1;
            }
        }

        private ConcurrentStack<HitInfo> GetQueue(byte categoryID, bool createWhenNotExist)
        {
            ConcurrentStack<HitInfo> destQueue = null;
            if (this.myHitter.TryGetValue(categoryID, out destQueue))
                return destQueue;
            else
            {
                //注意此处的逻辑，因为多线程访问的缘故，必须先 tryadd，再 tryget，
                if (createWhenNotExist)
                    this.myHitter.TryAdd(categoryID, new ConcurrentStack<HitInfo>());

                this.myHitter.TryGetValue(categoryID, out destQueue);
                return destQueue;
            }
        }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate { get { return Math.Round((double)this.hitCount / (double)this.actionCount, 2); } }
    }
}
