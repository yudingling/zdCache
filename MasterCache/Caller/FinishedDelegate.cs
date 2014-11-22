using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;

namespace ZdCache.MasterCache.Caller
{
    public delegate void FinishedDelegate(Guid id, bool success, object obj);
}
