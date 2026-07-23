
namespace MultiKeyRedisLock.Tests
{
    using StackExchange.Redis;

    [TestClass]
    public sealed class RedisLockFactoryTests
    {
        private RedisLockFactory factory;

        private const string redisConn = @"localhost:6379";

        [TestInitialize]
        public void Init()
        {
            var conn = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConn));
            var redis = conn.GetDatabase(15);

            var keys = conn.GetServer(redisConn).Keys(15, $"RedisLock*", 10, CommandFlags.None).ToList();
            keys.ForEach(key => redis.KeyDelete(key));

            this.factory = new RedisLockFactory(conn);
        }

        [DoNotParallelize]
        [TestMethod]
        public void 加鎖測試()
        {
            var lck = this.factory.CreateLock(new[] { "TEST001" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300));
            Assert.IsTrue(lck.IsAcquired);

            lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300));
            Assert.IsFalse(lck.IsAcquired);
        }

        [DoNotParallelize]
        [TestMethod]
        public void 釋放鎖測試()
        {
            using (var lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300)))
            {
                Assert.IsTrue(lck.IsAcquired);
            }

            using (var lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300)))
            {
                Assert.IsTrue(lck.IsAcquired);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public void 取不到鎖測試()
        {
            using (var lck1 = this.factory.CreateLock(new[] { "TEST001" }, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(300)))
            using (var lck2 = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(300)))
            {
                Assert.IsTrue(lck1.IsAcquired);
                Assert.IsFalse(lck2.IsAcquired);
                Assert.AreEqual(lck2.ExtendCount, 4);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public void 排除要不到鎖的對象加鎖測試()
        {
            var lck = this.factory.CreateLock(new[] { "TEST001" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), false);
            Assert.IsTrue(lck.IsAcquired);

            lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), false);
            Assert.IsTrue(lck.IsAcquired);
            Assert.IsTrue(lck.UnAcquareLockIds.Any());
            Assert.AreEqual(lck.UnAcquareLockIds.Count(), 1);
            Assert.AreEqual(lck.UnAcquareLockIds.First(), "TEST001");
        }

        [DoNotParallelize]
        [TestMethod]
        public void 排除要不到鎖的對象釋放鎖測試()
        {
            using (var lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), false))
            {
                Assert.IsTrue(lck.IsAcquired);
            }

            using (var lck = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromSeconds(600), TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), false))
            {
                Assert.IsTrue(lck.IsAcquired);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public void 排除要不到鎖的對象持續鎖測試()
        {
            using (var lck1 = this.factory.CreateLock(new[] { "TEST001" }, TimeSpan.FromMilliseconds(60000), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(300), false))
            using (var lck2 = this.factory.CreateLock(new[] { "TEST001", "TEST002" }, TimeSpan.FromMilliseconds(60000), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(300), false))
            {
                Assert.IsTrue(lck1.IsAcquired);
                Assert.IsFalse(lck1.UnAcquareLockIds.Any());

                Assert.IsTrue(lck2.IsAcquired);
                Assert.IsTrue(lck2.UnAcquareLockIds.Any());
                Assert.AreEqual(lck2.UnAcquareLockIds.Count(), 1);
                Assert.AreEqual(lck2.UnAcquareLockIds.First(), "TEST001");
            }
        }
    }
}
