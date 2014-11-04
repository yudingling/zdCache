using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;

namespace ZdCache.SlaveCache.LocalAction
{
    /// <summary>
    /// 本地化接口(针对缓存的 set/get/delete/update/expire)
    /// 注意，此方法后续是多线程调用的
    /// </summary>
    public interface ILocalAction
    {
        void ExecLocalAction(LocalActionType localAcType, ICacheDataType data);
    }
}
