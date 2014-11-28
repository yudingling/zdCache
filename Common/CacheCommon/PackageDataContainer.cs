using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using ZdCache.Common.ActionModels;
using System.Threading.Tasks;

namespace ZdCache.Common.CacheCommon
{
    /// <summary>
    /// 合并包数据的容器。
    /// </summary>
    public class PackageDataContainer : IDisposable
    {
        //key 为 PackageDataArg.id
        private ConcurrentDictionary<Guid, PackageDataArg> allBuiltArgs = new ConcurrentDictionary<Guid, PackageDataArg>();

        //无效数据检查
        AsyncCall clearUnsedPackageCall;

        public PackageDataContainer()
        {
            this.clearUnsedPackageCall = new AsyncCall(new AsyncMethod(ClearUnsedPackage), null, true, null);
        }

        /// <summary>
        /// 添加被拆分的包数据(argObj 实体)
        /// </summary>
        public void AddBuiltData(Guid callId, int totalCount, int order, List<byte[]> data)
        {
            PackageDataArg tempArg = GetCurrentPackageDataArg(callId, totalCount);

            if (tempArg != null)
                tempArg.AddPackage(order, data);
        }

        /// <summary>
        /// 添加被拆分的包数据(头部信息)
        /// </summary>
        public void AddBuiltData(Guid callId, int totalCount, int order,
            ActionKind acKind, ActionResult acResult, string dataTypeName, byte category, string argID)
        {
            PackageDataArg tempArg = GetCurrentPackageDataArg(callId, totalCount);

            //增加一个空的数据列表
            if (tempArg != null)
                tempArg.AddPackage(order, new List<byte[]>(), acKind, acResult, dataTypeName, category, argID);
        }

        private PackageDataArg GetCurrentPackageDataArg(Guid callId, int totalCount)
        {
            PackageDataArg tempArg = null;
            if (!this.allBuiltArgs.TryGetValue(callId, out tempArg))
            {
                //注意此处的写法，一定要保证数据来自于字典中, 但不要采用 tryadd 紧接着再 tryget，那样在最坏的情况下效率减半
                tempArg = new PackageDataArg(callId, totalCount);
                if (!this.allBuiltArgs.TryAdd(callId, tempArg))
                {
                    tempArg = null;
                    this.allBuiltArgs.TryGetValue(callId, out tempArg);
                }
            }

            return tempArg;
        }

        /// <summary>
        /// 判断 callID 指定的数据包是否都接收全了
        /// </summary>
        public bool IsAllPackectBuilt(Guid callID)
        {
            PackageDataArg arg;
            if (this.allBuiltArgs.TryGetValue(callID, out arg))
                return arg.IsAllPackectBuilt;
            else
                return true;  //如果没有该 id， 则也认为是接收全的
        }

        /// <summary>
        /// 清除 callID 标识对应的所有拆分包数据
        /// </summary>
        /// <param name="id"></param>
        public PackageDataArg ClearBuiltData(Guid callID)
        {
            PackageDataArg arg;
            this.allBuiltArgs.TryRemove(callID, out arg);
            return arg;
        }

        /// <summary>
        /// 清除无效的数据。 此处的时间间隔取 CallTimeOut 即可，因为如果超时了，则这个数据肯定是不再使用了
        /// </summary>
        /// <param name="arg"></param>
        private void ClearUnsedPackage(AsyncArgs arg)
        {
            while (true)
            {
                try
                {
                    DateTime cmpTM = DateTime.Now;
                    Parallel.ForEach<PackageDataArg>(this.allBuiltArgs.Values, model =>
                    {
                        if ((cmpTM - model.UpdateTM).TotalMilliseconds > ConstParams.UnusePackageDataExpireTM)
                            ClearBuiltData(model.CallID);
                    });
                }
                catch
                {
                }

                SleepHelper.Sleep(ConstParams.UnusePackageDataExpireTM);
            }
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (this.clearUnsedPackageCall != null)
                this.clearUnsedPackageCall.Stop();
        }

        #endregion
    }
}
