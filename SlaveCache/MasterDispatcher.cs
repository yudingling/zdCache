using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common;
using ZdCache.Common.ActionModels;
using ZdCache.Common.CDataType;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache
{
    /// <summary>
    /// 转发器(将数据转发至 master)
    /// </summary>
    internal class MasterDispatcher : IDisposable
    {
        private ConcurrentDictionary<string, ClientBinding> allBindings = new ConcurrentDictionary<string, ClientBinding>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorOccured">发送出错时的回调</param>
        public MasterDispatcher()
        {
        }

        /// <summary>
        /// 添加binding 到 dispatcher
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public void AddBinding(ClientBinding binding)
        {
            if (!this.allBindings.TryAdd(binding.ID, binding))
                throw new Exception(string.Format("AddBinding 出错，存在 id 一致的 binding [{0}]！", binding.ID));
        }

        /// <summary>
        /// 从 dispatcher 中移除 binding
        /// </summary>
        /// <param name="bingingId"></param>
        public void DeleteBinding(string bingingId)
        {
            ClientBinding temp;
            //删除，并释放 clientbinding
            if (this.allBindings.TryRemove(bingingId, out temp))
                temp.Close();
        }

        /// <summary>
        /// 分发数据到指定master
        /// </summary>
        public void Dispatch(string bindingId, CallArgsModel model)
        {
            ClientBinding binding;
            if (this.allBindings.TryGetValue(bindingId, out binding))
            {
                CommonFunc.SendCall(binding, model);
            }
        }

        /// <summary>
        /// 分发数据到指定master
        /// </summary>
        public void Dispatch(ClientBinding binding, CallArgsModel model)
        {
            CommonFunc.SendCall(binding, model);
        }

        /// <summary>
        /// 分发数据到所有master
        /// </summary>
        public void Dispatch(CallArgsModel model)
        {
            List<string> tempList = new List<string>();
            Parallel.ForEach<ClientBinding>(this.allBindings.Values, binding =>
            {
                CommonFunc.SendCall(binding, model);
            });
        }

        #region IDisposable 成员

        public void Dispose()
        {
            foreach (ClientBinding binding in this.allBindings.Values)
                binding.Close();
        }

        #endregion
    }
}
