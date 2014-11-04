using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ZdCache.MasterCache.LoadbalanceStrategy
{
    /// <summary>
    /// 默认负载平衡策略实现
    /// </summary>
    public class DefaultLoadBalanceStrategy : ILoadBalanceStrategy
    {
        #region ILoadBalanceStrategy 成员

        public SlaveModel[] MakeBalance(ICollection<SlaveModel> slaves)
        {
            ConcurrentBag<SlaveModel> bag = new ConcurrentBag<SlaveModel>();

            //并行执行
            Parallel.ForEach(slaves, model =>
            {
                //满足条件：1、存在空闲内存(20M)  2、1分钟内cpu占用率小于等于95%
                if (model.Status.AvailMem - model.Status.UsedMem >= 20971520
                    && model.Status.CpuUseRateIn1Minutes <= 95)
                {
                    bag.Add(model);
                }
            });

            SlaveModel[] retArray = bag.ToArray();
            //冒泡排序
            SlaveModel tmpKeyValue = null;
            for (int i = retArray.Length - 1; i >= 0; i--)
                for (int j = 0; j < i; j++)
                {
                    //优先级：内存使用率 > 可用内存 > 已缓存内存
                    if ((int)(retArray[i].Status.MemUseRate / 1024 / 1024) < (int)(retArray[j].Status.MemUseRate / 1024 / 1024)
                        || (int)(retArray[i].Status.FreeMem / 1024 / 1024) < (int)(retArray[j].Status.FreeMem / 1024 / 1024)
                        || (int)(retArray[i].Status.CachedMem / 1024 / 1024) < (int)(retArray[j].Status.CachedMem / 1024 / 1024))
                    {
                        tmpKeyValue = retArray[i];
                        retArray[i] = retArray[j];
                        retArray[j] = tmpKeyValue;
                    }
                }

            return retArray;
        }

        #endregion
    }
}
