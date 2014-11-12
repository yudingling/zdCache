using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace ZdCache.Common.DefferCallBack
{
    public class Deffered
    {
        private ConcurrentDictionary<Guid, Promise> promiseDic = new ConcurrentDictionary<Guid, Promise>();

        /// <summary>
        /// 创建 promise
        /// </summary>
        /// <param name="callID"></param>
        /// <param name="callBackTypes"></param>
        /// <returns></returns>
        public Promise CreatePromise(Guid callID, params Type[] callBackTypes)
        {
            Promise promise = null;
            if (!this.promiseDic.TryGetValue(callID, out promise))
            {
                //注意此处的写法，一定要保证数据来自于字典中, 但不要采用 tryadd 紧接着再 tryget，那样在最坏的情况下效率减半
                promise = new Promise(callBackTypes);
                if (!this.promiseDic.TryAdd(callID, promise))
                {
                    promise = null;
                    this.promiseDic.TryGetValue(callID, out promise);
                }
            }

            return promise;
        }

        /// <summary>
        /// 触发回调。
        /// 对于一个 promise 来说，任意一个回调类型被 emit 了，则将从 deffered 中剔除（deffered 状态的唯一性）
        /// </summary>
        public void Emit(Guid callID, Type callBackType, params object[] args)
        {
            Promise promise = null;
            //执行并删除对应的 promise
            if (this.promiseDic.TryRemove(callID, out promise))
            {
                promise.Emit(callBackType, args);
            }
        }
    }
}
