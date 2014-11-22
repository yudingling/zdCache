using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using System.Diagnostics;
using System.Threading;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// set call
    /// </summary>
    public class CallSet : Call
    {
        private bool isSuccess = false;

        private BalanceHandler myBalancer;

        //执行 set 的slaveModel
        private SlaveModel slaveForSet;

        #region 构造函数

        public CallSet(BalanceHandler balancer)
        {
            this.myBalancer = balancer;
        }

        public CallSet(bool sync, FinishedDelegate finished, BalanceHandler balancer)
            : base(sync, finished)
        {
            this.myBalancer = balancer;
        }

        #endregion

        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out IList<ICacheDataType> retList)
        {
            retList = null;
            if (slaveList == null || slaveList.Count == 0)
                return false;

            //控制总调用时间的超时
            Stopwatch sp = new Stopwatch();
            //for set
            MasterCallArgsModel masterCallArgForSet = new MasterCallArgsModel(this.callID, ActionKind.Set, args, SlaveSetReturn);
            //for delete
            MasterCallArgsModel masterCallArgForDel = new MasterCallArgsModel(this.callID, ActionKind.Delete, args, SlaveSetReturn);

            int startIndex = 0;
            //一直执行到 set 成功或者报错
            sp.Start();

            while (true)
            {
                this.isSuccess = false;
                this.slaveForSet = null;

                int timeOutMS = ConstParams.CallTimeOut - (int)sp.ElapsedMilliseconds;
                if (timeOutMS <= 0)
                    throw new Exception("set timeout!");

                this.slaveForSet = this.myBalancer.GetMostWantedSlave(args.TotalSize, ref startIndex);
                if (this.slaveForSet == null)
                    return false;

                this.DoBeforeProcess();
                this.allCallCount = CallProcessor.Process(slaveList, slaveForSet, this.processedList, masterCallArgForSet, masterCallArgForDel, this);
                bool isTimeOut = this.DoAfterProcess(timeOutMS);

                //action 超时(单个调用超时以及整体超时)，则抛出异常
                if (isTimeOut || sp.ElapsedMilliseconds > ConstParams.CallTimeOut)
                    throw new Exception("set timeout!");

                if (this.isSuccess)
                {
                    //提前粗略更新 status 信息，以便balance 处理(因为 slave 自动上报的 status 间隔比较长，1s一次，避免set 频率过高导致的平衡计算误差)
                    slaveForSet.Status.IncCachedMem(args.TotalSize);

                    //提高平衡计算频率
                    this.myBalancer.Sort();

                    break;
                }
            }

            sp.Stop();
            return this.isSuccess;
        }

        private void SlaveSetReturn(ReturnArgsModel returnArgsModel)
        {
            SlaveModel tempModel;
            if (this.processedList.TryRemove(returnArgsModel.Slave.ID, out tempModel))
            {
                //如果是当前的 slaveForSet 返回成功，则认为成功
                //设置是否成功， 注意，必须在 DoReturnProcess 前设置，保证阻塞（DoAfterProcess）退出前 isSuccess 已更新
                if (this.slaveForSet != null && returnArgsModel.Slave.ID == this.slaveForSet.ID
                    && returnArgsModel.AcResult == ActionResult.Succeed)
                {
                    this.isSuccess = true;
                }

                this.DoReturnProcess();
            }
        }
    }
}
