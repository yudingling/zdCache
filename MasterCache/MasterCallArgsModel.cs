using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;

namespace ZdCache.MasterCache
{
    public class MasterCallArgsModel : CallArgsModel
    {
        /// <summary>
        /// 回调
        /// </summary>
        public SlaveCallReturn AcCallReturn;

        /// <summary>
        /// 此构造函数, ActionResult 初始为 Init
        /// </summary>
        public MasterCallArgsModel(Guid id, ActionKind acKind, ICacheDataType args, SlaveCallReturn callRet)
            : base(id, acKind, ActionResult.Init, args)
        {
            this.AcCallReturn = callRet;
        }

        /// <summary>
        /// 此构造函数
        /// </summary>
        public MasterCallArgsModel(Guid id, ActionKind acKind, ActionResult acResult, ICacheDataType args, SlaveCallReturn callRet)
            : base(id, acKind, acResult, args)
        {
            this.AcCallReturn = callRet;
        }
    }
}
