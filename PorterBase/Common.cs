using System;
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
    /// 输出log 的委托，此回调应该自行处理所有异常，不应抛出
    /// </summary>
    /// <param name="msg"></param>
    public delegate void ErrorTracer(string msg);


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
