using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;

namespace ZdCache.MasterCache
{
    public class ReturnArgsModel
    {
        public SlaveModel Slave { get; set; }
        public ActionResult AcResult { get; set; }
        public ICacheDataType Data { get; set; }

        public ReturnArgsModel(SlaveModel slaveModel, ActionResult result, ICacheDataType acRet)
        {
            this.Slave = slaveModel;
            this.AcResult = result;
            this.Data = acRet;
        }
    }
}
