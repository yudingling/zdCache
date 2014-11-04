using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.MasterCache.LoadbalanceStrategy
{
    /// <summary>
    /// 负载平衡策略接口, 返回按选中级别从高到低的一个排序 SlaveModel 数组
    /// </summary>
    public interface ILoadBalanceStrategy
    {
        SlaveModel[] MakeBalance(ICollection<SlaveModel> slaves);
    }
}
