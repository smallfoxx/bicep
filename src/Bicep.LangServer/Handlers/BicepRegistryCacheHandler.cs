// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Registry;
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
        private readonly IModuleDispatcher moduleDispatcher;

        public BicepRegistryCacheHandler(IModuleDispatcher moduleDispatcher)
        {
            this.moduleDispatcher = moduleDispatcher;
        }

        public Task<BicepRegistryCacheResponse?> Handle(BicepRegistryCacheParams request, CancellationToken cancellationToken)
        {
            var content = request.Target;
            return Task.FromResult<BicepRegistryCacheResponse?>(new BicepRegistryCacheResponse(content, BicepRegistryCacheContentType.Template));
        }
    }
}
