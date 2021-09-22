// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Handlers
{
    [Method(BicepRegistryCacheHandler.LspMethod, Direction.ClientToServer)]
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
        public const string LspMethod = "textDocument/bicepCache";

        private readonly IModuleDispatcher moduleDispatcher;

        private readonly IFileResolver fileResolver;

        public BicepRegistryCacheHandler(IModuleDispatcher moduleDispatcher, IFileResolver fileResolver)
        {
            this.moduleDispatcher = moduleDispatcher;
            this.fileResolver = fileResolver;
        }

        public Task<BicepRegistryCacheResponse?> Handle(BicepRegistryCacheParams request, CancellationToken cancellationToken)
        {
            var moduleReference = this.moduleDispatcher.TryGetModuleReference(request.Target, out _) ?? throw new InvalidOperationException("The client failed to specify a target module reference.");
            if(!moduleReference.IsExternal)
            {
                throw new InvalidOperationException($"The specified module reference '{request.Target}' refers to a local module which is not supported by {LspMethod} requests.");
            }

            if(this.moduleDispatcher.GetModuleRestoreStatus(moduleReference, out _) != ModuleRestoreStatus.Succeeded)
            {
                throw new InvalidOperationException($"The module '{moduleReference.FullyQualifiedReference}' has not yet been successfully restored.");
            }

            var uri = this.moduleDispatcher.TryGetLocalModuleEntryPointUri(new Uri("dummy://dummy"), moduleReference, out _) ?? throw new InvalidOperationException($"Unable to obtain the entry point URI for module '{moduleReference.FullyQualifiedReference}'.");
            if(!this.fileResolver.TryRead(uri, out var contents, out _))
            {
                throw new InvalidOperationException($"Unable to read file '{uri}'.");
            }

            return Task.FromResult<BicepRegistryCacheResponse?>(new BicepRegistryCacheResponse(contents, BicepRegistryCacheContentType.Template));
        }
    }
}
