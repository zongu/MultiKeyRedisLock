
namespace MultiKeyRedisLock
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 鎖物件實例
    /// </summary>
    public class RedisLck : IRedisLck
    {
        private int extendCount;

        private RedisLockFactory factory;

        private IEnumerable<string> lockIds;

        private IEnumerable<string> unAcquareLockIds;

        private bool anyKeyExistQuit;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="unAcquareLockIds"></param>
        /// <param name="extendCount"></param>
        /// <param name="lockIds"></param>
        public RedisLck(RedisLockFactory factory, IEnumerable<string> unAcquareLockIds, int extendCount, IEnumerable<string> lockIds, bool anyKeyExistQuit)
        {
            this.factory = factory;
            this.unAcquareLockIds = unAcquareLockIds.ToList();
            this.extendCount = extendCount;
            this.lockIds = lockIds.ToList();
            this.anyKeyExistQuit = anyKeyExistQuit;
        }

        /// <summary>
        /// 延長次數
        /// </summary>
        public int ExtendCount
            => this.extendCount;

        /// <summary>
        /// 是否獲得鎖
        /// </summary>
        public bool IsAcquired
            => !this.anyKeyExistQuit || !this.unAcquareLockIds.Any();

        /// <summary>
        /// 鎖ID
        /// </summary>
        public IEnumerable<string> LockIds
            => this.lockIds;

        /// <summary>
        /// 沒要到鎖的ID
        /// </summary>
        public IEnumerable<string> UnAcquareLockIds
            => this.unAcquareLockIds;

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (
                this.factory != null && IsAcquired && this.lockIds.Any())
            {
                // 排掉不是本次鎖的對象
                this.factory.LockRelease(this.lockIds.Except(this.unAcquareLockIds));
            }
        }
    }
}
