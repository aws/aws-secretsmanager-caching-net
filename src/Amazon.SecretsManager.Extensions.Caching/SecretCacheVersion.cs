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
