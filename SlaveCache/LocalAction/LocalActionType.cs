using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.SlaveCache.LocalAction
{
    /// <summary>
    /// 本地化操作类型
    /// </summary>
    public enum LocalActionType
    {
        /// <summary>
        /// 设置（添加）
        /// </summary>
        Set,

        /// <summary>
        /// 查找
        /// </summary>
        Get,

        /// <summary>
        /// 更新
        /// </summary>
        Update,

        /// <summary>
        /// 删除
        /// </summary>
        Delete,

        /// <summary>
        /// 过期
        /// </summary>
        Expire
    }
}
