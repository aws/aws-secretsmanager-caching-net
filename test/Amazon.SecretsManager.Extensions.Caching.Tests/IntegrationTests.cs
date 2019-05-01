namespace Amazon.SecretsManager.Extensions.Caching.Tests
{
    using Moq;
    using Xunit;
    using Amazon.SecretsManager.Model;
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class SecretStringTests : IDisposable
    {
        private IAmazonSecretsManager client = new AmazonSecretsManagerClient();
        private SecretsManagerCache cache;
        private String testSecretName = System.Guid.NewGuid().ToString();
        private String testSecretString = System.Guid.NewGuid().ToString();

        // Runs before each individual test
        public SecretStringTests()
        {
            client.CreateSecretAsync(new CreateSecretRequest { Name = testSecretName, SecretString = testSecretString });
        }

        // Cleans up after each individual test
        public void Dispose()
        {
            client.DeleteSecretAsync(new DeleteSecretRequest { SecretId = testSecretName, ForceDeleteWithoutRecovery = true });
        }

        [Fact]
        public void GetSecretStringTest()
        {
            cache = new SecretsManagerCache(client);
            Assert.Equal(cache.GetSecretString(testSecretName).Result, testSecretString);
        }

        [Fact]
        public void SecretCacheTTLTest()
        {
            cache = new SecretsManagerCache(client, new SecretCacheConfiguration { CacheItemTTL = 1000 });
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            // Even though the secret is updated, the cached version should be retrieved
            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);

            Thread.Sleep(1000);

            // Cached secret string should be expired and the updated secret string retrieved
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public void SecretCacheRefreshTest()
        {
            cache = new SecretsManagerCache(client);
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);
            Assert.True(cache.RefreshNowAsync(testSecretName).Result);
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public void NoSecretBinaryTest()
        {
            cache = new SecretsManagerCache(client);
            Assert.Null(cache.GetSecretBinary(testSecretName));
        }

        class TestHook : ISecretCacheHook
        {
            private Dictionary<int, object> dictionary = new Dictionary<int, object>();
            public object Get(object cachedObject)
            {
                return dictionary[(int)cachedObject];
            }

            public object Put(object o)
            {
                int key = dictionary.Count;
                dictionary.Add(key, o);
                return key;
            }

            public int GetCount()
            {
                return dictionary.Count;
            }
        }

        [Fact]
        public void CacheHookTest()
        {
            TestHook testHook = new TestHook();
            cache = new SecretsManagerCache(client, new SecretCacheConfiguration { CacheHook = testHook });
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
        }
    }

    public class SecretBinaryTests : IDisposable
    {
        private IAmazonSecretsManager client = new AmazonSecretsManagerClient();
        private SecretsManagerCache cache;
        private String testSecretName = System.Guid.NewGuid().ToString();
        private MemoryStream testSecretBinary = new MemoryStream(Enumerable.Repeat((byte)0x20, 10).ToArray());

        // Runs before each individual test
        public SecretBinaryTests()
        {
            client.CreateSecretAsync(new CreateSecretRequest { Name = testSecretName, SecretBinary = testSecretBinary });
        }

        // Cleans up after each individual test
        public void Dispose()
        {
            client.DeleteSecretAsync(new DeleteSecretRequest { SecretId = testSecretName, ForceDeleteWithoutRecovery = true });
        }

        [Fact]
        public void GetSecretBinaryTest()
        {
            cache = new SecretsManagerCache(client);
            Assert.Equal(cache.GetSecretBinary(testSecretName).Result, testSecretBinary.ToArray());
        }

        [Fact]
        public void NoSecretStringTest()
        {
            cache = new SecretsManagerCache(client);
            Assert.Null(cache.GetSecretString(testSecretName));
        }
    }
}
