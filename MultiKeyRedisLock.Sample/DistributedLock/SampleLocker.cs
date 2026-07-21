
namespace MultiKeyRedisLock.Sample.DistributedLock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 分佈式鎖範例
    /// </summary>
    public class SampleLocker : ISampleLocker
    {
        /// <summary>
        /// 分佈式鎖工廠
        /// </summary>
        private RedisLockFactory factory;

        public SampleLocker(RedisLockFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// 加鎖
        /// </summary>
        /// <returns></returns>
        public IRedisLck GrabLock(IEnumerable<string> keys)
        {
            var resource = keys.Select(k => $"SampleLocker:{k}").ToList();
            return this.GrabLock(resource, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(300));
        }

        /// <summary>
        /// 加鎖
        /// </summary>
        /// <param name="resource">需要加鎖的KEY</param>
        /// <param name="ttl">鎖有效存活時間</param>
        /// <param name="waitTime">要不到鎖的等待間</param>
        /// <param name="retryTime">要不到鎖重新要鎖等待時間</param>
        /// <returns></returns>
        private IRedisLck GrabLock(IEnumerable<string> resource, TimeSpan ttl, TimeSpan waitTime, TimeSpan retryTime)
            => this.factory.CreateLock(resource, ttl, waitTime, retryTime);
    }
}
