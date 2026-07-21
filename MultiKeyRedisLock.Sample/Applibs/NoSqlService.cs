
namespace MultiKeyRedisLock.Sample.Applibs
{
    using System;
    using StackExchange.Redis;

    /// <summary>
    /// 非關連式資料庫服務
    /// </summary>
    internal class NoSqlService
    {
        const string redisConn = @"localhost:6379";

        private static Lazy<ConnectionMultiplexer> lazyRedisConnections;

        public static ConnectionMultiplexer RedisConnections
        {
            get
            {
                if (lazyRedisConnections == null)
                {
                    lazyRedisConnections = new Lazy<ConnectionMultiplexer>(() =>
                    {
                        var maxTimeout = 5 * 1000;
                        var options = ConfigurationOptions.Parse(redisConn);
                        options.AbortOnConnectFail = false;
                        options.ClientName = "RedisLock";
                        options.SyncTimeout = maxTimeout;
                        options.AsyncTimeout = maxTimeout;

                        var muxer = ConnectionMultiplexer.Connect(options);
                        muxer.ConnectionFailed += (sender, e) =>
                        {
                            Console.WriteLine("redis failed: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                        };
                        muxer.ConnectionRestored += (sender, e) =>
                        {
                            Console.WriteLine("redis restored: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                        };

                        return muxer;
                    });
                }

                return lazyRedisConnections.Value;
            }
        }

        private static Lazy<RedisLockFactory> lazyDistributedLockService;

        public static RedisLockFactory DistributedLockService
        {
            get
            {
                if (lazyDistributedLockService == null)
                {
                    lazyDistributedLockService = new Lazy<RedisLockFactory>(() =>
                    {
                        return new RedisLockFactory(RedisConnections);
                    });
                }

                return lazyDistributedLockService.Value;
            }
        }
    }
}
