using System;
using System.Text;
using System.Threading;

namespace ZdCache.Common
{
    /// <summary>
    /// 线程任务完成后的回调
    /// </summary>
    public delegate void ThreadTaskFinished();

    /// <summary>
    /// 线程任务
    /// </summary>
    public class SThreadTask : IDisposable
    {
        //唯一标识此 Task
        private int id;

        //线程锁
        private AutoResetEvent locks;

        private volatile AsyncArgs asArg;
        private volatile AsyncMethod asMethod;
        private volatile AsyncAbortMethod abortExecuteMethod;

        //用于标识是否继续 thread 的 while 循环， 当调用 Stop 强制结束 Task 的时候，需要通过此标志来继续线程
        //注意，需要 volatile 修饰
        private volatile bool continueLoop = false;
        private Thread thread;
        //标识线程是否还在执行
        private volatile bool isThreadAlive = false;

        //任务完成后的回调
        private ThreadTaskFinished callBack;

        public SThreadTask(int taskID)
        {
            this.id = taskID;
            //默认阻止
            locks = new AutoResetEvent(false);

            this.thread = new Thread(CallStart);
            //线程默认为后台线程，随主线程结束而结束
            thread.IsBackground = true;
            this.thread.Start();
        }

        /// <summary>
        /// 启动任务
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="method"></param>
        /// <param name="abortMethod"></param>
        public void Active(AsyncArgs arg, AsyncMethod method, AsyncAbortMethod abortMethod, ThreadTaskFinished finishedCallBack)
        {
            this.asArg = arg;
            this.asMethod = method;
            this.abortExecuteMethod = abortMethod;
            this.callBack = finishedCallBack;

            //标识线程启动
            this.isThreadAlive = true;
            //给信号，启动线程
            this.locks.Set();
        }

        /// <summary>
        /// 停止任务, 调用此方法将强制结束任务
        /// </summary>
        public void Stop()
        {
            try
            {
                //保持线程的执行 loop
                this.continueLoop = true;
                this.thread.Abort();
            }
            catch
            {
            }
        }

        /// <summary>
        /// 释放线程资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                //不再维持线程的 loop，退出线程的执行
                this.continueLoop = false;
                this.thread.Abort();
                //此处 Join
                this.thread.Join();
            }
            catch
            {
            }
        }

        private void CallStart()
        {
        OneLoop:
            try
            {

                while (true)
                {
                    //阻塞线程
                    locks.WaitOne();

                    try
                    {
                        if (this.asMethod != null)
                            this.asMethod(this.asArg);
                    }
                    catch (Exception ex)
                    {
                        bool tempGet = this.asMethod != null && this.asMethod.Method != null;
                        LogUnHandledException(ex.Message,
                            tempGet ? this.asMethod.Method.ReflectedType.Name : "None",
                            tempGet ? this.asMethod.Method.Name : "None");
                    }

                    //执行结束方法
                    HandleAbortMethod();
                    //重置 task
                    ResetMembers();
                    //执行结束的回调(在 ASync 中将把自身放回到池中)， 注意，此句应该放到逻辑的最后一处
                    HandleFinishedCallBack();
                }
            }
            catch (ThreadAbortException)
            {
                //这里要 ResetAbort，否则将在 catch 结束后继续抛出
                Thread.ResetAbort();

                //此处还要调用 HandleAbortMethod 方法，因为 asMethod 中有可能是个 while(true) 这样的循环，此时要退出 asMethod ，则必须使用 Thread.Abort
                HandleAbortMethod();
                //重置 task
                ResetMembers();
                //执行结束的回调(在 ASync 中将把自身放回到池中)， 注意，此句应该放到逻辑的最后一处
                HandleFinishedCallBack();

                //如果需要 loop， 则跳转，维持线程的执行
                if (this.continueLoop)
                    goto OneLoop;
            }
        }

        private void HandleAbortMethod()
        {
            try
            {
                if (this.abortExecuteMethod != null)
                    this.abortExecuteMethod(this.asArg);
            }
            catch (Exception ex)
            {
                bool tempGet = this.abortExecuteMethod != null && this.abortExecuteMethod.Method != null;
                LogUnHandledException(ex.Message,
                    tempGet ? this.abortExecuteMethod.Method.ReflectedType.Name : "None",
                    tempGet ? this.abortExecuteMethod.Method.Name : "None");
            }
        }

        private void HandleFinishedCallBack()
        {
            try
            {
                if (this.callBack != null)
                    this.callBack();
            }
            catch (Exception ex)
            {
                bool tempGet = this.callBack != null && this.callBack.Method != null;
                LogUnHandledException(ex.Message,
                    tempGet ? this.callBack.Method.ReflectedType.Name : "None",
                    tempGet ? this.callBack.Method.Name : "None");
            }
        }

        /// <summary>
        /// 重置 Task 的参数
        /// </summary>
        private void ResetMembers()
        {
            this.isThreadAlive = false;
            this.asArg = null;
            this.asMethod = null;
            this.abortExecuteMethod = null;
        }

        private void LogUnHandledException(string msg, string callerNM, string methodNM)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("********线程出现未处理异常*********");
            sb.Append("\r\n错误：" + msg);
            sb.Append(string.Format("\r\n调用者：{0} --> {1}", callerNM, methodNM));
            sb.Append("\r\n***********************************");

            Logger.WriteLog(LogMsgType.Error, sb.ToString());
        }

        /// <summary>
        /// 任务是否还在执行
        /// </summary>
        public bool IsAlive
        {
            get { return this.isThreadAlive; }
        }

        /// <summary>
        /// 获取 Task 的唯一ID，
        /// </summary>
        public int ID
        {
            get { return this.id; }
        }

        /// <summary>
        /// 获取 Task 对应的 Thread， 之所以发布出去， SleepHelper 中需要用到
        /// </summary>
        public Thread SThread
        {
            get { return this.thread; }
        }
    }
}
