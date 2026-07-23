
namespace MultiKeyRedisLock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using StackExchange.Redis;

    /// <summary>
    /// 分佈式鎖工廠(持久層)
    /// </summary>
    public class RedisLockFactory
    {
        private ConnectionMultiplexer conn;

        private readonly string affixKey = "RedisLock";

        private readonly int dataBase = 15;

        public RedisLockFactory(ConnectionMultiplexer conn)
        {
            this.conn = conn;
        }

        /// <summary>
        /// 加鎖
        /// </summary>
        /// <param name="resource">鎖KEY</param>
        /// <param name="ttl">鎖保留時間</param>
        /// <param name="waitTime">可被等待時間</param>
        /// <param name="retryTime">檢查間隔時間</param>
        /// <param name="anyKeyExistQuit">任一鎖要不到就停止</param>
        /// <returns></returns>
        public IRedisLck CreateLock(IEnumerable<string> resource, TimeSpan ttl, TimeSpan waitTime, TimeSpan retryTime, bool anyKeyExistQuit = true)
        {
            if (ttl < TimeSpan.Zero || ttl > TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl), "The value of ttl in milliseconds is negative or is greater than MaxValue");
            }

            if (waitTime < TimeSpan.Zero || waitTime > TimeSpan.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(waitTime), "The value of waitTime in milliseconds is negative or is greater than MaxValue");
            }

            if (retryTime < TimeSpan.Zero || retryTime > waitTime)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTime), "The value of retryTime in milliseconds is negative or is greater than waitTime");
            }

            var unAcquareResource = Enumerable.Empty<string>();
            int extendCount = 1;
            DateTime expireDateTime = DateTime.Now + waitTime;

            while (
                // 嘗試加鎖
                !this.TryInsert(resource.Select(r => (r, ttl.TotalSeconds)).ToList(), ref unAcquareResource, anyKeyExistQuit) &&
                // 如果超過等待時間就不繼續做了
                expireDateTime > DateTime.Now + retryTime)
            {
                extendCount++;
                SpinWait.SpinUntil(() => false, retryTime);
            }

            return new RedisLck(this, unAcquareResource.ToList(), extendCount, resource.ToList(), anyKeyExistQuit);
        }

        /// <summary>
        /// 釋放鎖
        /// </summary>
        /// <param name="lockId"></param>
        public void LockRelease(IEnumerable<string> lockIds)
        {
            try
            {
                this.UseConnection(redis =>
                {
                    redis.KeyDelete(lockIds.Select(p => (RedisKey)$"{this.affixKey}:{p}").ToArray());

                    return true;
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// 嘗試更新
        /// </summary>
        /// <param name="lockAcquires"></param>
        /// <param name="unAcquareResource"></param>
        /// <param name="anyKeyExistQuit"></param>
        /// <returns></returns>
        private bool TryInsert(IEnumerable<(string lockId, double expireSeconds)> lockAcquires, ref IEnumerable<string> unAcquareResource, bool anyKeyExistQuit)
        {
            try
            {
                var executeResult = this.UseConnection(redis =>
                {
                    var keys = lockAcquires.Select(la => (RedisKey)$"{this.affixKey}:{la.lockId}").ToArray();
                    var values =
                        new RedisValue[] { anyKeyExistQuit ? "1" : "0" }
                        .Concat(lockAcquires.Select(la => new RedisValue[] { (RedisValue)la.lockId, (RedisValue)la.expireSeconds }).SelectMany(p => p)).ToArray();

                    var script = @"
                        local existValues = {}
                        local existSet = {}
                        local anyKeyExistQuit = ARGV[1] == '1'
                        
                        for i = 1, #KEYS do
                            local val = redis.call('GET', KEYS[i])

                            if val then
                                existSet[tostring(KEYS[i])] = true
                                table.insert(existValues, val)
                            end
                        end
                        
                        if anyKeyExistQuit and #existValues > 0 then
                            return existValues
                        else
                            for i = 1, #KEYS do
                                if not existSet[tostring(KEYS[i])] then
                                    redis.call('SET', KEYS[i], ARGV[2 * i], 'EX', ARGV[2 * i + 1])
                                end
                            end
                        
                            if anyKeyExistQuit then
                                return nil
                            else
                                return existValues
                            end
                        end
                    ";

                    var result = redis.ScriptEvaluate(script, keys, values);

                    return result.IsNull ? Enumerable.Empty<string>() : ((RedisResult[])result).Select(p => p.ToString()).ToList();
                });

                unAcquareResource = executeResult.ToList();

                return !anyKeyExistQuit || !executeResult.Any();
            }
            catch
            {
                throw;
            }
        }

        private T UseConnection<T>(Func<IDatabase, T> func)
        {
            var redis = this.conn.GetDatabase(this.dataBase);
            return func(redis);
        }
    }
}
