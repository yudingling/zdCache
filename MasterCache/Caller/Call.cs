using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// call 基类
    /// </summary>
    public abstract class Call
    {
        /// <summary>
        /// 标志每个Call，唯一
        /// </summary>
        protected Guid callID = Guid.NewGuid();

        private MREContext callContext = new MREContext();

        /// <summary>
        /// 存储此 call 对应的执行命令的 SlaveModel
        /// </summary>
        protected ConcurrentDictionary<int, SlaveModel> processedList = new ConcurrentDictionary<int, SlaveModel>();

        /// <summary>
        /// call 的总数。 
        /// </summary>
        protected int allCallCount = -1;

        /// <summary>
        /// 已经返回的 call 数量。如果 allCallCount 不为 -1，且 allCallCount = returnedCallCount ，则认为处理完成
        /// </summary>
        protected int returnedCallCount = 0;

        /// <summary>
        /// 处理 call
        /// </summary>
        /// <param name="slaveList"></param>
        /// <param name="args"></param>
        /// <param name="retList">返回的结果，如果操作无返回cachedata，则为 null (比如find 是有结果， set、update、delete 是没有的)</param>
        /// <returns></returns>
        public virtual bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out List<ICacheDataType> retList)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoBeforeProcess
        /// </summary>
        protected void DoBeforeProcess()
        {
            this.allCallCount = -1;
            this.returnedCallCount = 0;
            //设为非终止，使线程等待
            this.callContext.Done(this.allCallCount, this.returnedCallCount, 0, MREType.ReSet);
        }

        /// <summary>
        /// DoAfterProcess
        /// </summary>
        /// <returns>返回是否超时</returns>
        protected bool DoAfterProcess(int timeOut)
        {
            //注意，下面的语句与 DoReturnProcess 中的 Set 操作需要控制单线程执行。 此处通过上下文来执行
            //返回是否超时
            return !this.callContext.Done(this.allCallCount, this.returnedCallCount, timeOut, MREType.WaitOne);
        }

        /// <summary>
        /// DoReturnProcess
        /// </summary>
        protected void DoReturnProcess()
        {
            //将已 return 的 call 数量增 1
            int tempVal = Interlocked.Increment(ref this.returnedCallCount);

            //这里还加一句判断，是为了提高速度，避免不必要的锁定
            if (this.allCallCount == tempVal)
            {
                //控制单线程执行
                this.callContext.Done(this.allCallCount, tempVal, 0, MREType.Set);
            }
        }

        /// <summary>
        /// 停止查找
        /// </summary>
        public void Stop()
        {
            //清除 call
            Parallel.ForEach(processedList.Values, model =>
            {
                model.RemoveCall(this.callID);
            });

            this.processedList.Clear();
        }

        /// <summary>
        /// 判断在 Processing 过程中是否退出(默认返回 false)。
        /// 比如 get 操作且返回唯一值，则当收到一个结果后，即可停止剩余的所有 process
        /// </summary>
        public virtual bool BreakWhenProcessing { get { return false; } }

        /// <summary>
        /// call 的唯一标识
        /// </summary>
        public Guid ID { get { return this.callID; } }
    }
}
