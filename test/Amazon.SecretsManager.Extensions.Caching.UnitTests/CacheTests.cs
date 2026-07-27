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
    using System.Threading.Tasks;
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
        public async Task GetSecretStringTest()
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
        public async Task NoSecretStringPresentTest()
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
        public async Task GetSecretBinaryTest()
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
        public async Task NoSecretBinaryPresentTest()
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
        public async Task GetSecretBinaryMultipleTest()
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
        public async Task BasicSecretCacheTest()
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
        public async Task SecretStringRefreshNowTest()
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
        public async Task BinarySecretRefreshNowTest()
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
        public async Task RefreshNowFailedTest()
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
        public async Task BasicSecretCacheTTLRefreshTest()
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
        public async Task GetSecretStringMultipleTest()
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
        public async Task TestBasicCacheEviction()
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
        public async Task TestBasicErrorCaching()
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
                try
                {
                    await cache.GetSecretString(secretStringResponse1.Name);
                }
                catch (AmazonSecretsManagerException)
                {
                    throw;
                }
                catch (AmazonServiceException)
                {
                }
            }

            return;
        }

        [Fact]
        public async Task ExceptionRetryTest()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), default(CancellationToken)))
                .ThrowsAsync(new AmazonServiceException("Expected exception 1"))
                .ThrowsAsync(new AmazonServiceException("Expected exception 2"))
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);
            int retryCount = 10;

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await cache.GetSecretString("");
                }
                catch (AmazonServiceException exception)
                {
                    Assert.Equal("Expected exception 1", exception.Message);
                }

            }

            // exceptionCount increments to 1 after the first failure, so the backoff
            // at count=1 is ~2s + jitter. Wait long enough for it to expire.
            Thread.Sleep(3000);

            try
            {
                await cache.GetSecretString("");
            }
            catch (AmazonServiceException exception)
            {
                Assert.Equal("Expected exception 2", exception.Message);
            }
        }

        [Fact]
        public async Task RefreshNowAsyncRespectsTokenCancellation()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(secretStringResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), default(CancellationToken)))
                .ReturnsAsync(describeSecretResponse1)
                .ThrowsAsync(new AmazonSecretsManagerException("This should not be called"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);

            // First call to populate the cache
            await cache.GetSecretString(secretStringResponse1.Name);

            // Cancel immediately - RefreshNowAsync should throw OperationCanceledException
            // promptly during the jitter delay rather than blocking the thread
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => cache.RefreshNowAsync(secretStringResponse1.Name, cts.Token));
                stopwatch.Stop();

                // With await Task.Delay and a pre-cancelled token, cancellation should be
                // near-instant (< 100ms). This verifies the delay is non-blocking and
                // respects the CancellationToken.
                Assert.True(stopwatch.ElapsedMilliseconds < 100,
                    $"Expected cancellation within 100ms but took {stopwatch.ElapsedMilliseconds}ms. " +
                    $"This suggests the delay is blocking rather than using async cancellation.");
            }
        }

        [Fact]
        public async Task RefreshNowAsyncAfterExceptionWithExpiredBackoff()
        {
            // Covers the branch: exception != null, but nextRetryTime is in the past
            // (wait is negative), so sleep remains the base jitter value.
            var fastConfig = new SecretCacheConfiguration
            {
                ExceptionRetryDelayBase = TimeSpan.FromMilliseconds(1),
                ExceptionRetryDelayMax = TimeSpan.FromMilliseconds(1),
                ForceRefreshDelayBase = TimeSpan.FromMilliseconds(10),
                ForceRefreshDelayJitter = TimeSpan.FromMilliseconds(5)
            };

            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonServiceException("Expected failure"))
                .ReturnsAsync(describeSecretResponse1);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse1);

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, fastConfig);

            // Trigger an exception to set nextRetryTime
            try { await cache.GetSecretString(secretStringResponse1.Name); }
            catch (AmazonServiceException) { }

            // Wait for backoff to expire (nextRetryTime will be in the past)
            Thread.Sleep(50);

            // RefreshNowAsync enters the exception branch with negative wait.
            // Should not throw and should recover successfully.
            bool success = await cache.RefreshNowAsync(secretStringResponse1.Name);
            Assert.True(success);
        }

        [Fact]
        public async Task RefreshNowAsyncAfterExceptionWithActiveBackoff()
        {
            // Covers the branch: exception != null, nextRetryTime is in the future,
            // and wait > sleep, so sleep is set to wait.
            // Use fast config so backoff exceeds force-refresh jitter quickly.
            var fastConfig = new SecretCacheConfiguration
            {
                ExceptionRetryDelayBase = TimeSpan.FromMilliseconds(50),
                ExceptionRetryDelayMax = TimeSpan.FromSeconds(5),
                ForceRefreshDelayBase = TimeSpan.FromMilliseconds(10),
                ForceRefreshDelayJitter = TimeSpan.FromMilliseconds(5)
            };

            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.Setup(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonServiceException("Persistent failure"));

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, fastConfig);

            // Each call when backoff has expired triggers DescribeSecret, which fails
            // and increments exceptionCount. With base=50ms, backoffs are:
            // count=1 ~65ms, count=2 ~115ms, count=3 ~215ms, count=4 ~415ms
            for (int i = 0; i < 4; i++)
            {
                try { await cache.GetSecretString(secretStringResponse1.Name); }
                catch (AmazonServiceException) { }
                Thread.Sleep(250);
            }

            // One more call to push exceptionCount to 4 with a longer backoff
            try { await cache.GetSecretString(secretStringResponse1.Name); }
            catch (AmazonServiceException) { }

            // Now nextRetryTime is well in the future (>400ms), which exceeds
            // the force-refresh jitter (~15ms). Use cancellation to verify the
            // code path without actually waiting.
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => cache.RefreshNowAsync(secretStringResponse1.Name, cts.Token));
            }
        }

        [Fact]
        public async Task RefreshNowAsyncWithNegativeWaitSucceeds()
        {
            // When nextRetryTime is in the past, wait is negative and does not
            // replace sleep (which is always positive), so Task.Delay is safe.
            var fastConfig = new SecretCacheConfiguration
            {
                ExceptionRetryDelayBase = TimeSpan.FromMilliseconds(1),
                ExceptionRetryDelayMax = TimeSpan.FromMilliseconds(1),
                ForceRefreshDelayBase = TimeSpan.FromMilliseconds(10),
                ForceRefreshDelayJitter = TimeSpan.FromMilliseconds(5)
            };

            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.SetupSequence(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonServiceException("Expected failure"))
                .ReturnsAsync(describeSecretResponse1);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse1);

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, fastConfig);

            // Trigger an exception so nextRetryTime is set
            try { await cache.GetSecretString(secretStringResponse1.Name); }
            catch (AmazonServiceException) { }

            // Wait long enough for nextRetryTime to be in the past (wait becomes negative)
            Thread.Sleep(50);

            // Negative wait is less than sleep, so sleep stays positive — no exception
            bool success = await cache.RefreshNowAsync(secretStringResponse1.Name);
            Assert.True(success);
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
        public async Task HookSecretCacheTest()
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

        // [UNRELIABLE] Concurrency test
        [Fact]
        public async Task ConcurrentGetSecretStringOnlyRefreshesOnce()
        {
            int describeCallCount = 0;
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse1);
            secretsManager.Setup(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    Interlocked.Increment(ref describeCallCount);
                    return describeSecretResponse1;
                });

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);

            // Fire 20 concurrent requests for the same secret
            var tasks = Enumerable.Range(0, 20)
                .Select(_ => cache.GetSecretString(secretStringResponse1.Name))
                .ToArray();

            string[] results = await Task.WhenAll(tasks);

            // All concurrent callers should receive the correct value
            foreach (string result in results)
            {
                Assert.Equal(secretStringResponse1.SecretString, result);
            }

            // Only one DescribeSecret call should have been made despite 20 concurrent requests
            Assert.Equal(1, describeCallCount);
        }

        // [UNRELIABLE] Concurrency test
        [Fact]
        public async Task ConcurrentGetSecretStringDifferentSecretsSucceeds()
        {
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse1);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse3.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse3);
            secretsManager.Setup(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(describeSecretResponse1);
            secretsManager.Setup(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse3.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(describeSecretResponse1);

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object);

            // Interleave requests for two different secrets concurrently
            var tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(cache.GetSecretString(secretStringResponse1.Name));
                tasks.Add(cache.GetSecretString(secretStringResponse3.Name));
            }

            string[] results = await Task.WhenAll(tasks);

            // Verify each result matches its corresponding secret (even indices = secret1, odd = secret3)
            for (int i = 0; i < results.Length; i++)
            {
                string expected = i % 2 == 0 ? secretStringResponse1.SecretString : secretStringResponse3.SecretString;
                Assert.Equal(expected, results[i]);
            }
        }

        // [UNRELIABLE] Concurrency test
        [Fact]
        public async Task ConcurrentRefreshNowDoesNotCorruptState()
        {
            int refreshCount = 0;
            Mock<IAmazonSecretsManager> secretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            secretsManager.Setup(i => i.GetSecretValueAsync(It.Is<GetSecretValueRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(secretStringResponse1);
            secretsManager.Setup(i => i.DescribeSecretAsync(It.Is<DescribeSecretRequest>(j => j.SecretId == secretStringResponse1.Name), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    Interlocked.Increment(ref refreshCount);
                    return describeSecretResponse1;
                });

            // Minimal delay so refreshes complete quickly and contention is maximized
            var fastConfig = new SecretCacheConfiguration
            {
                ForceRefreshDelayBase = TimeSpan.FromMilliseconds(1),
                ForceRefreshDelayJitter = TimeSpan.FromMilliseconds(1)
            };

            SecretsManagerCache cache = new SecretsManagerCache(secretsManager.Object, fastConfig);

            // Populate the cache first
            await cache.GetSecretString(secretStringResponse1.Name);

            // Launch 10 concurrent RefreshNowAsync calls to stress internal state transitions
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => cache.RefreshNowAsync(secretStringResponse1.Name))
                .ToArray();

            bool[] results = await Task.WhenAll(tasks);

            // All refreshes should complete successfully, and the cache should remain consistent
            string value = await cache.GetSecretString(secretStringResponse1.Name);
            Assert.Equal(secretStringResponse1.SecretString, value);
        }
    }
}