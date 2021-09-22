// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MediatR;
using OmniSharp.Extensions.JsonRpc;
using System.Threading;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Handlers
{
    [Method("textDocument/bicepCache", Direction.ClientToServer)]
    public record BicepRegistryCacheParams(string Target) : IRequest<BicepRegistryCacheResponse?>;

    public record BicepRegistryCacheResponse(string Content, BicepRegistryCacheContentType ContentType);

    public enum BicepRegistryCacheContentType
    {
        None,
        Json,
        Template
    };

    public class BicepRegistryCacheHandler : IJsonRpcRequestHandler<BicepRegistryCacheParams, BicepRegistryCacheResponse?>
    {
        private const string Hax =
@"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""metadata"": {
    ""_target"": ""TARGET_PLACEHOLDER"",
    ""_generator"": {
      ""name"": ""bicep"",
      ""version"": ""0.4.640.52532"",
      ""templateHash"": ""742614593825485067""
    }
  },
  ""parameters"": {
    ""namePrefix"": {
      ""type"": ""string""
    },
    ""location"": {
      ""type"": ""string"",
      ""defaultValue"": ""[resourceGroup().location]""
    },
    ""dockerImage"": {
      ""type"": ""string""
    },
    ""dockerImageTag"": {
      ""type"": ""string""
    },
    ""appPlanId"": {
      ""type"": ""string""
    }
  },
  ""functions"": [],
  ""resources"": [
    {
      ""type"": ""Microsoft.Web/sites"",
      ""apiVersion"": ""2020-06-01"",
      ""name"": ""[format('{0}site', parameters('namePrefix'))]"",
      ""location"": ""[parameters('location')]"",
      ""properties"": {
        ""siteConfig"": {
          ""appSettings"": [
            {
              ""name"": ""DOCKER_REGISTRY_SERVER_URL"",
              ""value"": ""https://index.docker.io""
            },
            {
              ""name"": ""DOCKER_REGISTRY_SERVER_USERNAME"",
              ""value"": """"
            },
            {
              ""name"": ""DOCKER_REGISTRY_SERVER_PASSWORD"",
              ""value"": """"
            },
            {
              ""name"": ""WEBSITES_ENABLE_APP_SERVICE_STORAGE"",
              ""value"": ""false""
            }
          ],
          ""linuxFxVersion"": ""[format('DOCKER|{0}:{1}', parameters('dockerImage'), parameters('dockerImageTag'))]""
        },
        ""serverFarmId"": ""[parameters('appPlanId')]""
      }
    }
  ],
  ""outputs"": {
    ""siteUrl"": {
      ""type"": ""string"",
      ""value"": ""[reference(resourceId('Microsoft.Web/sites', format('{0}site', parameters('namePrefix')))).hostNames[0]]""
    }
  }
}
";

        public Task<BicepRegistryCacheResponse?> Handle(BicepRegistryCacheParams request, CancellationToken cancellationToken)
        {
            var content = Hax.Replace("TARGET_PLACEHOLDER", request.Target);
            return Task.FromResult<BicepRegistryCacheResponse?>(new BicepRegistryCacheResponse(content, BicepRegistryCacheContentType.Template));
        }
    }
}
