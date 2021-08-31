namespace Amazon.SecretsManager.Extensions.Caching.Tests
{
    using Xunit;
    using Amazon.SecretsManager.Model;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    // Performs test secret cleanup before and after integ tests are run
    public class TestBase : IDisposable
    {
        public static IAmazonSecretsManager Client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.USWest2);
        public static String TestSecretPrefix = "IntegTest";
        public static List<String> SecretNamesToDelete = new List<String>();

        public TestBase()
        {
            FindPreviousTestSecrets();
            DeleteSecrets(forceDelete: false);
        }

        public void Dispose()
        {
            DeleteSecrets(forceDelete: true);
        }

        private async void FindPreviousTestSecrets()
        {
            String nextToken = null;
            var twoDaysAgo = DateTime.Now.AddDays(-2);
            do
            {
                var response = await TestBase.Client.ListSecretsAsync(new ListSecretsRequest { NextToken = nextToken });
                nextToken = response.NextToken;
                List<SecretListEntry> secretList = response.SecretList;
                foreach (SecretListEntry secret in secretList)
                {
                    if (secret.Name.StartsWith(TestSecretPrefix)
                        && DateTime.Compare(secret.LastChangedDate, twoDaysAgo) < 0
                        && DateTime.Compare(secret.LastAccessedDate, twoDaysAgo) < 0)
                    {
                        SecretNamesToDelete.Add(secret.Name);
                    }
                }
                Thread.Sleep(1000);
            } while (nextToken != null);
        }

        private async void DeleteSecrets(bool forceDelete)
        {
            foreach (String secretName in SecretNamesToDelete)
            {
                await TestBase.Client.DeleteSecretAsync(new DeleteSecretRequest { SecretId = secretName, ForceDeleteWithoutRecovery = forceDelete });
                Thread.Sleep(500);
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

        private async Task<string> Setup(TestType type)
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

            await TestBase.Client.CreateSecretAsync(req);
            TestBase.SecretNamesToDelete.Add(testSecretName);
            return testSecretName;
        }

        [Fact]
        public async void GetSecretStringTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Equal(cache.GetSecretString(testSecretName).Result, testSecretString);
        }

        [Fact]
        public async void SecretCacheTTLTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client, new SecretCacheConfiguration { CacheItemTTL = 1000 });
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            await TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            // Even though the secret is updated, the cached version should be retrieved
            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);

            Thread.Sleep(1000);

            // Cached secret string should be expired and the updated secret string retrieved
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public async void SecretCacheRefreshTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            String originalSecretString = cache.GetSecretString(testSecretName).Result;
            await TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            Assert.Equal(originalSecretString, cache.GetSecretString(testSecretName).Result);
            Assert.True(cache.RefreshNowAsync(testSecretName).Result);
            Assert.NotEqual(originalSecretString, cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public async void NoSecretBinaryTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(cache.GetSecretBinary(testSecretName).Result);
        }

        [Fact]
        public async void GetSecretBinaryTest()
        {
            String testSecretName = await Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Equal(cache.GetSecretBinary(testSecretName).Result, testSecretBinary.ToArray());
        }

        [Fact]
        public async void NoSecretStringTest()
        {
            String testSecretName = await Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(cache.GetSecretString(testSecretName).Result);
        }

        [Fact]
        public async void CacheHookTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
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

