namespace Amazon.SecretsManager.Extensions.Caching.IntegTests
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
    public class TestBase : IAsyncLifetime
    {
        public static IAmazonSecretsManager Client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.USWest2);
        public static String TestSecretPrefix = "IntegTest";
        public static List<String> SecretNamesToDelete = new List<String>();

        public async Task InitializeAsync()
        {
            await FindPreviousTestSecrets();
            await DeleteSecrets(forceDelete: false);
        }

        public async Task DisposeAsync()
        {
            await DeleteSecrets(forceDelete: true);
        }

        private async Task FindPreviousTestSecrets()
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
            } while (nextToken != null);
        }

        private async Task DeleteSecrets(bool forceDelete)
        {
            foreach (String secretName in SecretNamesToDelete)
            {
                await TestBase.Client.DeleteSecretAsync(new DeleteSecretRequest { SecretId = secretName, ForceDeleteWithoutRecovery = forceDelete });
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
            Assert.Equal(await cache.GetSecretString(testSecretName), testSecretString);
        }

        [Fact]
        public async void SecretCacheTTLTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client, new SecretCacheConfiguration { CacheItemTTL = 1000 });
            String originalSecretString = await cache.GetSecretString(testSecretName);
            await TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            // Even though the secret is updated, the cached version should be retrieved
            Assert.Equal(originalSecretString, await cache.GetSecretString(testSecretName));

            Thread.Sleep(1000);

            // Cached secret string should be expired and the updated secret string retrieved
            Assert.NotEqual(originalSecretString, await cache.GetSecretString(testSecretName));
        }

        [Fact]
        public async void SecretCacheRefreshTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            String originalSecretString = await cache.GetSecretString(testSecretName);
            await TestBase.Client.UpdateSecretAsync(new UpdateSecretRequest { SecretId = testSecretName, SecretString = System.Guid.NewGuid().ToString() });

            Assert.Equal(originalSecretString, await cache.GetSecretString(testSecretName));
            Assert.True(await cache.RefreshNowAsync(testSecretName));
            Assert.NotEqual(originalSecretString, await cache.GetSecretString(testSecretName));
        }

        [Fact]
        public async void NoSecretBinaryTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(await cache.GetSecretBinary(testSecretName));
        }

        [Fact]
        public async void GetSecretBinaryTest()
        {
            String testSecretName = await Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Equal(await cache.GetSecretBinary(testSecretName), testSecretBinary.ToArray());
        }

        [Fact]
        public async void NoSecretStringTest()
        {
            String testSecretName = await Setup(TestType.SecretBinary);
            cache = new SecretsManagerCache(TestBase.Client);
            Assert.Null(await cache.GetSecretString(testSecretName));
        }

        [Fact]
        public async void CacheHookTest()
        {
            String testSecretName = await Setup(TestType.SecretString);
            TestHook testHook = new TestHook();
            cache = new SecretsManagerCache(TestBase.Client, new SecretCacheConfiguration { CacheHook = testHook });
            String originalSecretString = await cache.GetSecretString(testSecretName);
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

