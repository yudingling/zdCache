using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.SlaveCache.LocalAction
{
    /// <summary>
    /// 默认的缓存本地化实现，暂时未对本地化做操作
    /// </summary>
    public class DefaultLocalAction : ILocalAction
    {
        #region ILocalAction 成员

        public void ExecLocalAction(LocalActionType localAcType, Common.CDataType.ICacheDataType data)
        {
            switch (localAcType)
            {
                case LocalActionType.Set:
                    //执行 set 时的操作，比如 add 到数据库， 保存到本地文件等等..
                    break;

                case LocalActionType.Delete:
                    break;

                default:
                    break;
            }
        }

        #endregion
    }
}
