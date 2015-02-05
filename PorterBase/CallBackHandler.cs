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
        private volatile bool running = true;

        private List<AsyncCall> callList;
        private PorterReceive myCallBack;

        private ConcurrentQueue<CallBackListArg> argList;

        //通过信号量控制等待
        private SemaphoreSlim semaphore = new SemaphoreSlim(0);

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
                        //信号量减1
                        this.semaphore.Wait(0);

                        //此方法应该自行处理异常，一般不应该抛出到这
                        this.myCallBack(calBackArg.ID, calBackArg.dataList);
                    }
                    else
                        this.semaphore.Wait(Timeout.Infinite);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 获取一个回调
        /// </summary>
        /// <returns></returns>
        private CallBackListArg Pop()
        {
            CallBackListArg arg;
            if (this.argList.TryDequeue(out arg))
                return arg;
            else
                return null;
        }

        /// <summary>
        /// 添加一个回调
        /// </summary>
        /// <param name="arg"></param>
        internal void Push(CallBackListArg arg)
        {
            try
            {
                if (this.running)
                {
                    this.argList.Enqueue(arg);

                    //Release 方法达到最大值，会抛出异常，此处需要处理
                    semaphore.Release();
                }
            }
            catch
            {
            }
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源，将一直阻塞直到队列中的所有数据都处理完成
        /// </summary>
        public void Dispose()
        {
            if (this.semaphore != null)
            {
                //设置停止标识
                this.running = false;

                while (this.argList.Count > 0)
                    SleepHelper.Sleep(1);

                //给信号，使线程结束
                for (int i = 0; i < this.callList.Count; i++)
                    this.semaphore.Release();

                bool stoped = false;
                while (!stoped)
                {
                    stoped = true;
                    this.semaphore.Release();
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

                this.semaphore.Dispose();
                this.semaphore = null;
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
