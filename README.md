# AWS Secrets Manager Caching Client for C#

The AWS Secrets Manager caching client enables in-process caching of secrets for .NET applications.

## Required Prerequisites

To use this client, you must have a .NET project with one of the following:

* .NET Framework 4.6.1 or higher
* .NET Standard 2.0 or higher

## Optional Prerequisites

You must have an Amazon Web Services (AWS) account to access secrets stored in AWS Secrets Manager and use AWS SDK for Java.

* **To create an AWS account**, go to [Sign In or Create an AWS Account](https://portal.aws.amazon.com/gp/aws/developer/registration/index.html) and then choose **I am a new user.** Follow the instructions to create an AWS account.

* **To create a secret in AWS Secrets Manager**, go to [Creating Secrets](https://docs.aws.amazon.com/secretsmanager/latest/userguide/manage_create-basic-secret.html) and follow the instructions on that page.

* **To download and install the AWS SDK for Java**, go to [Installing the AWS SDK for .NET](https://aws.amazon.com/sdk-for-net/) in the AWS SDK for .NET documentation and then follow the instructions on that page.

## Download

You can get the latest release from `Nuget`:

```xml
<ItemGroup>
     <PackageReference Include="AWSSDK.SecretsManager.Caching" Version="1.0.0.0" />
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
* You can configure max cache size, cache item TTL, secret version stage, and cache hook with the `SecretCacheConfiguration` class.
* Learn more about [AWS Lambda Function Handlers in C#](https://docs.aws.amazon.com/lambda/latest/dg/dotnet-programming-model-handler-types.html).

## License

This library is licensed under the Apache 2.0 License.