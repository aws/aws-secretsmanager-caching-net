# AWS Secrets Manager Caching Client for .NET



[![NuGet](https://img.shields.io/nuget/v/AWSSDK.SecretsManager.Caching)](https://www.nuget.org/packages/AWSSDK.SecretsManager.Caching)
[![.NET](https://github.com/aws/aws-secretsmanager-caching-net/actions/workflows/dotnet.yml/badge.svg?event=push)](https://github.com/aws/aws-secretsmanager-caching-net/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/aws/aws-secretsmanager-caching-net/branch/master/graph/badge.svg?token=cugbEh31cw)](https://codecov.io/gh/aws/aws-secretsmanager-caching-net)

The AWS Secrets Manager caching client enables in-process caching of secrets for .NET applications.

## Required Prerequisites

To use this client, you must have:

* A .NET project with one of the following:
    * .NET Framework 4.6.2 or higher
    * .NET Standard 2.0 or higher

* An Amazon Web Services (AWS) account to access secrets stored in AWS Secrets Manager and use AWS SDK for .NET.

    * **To create an AWS account**, go to [Sign In or Create an AWS Account](https://portal.aws.amazon.com/gp/aws/developer/registration/index.html) and then choose **I am a new user.** Follow the instructions to create an AWS account.

    * **To create a secret in AWS Secrets Manager**, go to [Creating Secrets](https://docs.aws.amazon.com/secretsmanager/latest/userguide/manage_create-basic-secret.html) and follow the instructions on that page.

    * **To download and install the AWS SDK for .NET**, go to [Installing the AWS SDK for .NET](https://aws.amazon.com/sdk-for-net/) in the AWS SDK for .NET documentation and then follow the instructions on that page.

## Download

You can get the latest release from `Nuget`:

```xml
<ItemGroup>
     <PackageReference Include="AWSSDK.SecretsManager.Caching" Version="2.*" />
</ItemGroup>
```

## Getting Started

The following code sample demonstrates how to start using the caching client:

```cs
using System;
using Amazon.SecretsManager.Extensions.Caching.SecretsManagerCache;

namespace LambdaExample {
    public class CachingExample 
    {
        private SecretsManagerCache cache = new SecretsManagerCache();
        private const String MySecretName = "MySecret";

        public async Task<Response> FunctionHandlerAsync(String input, ILambdaContext context)
        {
            String MySecret = await cache.GetSecretString(MySecretName);
            ...
        }
    }
}
```

* After instantiating the cache, retrieve your secret using `GetSecretString` or `GetSecretBinary`. 
* On successive retrievals, the cache will return the cached copy of the secret. 
* Learn more about [AWS Lambda Function Handlers in C#](https://docs.aws.amazon.com/lambda/latest/dg/dotnet-programming-model-handler-types.html).

### Cache Configuration

You can configure the `SecretCacheConfiguration` object with the following parameters:
* `CacheItemTTL` - The TTL of a Cache item in milliseconds. The default value is `3600000` ms, or 1 hour.
* `MaxCacheSize` - The maximum number of items the Cache can contain before evicting using LRU. The default value is `1024`.
* `VersionStage` - The Version Stage the Cache will request when retrieving secrets from Secrets Manager. The default value is `AWSCURRENT`.
* `Client` - The Secrets Manager client to be used by the Cache. The default value is `null`, which causes the Cache to instantiate a new Secrets Manager client.
* `CacheHook` - An implementation of the ISecretCacheHook interface. The default value is `null`.

### Request Timeouts

If you don't pass in a client, the cache creates one whose requests time out after `SecretsManagerCache.DefaultRequestTimeout` (2 seconds). The SDK otherwise uses the HttpClient default of 100 seconds. On .NET Core and later, a timed out request is retried like any other network error. If you pass in your own client, the cache uses its timeout unchanged, so set it yourself:

```cs
var client = new AmazonSecretsManagerClient(new AmazonSecretsManagerConfig { Timeout = TimeSpan.FromSeconds(2) });
var cache = new SecretsManagerCache(client);
```

You can also pass a `CancellationToken` to bound a single call.

On .NET Core and later, this timeout covers sending the request and waiting for the response headers, but not reading the response body. On .NET Framework, it covers waiting for the response headers but not connecting, the TLS handshake, or reading the response body, and a timed out request is not retried.

A timed out request is handled like an error from Secrets Manager: the call throws it, and until the retry delay passes, later calls for that secret throw it again. If the secret is already cached, its cached value is returned instead. `RefreshNowAsync` returns `false` after a timeout. The exception is a `TimeoutException` on .NET Core and later, and an `OperationCanceledException` on .NET Framework, even though you didn't cancel the call.

## Post-Quantum TLS Support

This library automatically supports Post-Quantum TLS when available on the underlying platform:

- **macOS**: Requires .NET 10+ and `export DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK=1`
- **Linux**: Works with OpenSSL 3.5+ 
- **Windows**: Not yet supported 

## Getting Help
We use GitHub issues for tracking bugs and caching library feature requests and have limited bandwidth to address them. Please use these community resources for getting help:
* Ask a question on [Stack Overflow](https://stackoverflow.com/) and tag it with [aws-secrets-manager](https://stackoverflow.com/questions/tagged/aws-secrets-manager).
* Open a support ticket with [AWS Support](https://console.aws.amazon.com/support/home#/).
* If it turns out that you may have found a bug, please [open an issue](https://github.com/aws/aws-secretsmanager-caching-csharp/issues/new). 

## License

This library is licensed under the Apache 2.0 License.
