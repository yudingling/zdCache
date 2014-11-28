using System;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace ZdCache.Common
{
    /// <summary>
    /// 异步方法委托
    /// </summary>
    /// <param name="arg"></param>
    public delegate void AsyncMethod(AsyncArgs arg);

    /// <summary>
    /// 异步方法结束后的回调方法委托
    /// </summary>
    /// <param name="arg"></param>
    public delegate void AsyncAbortMethod(AsyncArgs arg);

    /// <summary>
    /// 异步调用类参数
    /// </summary>
    public class AsyncArgs
    {
        public object Args;
        public object ArgsAbort;

        public AsyncArgs()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="arg">该参数传递给线程执行方法</param>
        /// <param name="argAbort">该参数传递给 Abort 方法</param>
        public AsyncArgs(object arg, object argAbort)
        {
            this.Args = arg;
            this.ArgsAbort = argAbort;
        }
    }

    /// <summary>
    /// 异步调用类
    /// 使用方式：
    ///     AsyncCall mainThread = new AsyncCall(new AsyncMethod(ThreadAction), null, false, null);
    ///     AsyncCall mainThread = new AsyncCall(new AsyncMethod(ThreadAction), new AsyncArgs(argObj1, argObj2), false,
    ///                                delegate(AsyncArgs argForEnd)
    ///                                {
    ///                                    if (argForEnd != null && argForEnd.ArgsAbort != null && argForEnd.ArgsAbort is SocketClient)
    ///                                        (argForEnd.ArgsAbort as SocketClient).Close();
    ///                                });
    /// </summary>
    public class AsyncCall
    {
        private AsyncArgs asArg;
        private AsyncMethod asMethod;
        private AsyncAbortMethod abortExecuteMethod;

        private SThreadTask task;
        private object lockObj = new object();

        /// <summary>
        ///  构造一个异步执行方法，支持终结控制  
        /// </summary>
        /// <param name="method">线程执行的方法</param>
        /// <param name="args">异步调用参数</param>
        /// <param name="startWhenCreated">是否创建后立即执行</param>
        /// <param name="finallyMethod">线程退出后需要执行的方法，如果 method 方法是一个不可退出的方法，比如 while(true) 这样的，则需要手动调用 Stop 来触发 abortMethod</param>
        public AsyncCall(AsyncMethod method, AsyncArgs args, bool startWhenCreated, AsyncAbortMethod abortMethod)
        {
            this.asMethod = method;
            this.asArg = args;
            this.abortExecuteMethod = abortMethod;
            if (startWhenCreated)
                Start();
        }

        public void Start()
        {
            lock (this.lockObj)
            {
                if (this.task == null)
                {
                    this.task = SThreadPool.Pop();
                    this.task.Active(this.asArg, this.asMethod, this.abortExecuteMethod, Finished);
                }
            }
        }

        /// <summary>
        /// 停止任务
        /// </summary>
        public void Stop()
        {
            //注意， stop 方法与 finished 方法必须是互斥执行的，因为这里对 task 引用进行了判断
            lock (this.lockObj)
            {
                if (this.task != null && this.task.IsAlive)
                {
                    //结合SleepHelper，关闭使用 ManualResetEvent 来实现等待的线程
                    SleepHelper.SetSignal(this.task.SThread);
                    SleepHelper.RemoveResetEvent(this.task.SThread);

                    if (this.task.IsAlive)
                        this.task.Stop();
                }
            }
        }

        /// <summary>
        /// 任务完成后的回调, 需要将其放回线程池中
        /// </summary>
        private void Finished()
        {
            lock (this.lockObj)
            {
                if (this.task != null)
                {
                    SThreadPool.Push(this.task);
                    this.task = null;
                }
            }
        }

        public bool IsAlive { get { return this.task != null ? this.task.IsAlive : false; } }
    }
}
