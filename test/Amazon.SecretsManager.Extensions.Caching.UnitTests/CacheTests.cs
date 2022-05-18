/*
 * Copyright 2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"). You may not use this file except in compliance with
 * the License. A copy of the License is located at
 *
 * http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

namespace Amazon.SecretsManager.Extensions.Caching.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Amazon.Runtime;
    using Amazon.SecretsManager.Model;
    using Moq;
    using Xunit;

    public class CacheTests
    {
        private const string AWSCURRENT_VERSIONID_1 = "01234567890123456789012345678901";
        private const string AWSCURRENT_VERSIONID_2 = "12345678901234567890123456789012";

        private readonly GetSecretValueResponse secretStringResponse1 = new GetSecretValueResponse
        {
            Name = "MySecretString",
            VersionId = AWSCURRENT_VERSIONID_1,
            SecretString = "MySecretValue1",
        };

        private readonly GetSecretValueResponse secretStringResponse2 = new GetSecretValueResponse
        {
            Name = "MySecretString",
            VersionId = AWSCURRENT_VERSIONID_2,
            SecretString = "MySecretValue2"
        };

        private readonly GetSecretValueResponse secretStringResponse3 = new GetSecretValueResponse
        {
            Name = "OtherSecretString",
            VersionId = AWSCURRENT_VERSIONID_1,
            SecretString = "MyOtherSecretValue"
        };

        private readonly GetSecretValueResponse secretStringResponse4 = new GetSecretValueResponse
        {
            Name = "AnotherSecretString",
            VersionId = AWSCURRENT_VERSIONID_1,
            SecretString = "AnotherSecretValue"
        };

        private readonly GetSecretValueResponse binaryResponse1 = new GetSecretValueResponse
        {
            Name = "MyBinarySecret",
            VersionId = AWSCURRENT_VERSIONID_1,
            SecretBinary = new MemoryStream(Enumerable.Repeat((byte)0x20, 10).ToArray())
        };

        private readonly GetSecretValueResponse binaryResponse2 = new GetSecretValueResponse
        {
            Name = "MyBinarySecret",
            VersionId = AWSCURRENT_VERSIONID_2,
            SecretBinary = new MemoryStream(Enumerable.Repeat((byte)0x30, 10).ToArray())
        };

        private readonly DescribeSecretResponse describeSecretResponse1 = new DescribeSecretResponse()
        {
            VersionIdsToStages = new Dictionary<string, List<string>> {
                { AWSCURRENT_VERSIONID_1, new List<String> { "AWSCURRENT" } }
            }
        };

        private readonly DescribeSecretResponse describeSecretResponse2 = new DescribeSecretResponse()
        {
            VersionIdsToStages = new Dictionary<string, List<string>> {
                { AWSCURRENT_VERSIONID_2, new List<String> { "AWSCURRENT" } }
            }
        };


        [Fact]
        public void SecretCacheConstructorTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            SecretsManagerCache cache1 = new SecretsManagerCache(secretsManager.Object);
            SecretsManagerCache cache2 = new SecretsManagerCache(secretsManager.Object, new SecretCacheConfiguration());
            Assert.NotNull(cache1);
            Assert.NotNull(cache2);
        }

        [Fact]
        public async void GetSecretStringTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            string first = await cache.GetSecretString(secretStringResponse1.Name);
            Assert.Equal(first, secretStringResponse1.SecretString);
        }

        [Fact]
        public async void NoSecretStringPresentTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(binaryResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            string first = await cache.GetSecretString(secretStringResponse1.Name);
            Assert.Null(first);
        }

        [Fact]
        public async void GetSecretBinaryTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(binaryResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            
            byte[] first = await cache.GetSecretBinary(binaryResponse1.Name);
            Assert.Equal(first, binaryResponse1.SecretBinary.ToArray());
        }

        [Fact]
        public async void NoSecretBinaryPresentTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);

            byte[] first = await cache.GetSecretBinary(binaryResponse1.Name);
            Assert.Null(first);
        }

        [Fact]
        public async void GetSecretBinaryMultipleTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(binaryResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            
            byte[] first = null;
            for (int i = 0; i < 10; i++)
            {
                first = await cache.GetSecretBinary(binaryResponse1.Name);
            }
            Assert.Equal(first, binaryResponse1.SecretBinary.ToArray());
            
        }

        [Fact]
        public async void BasicSecretCacheTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            
            String first = await cache.GetSecretString(secretStringResponse1.Name);
            String second = await cache.GetSecretString(secretStringResponse1.Name);
            Assert.Equal(first, second);
            
        }

        [Fact]
        public async void SecretStringRefreshNowTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ReturnsAsync(secretStringResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            {
                String first = await cache.GetSecretString(secretStringResponse1.Name);
                bool success = await cache.RefreshNowAsync(secretStringResponse1.Name);
                String second = await cache.GetSecretString(secretStringResponse1.Name);
                Assert.True(success);
                Assert.NotEqual(first, second);
            }
        }

        [Fact]
        public async void BinarySecretRefreshNowTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(binaryResponse1)
                .ReturnsAsync(binaryResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == binaryResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            byte[] first = await cache.GetSecretBinary(binaryResponse1.Name);
            bool success = await cache.RefreshNowAsync(binaryResponse1.Name);
            byte[] second = await cache.GetSecretBinary(binaryResponse1.Name);
            Assert.True(success);
            Assert.NotEqual(first, second);
        }

        [Fact]
        public async void RefreshNowFailedTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonServiceException("Caught exception"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            String first = await cache.GetSecretString(secretStringResponse1.Name);
            bool success = await cache.RefreshNowAsync(secretStringResponse1.Name);
            String second = await cache.GetSecretString(secretStringResponse2.Name);
            Assert.False(success);
            Assert.Equal(first, second);
        }

        [Fact]
        public async void BasicSecretCacheTTLRefreshTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ReturnsAsync(secretStringResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, new SecretCacheConfiguration { CacheItemTTL = 1000 });
            
            String first = await cache.GetSecretString(secretStringResponse1.Name);
            String second = await cache.GetSecretString(secretStringResponse1.Name);
            Assert.Equal(first, second);

            Thread.Sleep(5000);
            String third = await cache.GetSecretString(secretStringResponse2.Name);
            Assert.NotEqual(second, third);
        }

        [Fact]
        public async void GetSecretStringMultipleTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            String first = null;
            for (int i = 0; i < 10; i++)
            {
                first = await cache.GetSecretString(secretStringResponse1.Name);
            }
            Assert.Equal(first, secretStringResponse1.SecretString);
        }

        [Fact]
        public async void TestBasicCacheEviction()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ReturnsAsync(secretStringResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse3.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse3)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse2)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, new SecretCacheConfiguration { MaxCacheSize = 1 });
            String first = await cache.GetSecretString(secretStringResponse1.Name);
            String second = await cache.GetSecretString(secretStringResponse3.Name);
            String third = await cache.GetSecretString(secretStringResponse2.Name);
            Assert.NotEqual(first, third);
        }

        [Fact]
        public void TestBasicErrorCaching()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ThrowsAsync(new AmazonServiceException("Expected exception"))
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            for (int i = 0; i < 5; i++)
            {
                Assert.ThrowsAsync<AmazonServiceException>(async () => await cache.GetSecretString(secretStringResponse1.Name));
            }
        }

        [Fact]
        public async void ExceptionRetryTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), default(CancellationToken)))
                .ThrowsAsync(new AmazonServiceException("Expected exception 1"))
                .ThrowsAsync(new AmazonServiceException("Expected exception 2"))
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            int retryCount = 10;
            String result = null;
            Exception ex = null;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    result = cache.GetSecretString("").Result;
                }
                catch (AggregateException exception) { ex = exception.InnerException; }
                Assert.Equal("Expected exception 1", ex.Message);
            }

            // Wait for backoff interval before retrying to verify a retry is performed.
            Thread.Sleep(2100);

            try
            {
                await cache.GetSecretString("");
            }
            catch (AmazonServiceException exception)
            {
                Assert.Equal("Expected exception 2", exception.Message);
            }
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
        public async void HookSecretCacheTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ReturnsAsync(binaryResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            
            TestHook testHook = new TestHook();
            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, new SecretCacheConfiguration { CacheHook = testHook });

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(await cache.GetSecretString(secretStringResponse1.Name), secretStringResponse1.SecretString);
            }
            Assert.Equal(2, testHook.GetCount());

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(await cache.GetSecretBinary(binaryResponse1.Name), binaryResponse1.SecretBinary.ToArray());
            }
            Assert.Equal(4, testHook.GetCount());
        }
    }
}