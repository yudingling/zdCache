using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.MasterCache
{
    /// <summary>
    /// 委托，用于 Slave 对 master 命令的响应 (SlaveModel 将通过 SlaveCallReturn 通知 Call 对象)
    /// </summary>
    /// <param name="returnArgsModel"></param>
    public delegate void SlaveCallReturn(ReturnArgsModel returnArgsModel);
}
