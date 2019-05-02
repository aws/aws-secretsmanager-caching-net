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
    using Amazon.Runtime;

    // Performs test secret cleanup before and after integ tests are run
    public abstract class TestBase : IDisposable
    {
        public static IAmazonSecretsManager Client = new AmazonSecretsManagerClient();
        public static String TestSecretPrefix = "IntegTest";
        public static List<String> SecretNamesToDelete = new List<String>();

        public TestBase()
        {
            FindPreviousTestSecrets();
            DeleteSecrets();
        }

        public void Dispose()
        {
            DeleteSecrets();
        }

        private void FindPreviousTestSecrets()
        {
            String nextToken = null;
            do
            {
                var response = TestBase.Client.ListSecrets(new ListSecretsRequest { NextToken = nextToken });
                nextToken = response.NextToken;
                List<SecretListEntry> secretList = response.SecretList;
                foreach (SecretListEntry secret in secretList)
                {
                    if (secret.Name.StartsWith(TestSecretPrefix))
                    {
                        SecretNamesToDelete.Add(secret.Name);
                    }
                }
            } while (nextToken != null);
        }

        private void DeleteSecrets()
        {
            foreach (String secretName in SecretNamesToDelete)
            {
                TestBase.Client.DeleteSecret(new DeleteSecretRequest { SecretId = secretName });
            }
            SecretNamesToDelete.Clear();
        }
    }

    public class IntegrationTests : IClassFixture<TestBase>
    {
        private SecretsManagerCache cache;
        private String testSecretString = System.Guid.NewGuid().ToString();
        private MemoryStream testSecretBinary = new MemoryStream(Enumerable.Repeat((byte)0x20, 10).ToArray());
        
        private enum TestType { SecretString = 0, SecretBinary = 1 };

        private String Setup(TestType type)
        {
            String testSecretName = TestBase.TestSecretPrefix + Guid.NewGuid().ToString();
            CreateSecretRequest req = null;

            if (type == TestType.SecretString)
            {
                req = new CreateSecretRequest { Name = testSecretName, SecretString = testSecretString };
            }
            else if (type == TestType.SecretBinary)
            {
                req = new CreateSecretRequest { Name = testSecretName, SecretBinary = testSecretBinary };
            }

            TestBase.Client.CreateSecret(req);
            TestBase.SecretNamesToDelete.Add(testSecretName);
            return testSecretName;
        }

        [Fact]
        public void GetSecretStringTest()
        {
            String testSecretName = Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Equal(cache.GetSecretString(testSecretName).Result, testSecretString);
        }

        [Fact]
        public void SecretCacheTTLTest()
        {
            String testSecretName = Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client, new SecretCacheConfiguration { CacheItemTTL = 1000 });
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            // Even though the secret is updated, the cached version should be retrieved
            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);

            Thread.Sleep(1000);

            // Cached secret string should be expired and the updated secret string retrieved
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public void SecretCacheRefreshTest()
        {
            String testSecretName = Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);
            Assert.True(cache.RefreshNowAsync(testSecretName).Result);
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public void NoSecretBinaryTest()
        {
            String testSecretName = Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(cache.GetSecretBinary(testSecretName));
        }

        [Fact]
        public void GetSecretBinaryTest()
        {
            String testSecretName = Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Equal(cache.GetSecretBinary(testSecretName).Result, testSecretBinary.ToArray());
        }

        [Fact]
        public void NoSecretStringTest()
        {
            String testSecretName = Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(cache.GetSecretString(testSecretName));
        }

        [Fact]
        public void CacheHookTest()
        {
            String testSecretName = Setup(TestType.SecretString);
            TestHook testHook = new TestHook();
            cache = new SecretsManagerCache(TestBase.Client, new SecretCacheConfiguration { CacheHook = testHook });
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
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
    }
}

