using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;

namespace ZdCache.Common.CacheCommon
{
    public class CallArgsModel
    {
        /// <summary>
        /// call 的标志, guid
        /// </summary>
        public Guid AcCallID { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public ActionKind AcKind { get; set; }

        /// <summary>
        /// 操作返回
        /// </summary>
        public ActionResult AcResult { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public ICacheDataType AcArgs { get; set; }

        /// <summary>
        /// 此构造函数
        /// </summary>
        public CallArgsModel(Guid id, ActionKind acKind, ActionResult acResult, ICacheDataType args)
        {
            this.AcCallID = id;
            this.AcKind = acKind;
            this.AcResult = acResult;
            this.AcArgs = args;
        }

        /// <summary>
        /// 构造函数, ActionResult 设置为 Init
        /// </summary>
        public CallArgsModel(Guid id, ActionKind acKind, ICacheDataType args)
        {
            this.AcCallID = id;
            this.AcKind = acKind;
            this.AcResult = ActionResult.Init;
            this.AcArgs = args;
        }
    }
}
