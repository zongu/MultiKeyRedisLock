
namespace MultiKeyRedisLock
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 鎖物件介面
    /// </summary>
    public interface IRedisLck : IDisposable
    {
        /// <summary>
        /// 延長次數
        /// </summary>
        int ExtendCount { get; }

        /// <summary>
        /// 是否獲得鎖
        /// </summary>
        bool IsAcquired { get; }

        /// <summary>
        /// 鎖ID
        /// </summary>
        IEnumerable<string> LockIds { get; }

        /// <summary>
        /// 沒要到鎖的ID
        /// </summary>
        IEnumerable<string> UnAcquareLockIds { get; }
    }
}
