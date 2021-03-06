﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// 接收到数据后的回调委托, 此回调应该自行处理所有异常，不应抛出
    /// </summary>
    /// <param name="tokenID"></param>
    /// <param name="data"></param>
    public delegate void PorterReceive(int tokenID, List<byte[]> data);

    /// <summary>
    /// porter 错误类型
    /// </summary>
    public enum ErrorType { Receive, Send, Other }

    /// <summary>
    /// 输出log 的委托，此回调应该自行处理所有异常，不应抛出
    /// </summary>
    public delegate void ErrorTracer(ErrorType errorType, int tokenID, string msg);


    /// <summary>
    /// 用于获取 UserTooken 的唯一标志
    /// </summary>
    internal class IDGetter
    {
        private static int tookenID = 0;

        internal static int GetID()
        {
            return Interlocked.Increment(ref tookenID);
        }
    }
}
