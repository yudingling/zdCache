using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace ZdCache.Common
{
    /// <summary>
    /// log 信息类型
    /// </summary>
    public enum LogMsgType
    {
        /// <summary>
        /// 信息提示
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warn,

        /// <summary>
        /// 异常
        /// </summary>
        Error
    }

    /// <summary>
    /// log 信息实体
    /// </summary>
    internal class LogEntity
    {
        public LogMsgType MsgType { get; set; }
        public string Message { get; set; }

        public LogEntity(LogMsgType logType, string logMsg)
        {
            this.MsgType = logType;
            this.Message = logMsg;
        }
    }

    /// <summary>
    /// log 记录类
    /// 使用方式：
    ///     Logger.WriteLog(logstr);
    ///     Logger.WriteLog(logfilePreName, logstr); //给log 生成的文件添加 logfilePreName 前缀
    ///     Logger.WriteLog(logType, logstr);
    ///     Logger.WriteLog(logType, logfilePreName, logstr);
    /// </summary>
    public class Logger
    {
        //log 路径(默认)
        private static string logDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

        private static object LockObj = new object();
        private static ConcurrentDictionary<string, ConcurrentQueue<LogEntity>> _msgs = new ConcurrentDictionary<string, ConcurrentQueue<LogEntity>>();

        private static string defaultPreName = "Default";

        private static bool running = true;
        //写文件线程的最大数量, 一个 PreName 对应一个线程
        private const int MaxWriteThreadCount = 4;
        //存储等待处理的 preName
        private static MyConcurrentList<string> waitForHandleQueue = new MyConcurrentList<string>();

        private static AsyncCall logMonitorCall = null;
        private static List<AsyncCall> fileWriteCall = new List<AsyncCall>();

        /// <summary>
        /// 构造函数
        /// </summary>
        static Logger()
        {
            running = true;
            logMonitorCall = new AsyncCall(new AsyncMethod(LogMonitor), null, true, null);
        }

        /// <summary>
        /// 使用指定的log路径进行初始
        /// </summary>
        /// <param name="directory"></param>
        public static void Init(string directory)
        {
            lock (LockObj)
            {
                if (logMonitorCall != null)
                {
                    running = false;
                    logMonitorCall.Stop();
                }

                logDirectory = directory;

                running = true;
                logMonitorCall = new AsyncCall(new AsyncMethod(LogMonitor), null, true, null);
            }
        }

        /// <summary>
        /// 记录log (默认为错误信息, 不带文件头前缀)
        /// </summary>
        /// <param name="log"></param>
        public static void WriteLog(string log)
        {
            WriteLog(LogMsgType.Error, log);
        }

        /// <summary>
        /// 记录log (默认为错误信息)
        /// </summary>
        /// <param name="log"></param>
        public static void WriteLog(string logFilePreName, string log)
        {
            WriteLog(LogMsgType.Error, logFilePreName, log);
        }

        /// <summary>
        /// 记录log （不带文件头前缀）
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="log"></param>
        public static void WriteLog(LogMsgType logType, string log)
        {
            try
            {
                if (running)
                    GetLogList(defaultPreName).Enqueue(new LogEntity(logType, string.Format("{0}：{1}\r\n", DateTime.Now.ToString("HH:mm:ss"), log)));
            }
            catch
            {
            }
        }

        /// <summary>
        /// 记录log （带文件头前缀，用以区分不同log类型）
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="logFilePreName">log文件名前缀</param>
        /// <param name="log"></param>
        public static void WriteLog(LogMsgType logType, string logFilePreName, string log)
        {
            try
            {
                if (running)
                    GetLogList(logFilePreName).Enqueue(new LogEntity(logType, string.Format("{0}：{1}\r\n", DateTime.Now.ToString("HH:mm:ss"), log)));
            }
            catch
            {
            }
        }

        /// <summary>
        /// 获取 logFilePreName 对应的存储 List
        /// </summary>
        private static ConcurrentQueue<LogEntity> GetLogList(string logFilePreName)
        {
            //如果 logFilePreName 为空则取默认的
            string tmpLogFileNM = (logFilePreName != null && logFilePreName.Length > 0) ? logFilePreName : defaultPreName;

            if (!_msgs.ContainsKey(tmpLogFileNM))
            {
                _msgs.TryAdd(tmpLogFileNM, new ConcurrentQueue<LogEntity>());
            }

            return _msgs[tmpLogFileNM];
        }

        /// <summary>
        /// 结束 log 线程
        /// </summary>
        public static void CloseLog()
        {
            //设置停止标识
            running = false;

            //保证全部处理完
            bool finished = false;
            while (!finished)
            {
                finished = true;
                foreach (string key in _msgs.Keys)
                {
                    if (_msgs[key].Count > 0)
                    {
                        finished = false;
                        SleepHelper.Sleep(10);
                        break;
                    }
                }
            }

            //停止线程
            foreach (AsyncCall call in fileWriteCall)
                call.Stop();
            fileWriteCall.Clear();

            logMonitorCall.Stop();
            logMonitorCall = null;

            _msgs.Clear();
        }

        /// <summary>
        /// log 监听线程
        /// </summary>
        /// <param name="arg"></param>
        private static void LogMonitor(AsyncArgs arg)
        {
            while (true)
            {
                try
                {
                    if (_msgs.Count > 0)
                    {
                        //检查log文件数
                        CheckLogCount(_msgs.Keys, 5);

                        foreach (string preName in _msgs.Keys)
                        {
                            //添加到 waitForHandleQueue
                            if (_msgs[preName].Count > 0 && !waitForHandleQueue.Contains(preName))
                            {
                                waitForHandleQueue.Append(preName);

                                //判断当前已存在的写线程数
                                if (fileWriteCall.Count < MaxWriteThreadCount)
                                {
                                    fileWriteCall.Add(new AsyncCall(new AsyncMethod(WriteToFile), null, true, null));
                                }
                            }
                        }
                    }

                    //2秒轮询一次
                    SleepHelper.Sleep(2000);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 写文件线程
        /// </summary>
        /// <param name="arg"></param>
        private static void WriteToFile(AsyncArgs arg)
        {
            LogEntity entity = null;
            string currentPreName = string.Empty;

            while (true)
            {
                try
                {
                    //从待处理的队列中取出一个，如果不存在待处理的，则等待1s
                    if (waitForHandleQueue.RemoveFirst(out currentPreName))
                    {
                        ConcurrentQueue<LogEntity> currentQueue = GetLogList(currentPreName);
                        if (currentQueue != null && currentQueue.Count > 0)
                        {
                            using (FileStream fs = GetFileStream(currentPreName))
                            {
                                fs.Position = fs.Length;
                                using (StreamWriter sw = new StreamWriter(fs))
                                {
                                    while (currentQueue.TryDequeue(out entity))
                                    {
                                        sw.Write(entity.Message);
                                    }

                                    //flush out
                                    sw.Flush();
                                    sw.Close();
                                }
                                fs.Close();
                            }
                        }
                    }
                    else
                        SleepHelper.Sleep(1000);
                }
                catch
                {
                    //追加到系统日志中
                    //EventLogMgr.adminLog(LogMsgType.Error,
                    //    string.Format("{0}：文件log发生错误：{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), err.Message));

                    //出错了，也睡眠 1s
                    SleepHelper.Sleep(1000);
                }
            }
        }

        private static FileStream GetFileStream(string preName)
        {
            string tmpName = preName == defaultPreName ? "" : preName;
            string fileNM = string.Format("{0}\\Log{1}{2}.txt", logDirectory, tmpName, DateTime.Now.ToString("yyyyMMdd"));

            return new FileStream(fileNM, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        }

        //检查日志数量
        private static void CheckLogCount(ICollection<string> preNameList, int dayNum)
        {
            string oldLogDate = DateTime.Now.AddDays(-1 * dayNum).ToString("yyyyMMdd");
            DirectoryInfo drInfo = new DirectoryInfo(logDirectory);
            if (!drInfo.Exists)
                drInfo.Create();
            foreach (FileInfo file in drInfo.GetFiles("*.txt"))
            {
                foreach (string preLogName in preNameList)
                {
                    string tmpName = preLogName == defaultPreName ? "" : preLogName;
                    if (file.Name.Length == 15 + tmpName.Length
                        && file.Name.StartsWith("Log" + tmpName)
                        && string.Compare(file.Name.Substring(3 + tmpName.Length, 8), oldLogDate) < 0)
                        file.Delete();
                }
            }
        }
    }
}
