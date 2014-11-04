using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using ZdCache.Common;
using System.Threading;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// 回调处理器。 通过多线程对 callback list 进行回调
    /// </summary>
    public class CallBackHandler : IDisposable
    {
        private bool running = true;

        private List<AsyncCall> callList;
        private PorterReceive myCallBack;

        //存储所有的回调列表，此处必须是线程安全的
        //对于socket 来说，回调参数用完就可以丢弃，所以此处用 queuq 即可 (需要先进先出)
        private ConcurrentQueue<CallBackListArg> argList;

        //控制等待
        private ManualResetEvent manualRE = new ManualResetEvent(false);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">处理回调的线程数量</param>
        internal CallBackHandler(int threadCount, PorterReceive callbackMethod)
        {
            this.argList = new ConcurrentQueue<CallBackListArg>();
            this.myCallBack = callbackMethod;

            if (this.myCallBack != null)
            {
                this.callList = new List<AsyncCall>();
                for (int i = 0; i < threadCount; i++)
                {
                    //回调线程
                    this.callList.Add(new AsyncCall(new AsyncMethod(HandleCallBack), null, true, null));
                }
            }
        }

        /// <summary>
        /// 处理回调的线程方法
        /// </summary>
        /// <param name="arg"></param>
        private void HandleCallBack(AsyncArgs arg)
        {
            while (running)
            {
                try
                {
                    CallBackListArg calBackArg = Pop();
                    if (calBackArg != null)
                    {
                        //此方法应该自行处理异常，一般不应该抛出到这
                        this.myCallBack(calBackArg.ID, calBackArg.dataList);
                    }
                    else
                    {
                        //如果找不到了，则最多睡眠 1 ms
                        this.manualRE.Reset();
                        this.manualRE.WaitOne(1);
                    }
                }
                catch
                {
                }
            }
        }

        //private void WaitInTimeLess()
        //{
        //    //以下为实现短时间等待
        //    //sleep 是以毫秒为单位的，会交出当前线程分配的cpu时间片，转而执行别的线程，直到指定的时间结束，sleep在一定程度上能减少cpu的使用
        //    //spinwait 是以cpu的时钟周期为单位，让cpu处于自旋转的等待过程，cpu在循环指定的时钟周期期间，线程处于假死，cpu也假死，
        //    //但线程没有交出所分配的时间片，所以cpu占用是一直存在的
        //    //结合两者，就能够实现等待很短时间，且能达到只用sleep（1）这样的方式降低cpu使用的效果
        //    uint loops = 0;
        //    while (!this.isFinished)
        //    {
        //        if (Environment.ProcessorCount == 1 || (++loops % 100) == 0)
        //        {
        //            Thread.Sleep(1);
        //        }
        //        else
        //        {
        //            Thread.SpinWait(20);
        //        }
        //        if (sp.ElapsedMilliseconds > timeOut)
        //        {
        //            isTimeOut = true;
        //            break;
        //        }
        //    }
        //}

        /// <summary>
        /// 添加一个回调
        /// </summary>
        /// <param name="arg"></param>
        internal void Push(CallBackListArg arg)
        {
            if (this.running)
                this.argList.Enqueue(arg);

            manualRE.Set();
        }

        /// <summary>
        /// 获取一个回调
        /// </summary>
        /// <returns></returns>
        internal CallBackListArg Pop()
        {
            CallBackListArg arg;
            if (this.argList.TryDequeue(out arg))
                return arg;
            else
                return null;
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源，将一直阻塞直到队列中的所有数据都处理完成
        /// </summary>
        public void Dispose()
        {
            while (this.argList.Count > 0)
                SleepHelper.Sleep(1);

            //设置停止标识
            this.running = false;

            bool stoped = false;
            while (!stoped)
            {
                stoped = true;
                foreach (AsyncCall call in this.callList)
                {
                    if (call != null && call.IsAlive)
                    {
                        stoped = false;
                        break;
                    }
                }
                SleepHelper.Sleep(100);
            }
        }

        #endregion
    }

    internal class CallBackListArg
    {
        internal int ID;
        internal List<byte[]> dataList;

        /// <summary>
        /// 回调参数的构造
        /// </summary>
        /// <param name="tokenID">usertoken 的 id，用以标识唯一的 SAEA</param>
        /// <param name="bytesList">bytes 列表</param>
        internal CallBackListArg(int tokenID, List<byte[]> bytesList)
        {
            this.ID = tokenID;
            this.dataList = bytesList;
        }

        /// <summary>
        /// 回调参数的构造
        /// </summary>
        /// <param name="tokenID">usertoken 的 id，用以标识唯一的 SAEA</param>
        /// <param name="bytesData">bytes 数组</param>
        internal CallBackListArg(int tokenID, byte[] bytesData)
        {
            this.ID = tokenID;
            this.dataList = new List<byte[]>();
            this.dataList.Add(bytesData);
        }
    }
}
