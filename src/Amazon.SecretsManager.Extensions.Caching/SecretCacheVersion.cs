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
    using Amazon.SecretsManager.Model;
    using System;
    using System.Threading.Tasks;

    class SecretCacheVersion : SecretCacheObject<GetSecretValueResponse>
    {
        private readonly String versionId;
        private readonly int hash;

        public SecretCacheVersion(String secretId, String versionId, IAmazonSecretsManager client, SecretCacheConfiguration config)
            : base(secretId, client, config)
        {
            this.versionId = versionId;
            this.hash = String.Format("%s %s", secretId, versionId).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is SecretCacheVersion scv
                && this.secretId == scv.secretId
                && this.versionId == scv.versionId;
        }

        public override int GetHashCode()
        {
            return this.hash;
        }

        public override string ToString()
        {
            return String.Format("SecretCacheVersion: %s %v", secretId, versionId);
        }

        /// <summary>
        /// Asynchronously retrieves the most current GetSecretValueResponse from Secrets Manager
        /// as part of the Refresh operation.
        /// </summary>
        protected override async Task<GetSecretValueResponse> ExecuteRefreshAsync()
        {
            return await this.client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = this.secretId, VersionId = this.versionId });
        }

        protected override Task<GetSecretValueResponse> GetSecretValueAsync(GetSecretValueResponse result)
        {
            return Task.FromResult(result);
        }
    }
}
