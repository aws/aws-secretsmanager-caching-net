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

namespace Amazon.SecretsManager.Extensions.Caching
{
    using Amazon.Runtime;
    using Amazon.SecretsManager.Model;
    using Amazon.Util;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class SecretCacheObject<T>
    {
        private readonly JitteredDelay exceptionJitteredDelay;
        private readonly JitteredDelay forceRefreshJitteredDelay;

        /// The secret identifier for this cached object. 
        protected String secretId;

        /// A private object to synchronize access to certain methods. 
        protected static readonly SemaphoreSlim Lock = new SemaphoreSlim(1,1);

        /// The AWS Secrets Manager client to use for requesting secrets. 
        protected IAmazonSecretsManager client;

        /// The Secret Cache Configuration.
        protected SecretCacheConfiguration config;

        /// A flag to indicate a refresh is needed. 
        private bool refreshNeeded = true;

        /// The result of the last AWS Secrets Manager request for this item.
        private Object data = null;
        
        /// If the last request to AWS Secrets Manager resulted in an exception,
        /// that exception will be thrown back to the caller when requesting
        /// secret data.
        protected Exception exception = null;

       
        /// The number of exceptions encountered since the last successfully
        /// AWS Secrets Manager request.  This is used to calculate an exponential
        /// backoff.
        private long exceptionCount = 0;
        
        /// The time to wait before retrying a failed AWS Secrets Manager request.
        private DateTime nextRetryTime = DateTime.MinValue;

        public static readonly ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random(Environment.TickCount));



        /// <summary>
        /// Construct a new cached item for the secret.
        /// </summary>
        /// <param name="secretId"> The secret identifier. This identifier could be the full ARN or the friendly name for the secret. </param>
        /// <param name="client"> The AWS Secrets Manager client to use for requesting the secret. </param>
        /// <param name="config"> The secret cache configuration. </param>
        public SecretCacheObject(String secretId, IAmazonSecretsManager client, SecretCacheConfiguration config)
        {
            this.secretId = secretId;
            this.client = client;
            this.config = config;
            this.exceptionJitteredDelay = new JitteredDelay(
                config.ExceptionRetryDelayBase,
                config.ExceptionRetryDelayBase,
                config.ExceptionRetryDelayMax);
            this.forceRefreshJitteredDelay = new JitteredDelay(
                config.ForceRefreshDelayBase,
                config.ForceRefreshDelayJitter);
        }
     
        protected abstract Task<T> ExecuteRefreshAsync(CancellationToken cancellationToken = default);

        protected abstract Task<GetSecretValueResponse> GetSecretValueAsync(T result, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return the typed result object.
        /// </summary>
        private T GetResult()
        {
            if (null != config.CacheHook)
            {
                return (T)config.CacheHook.Get(data);
            }
            return (T)data;
        }

        /// <summary>
        /// Store the result data.
        /// </summary>
        private void SetResult(T result)
        {
            if (null != config.CacheHook)
            {
                data = config.CacheHook.Put(result);
            }
            else
            {
                data = result;
            }
        }

        /// <summary>
        /// Determine if the secret object should be refreshed.
        /// </summary>
        protected bool IsRefreshNeeded()
        {
            if (refreshNeeded) { return true; }
            if (null == exception) { return false; }

            // If we encountered an exception on the last attempt
            // we do not want to keep retrying without a pause between
            // the refresh attempts.
            //
            // If we have exceeded our backoff time we will refresh
            // the secret now.
            return DateTime.UtcNow >= nextRetryTime;
        }

        /// <summary>
        /// Refresh the cached secret state only when needed.
        /// </summary>
        private async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRefreshNeeded()) { return false; }
            refreshNeeded = false;
            try
            {
                SetResult(await ExecuteRefreshAsync(cancellationToken));
                exception = null;
                exceptionCount = 0;
                return true;
            }
            catch (Exception ex) when (ex is AmazonServiceException || ex is AmazonClientException)
            {
                exception = ex;
                exceptionCount++;
                // Determine the amount of growth in exception backoff time based on the growth
                // factor and default backoff duration.

                nextRetryTime = DateTime.UtcNow + exceptionJitteredDelay.GetRetryDelay((int)exceptionCount);
            }
            return false;
        }

        /// <summary>
        /// Method to force the refresh of a cached secret state.
        /// Returns true if the refresh completed without error.
        /// </summary>
        /// <exception cref="System.OperationCanceledException">Thrown when the <paramref name="cancellationToken"/> is cancelled during the backoff delay.</exception>
        public async Task<bool> RefreshNowAsync(CancellationToken cancellationToken = default)
        {
            refreshNeeded = true;
            // When forcing a refresh, always sleep with a random jitter
            // to prevent coding errors that could be calling refreshNow
            // in a loop.
            TimeSpan sleep = forceRefreshJitteredDelay.GetRetryDelay(1);

            // Make sure we are not waiting for the next refresh after an
            // exception.  If we are, sleep based on the retry delay of
            // the refresh to prevent a hard loop in attempting to refresh a
            // secret that continues to throw an exception such as AccessDenied.
            if (null != exception)
            {
                TimeSpan wait = nextRetryTime - DateTime.UtcNow;
                if (wait > sleep)
                {
                    sleep = wait;
                }
            }
            await Task.Delay(sleep, cancellationToken);

            // Perform the requested refresh.
            bool success = false;
            await Lock.WaitAsync(cancellationToken);
            try
            {
                success = await RefreshAsync(cancellationToken);
            }
            finally
            {
                Lock.Release();
            }
            return (null == exception && success);
        }

        /// <summary>
        /// Asynchronously return the cached result from AWS Secrets Manager for GetSecretValue.
        /// If the secret is due for a refresh, the refresh will occur before the result is returned.
        /// If the refresh fails, the cached result is returned, or the cached exception is thrown.
        /// </summary>
        public async Task<GetSecretValueResponse> GetSecretValue(CancellationToken cancellationToken)
        {
            bool success = false;
            await Lock.WaitAsync(cancellationToken);
            try
            {
                success = await RefreshAsync(cancellationToken);
            }
            finally
            {
                Lock.Release();
            }

            if (!success && null == data && null != exception)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }
            return await GetSecretValueAsync(GetResult(), cancellationToken);
        }
    }
}
