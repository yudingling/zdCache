using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading;
using System.Globalization;

namespace ZdCache.Common
{
    /// <summary>
    /// 系统 event log 类型
    /// </summary>
    public enum EventLogSourceType
    {
        /// <summary>
        /// Core service 的 log
        /// </summary>
        RWRTUCore,

        /// <summary>
        /// Exchange service 的 log
        /// </summary>
        RWRTUExchange,

        /// <summary>
        /// Web 的 log
        /// </summary>
        RWRTUWeb
    }

    /// <summary>
    /// 日志查询 model
    /// </summary>
    public class QueryLogModel
    {
        public string EventTypeName { get; set; }
        public string EventTime { get; set; }
        public string EventMessage { get; set; }
    }

    public class EventLogMgr
    {
        private static string GetSourceName(EventLogSourceType sourceType)
        {
            return sourceType.ToString() + "服务";
        }

        /// <summary>
        /// 生成管理端日志。 此方法不能抛出异常
        /// </summary>
        /// <param name="sourceType">log 节点类型</param>
        /// <param name="logType">log类型</param>
        /// <param name="sEvent">日志内容</param>
        public static void adminLog(EventLogSourceType sourceType, LogMsgType logType, string sEvent)
        {
            try
            {
                string sourceTypeNM = GetSourceName(sourceType);

                if (!EventLog.SourceExists(sourceTypeNM))
                    EventLog.CreateEventSource(sourceTypeNM, sourceTypeNM);

                switch (logType)
                {
                    case LogMsgType.Info:
                        EventLog.WriteEntry(sourceTypeNM, sEvent, EventLogEntryType.Information, 234);
                        break;
                    case LogMsgType.Warn:
                        EventLog.WriteEntry(sourceTypeNM, sEvent, EventLogEntryType.Warning, 234);
                        break;
                    case LogMsgType.Error:
                        EventLog.WriteEntry(sourceTypeNM, sEvent, EventLogEntryType.Error, 234);
                        break;
                    default: break;
                }
            }
            catch
            {
            }
        }

        public static List<QueryLogModel> queryLog(EventLogSourceType sourceType, List<int> level, DateTime stm, DateTime etm, int maxCount)
        {
            List<QueryLogModel> retList = new List<QueryLogModel>();

            string sourceNM = GetSourceName(sourceType);

            string levelQueryStr = "";
            if (level.Count > 0)
            {
                for (int i = 0; i < level.Count - 1; i++)
                    levelQueryStr += "Level=" + level[i] + " or ";

                levelQueryStr += "Level=" + level[level.Count - 1];

                levelQueryStr = string.Format("({0}) and ", levelQueryStr);
            }

            string queryStr = string.Format("<QueryList><Query Id=\"0\" Path=\"{0}\"><Select Path=\"{0}\">*[System[{1}TimeCreated[@SystemTime&gt;='{2}' and @SystemTime&lt;='{3}']]]</Select></Query></QueryList>",
                sourceNM,
                levelQueryStr,
                stm.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                etm.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));



            //a bug in .net framework， http://support.microsoft.com/kb/2829534/zh-cn
            //需要设置 culture 为 en-us， 否则下面的 eventInstance.FormatDescription() 会获取为空
            var beforeCulture = Thread.CurrentThread.CurrentCulture;
            var newCulture = new CultureInfo("en-US");
            try
            {
                EventLogQuery logq = new EventLogQuery(sourceNM, PathType.LogName, queryStr);
                logq.ReverseDirection = true;

                EventLogReader logReader = new EventLogReader(logq);

                int rowCount = 0;
                for (EventRecord eventInstance = logReader.ReadEvent();
                    eventInstance != null; eventInstance = logReader.ReadEvent(), rowCount++)
                {
                    if (rowCount >= maxCount)
                        break;

                    QueryLogModel model = new QueryLogModel()
                    {
                        EventTypeName = eventInstance.LevelDisplayName,
                        EventTime = eventInstance.TimeCreated == null ? "" : ((DateTime)eventInstance.TimeCreated).ToString("yyyy-MM-dd HH:mm:ss"),

                    };

                    //注意，这个之所以放到循环之内，是因为 eventInstance.LevelDisplayName 得正常获取，则需要当前的 culture
                    Thread.CurrentThread.CurrentCulture = newCulture;
                    model.EventMessage = eventInstance.FormatDescription();
                    Thread.CurrentThread.CurrentCulture = beforeCulture;

                    retList.Add(model);
                }

                return retList;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = beforeCulture;
            }
        }
    }
}