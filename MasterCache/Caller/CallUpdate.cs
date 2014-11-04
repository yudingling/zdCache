using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using ZdCache.Common;
using System.Threading.Tasks;
using ZdCache.Common.CacheCommon;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// update call
    /// </summary>
    public class CallUpdate : Call
    {
        protected bool isSuccess = false;

        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out List<ICacheDataType> retList)
        {
            retList = null;
            if (slaveList == null || slaveList.Count == 0)
                return false;

            this.DoBeforeProcess();
            this.allCallCount = CallProcessor.Process(slaveList, this.processedList, new MasterCallArgsModel(this.callID, ActionKind.Update, args, UpdateCallReturn), this);
            bool isTimeOut = this.DoAfterProcess(ConstParams.CallTimeOut);

            //结束 action
            Stop();

            //action 超时，则抛出异常
            if (isTimeOut)
                throw new Exception("update timeout!");

            return this.isSuccess;
        }

        private void UpdateCallReturn(ReturnArgsModel returnArgsModel)
        {
            SlaveModel tempModel;
            if (this.processedList.TryRemove(returnArgsModel.Slave.ID, out tempModel))
            {
                //只要有一个update成功，则设置成功
                if (returnArgsModel.AcResult == ActionResult.Succeed)
                    this.isSuccess = true;

                this.DoReturnProcess();
            }
        }
    }
}
