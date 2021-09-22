// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
import * as vscode from "vscode";
import { LanguageClient } from "vscode-languageclient/node";
import { bicepCacheRequestType } from "./protocol";

export class BicepCacheContentProvider
  implements vscode.TextDocumentContentProvider
{
  constructor(private readonly languageClient: LanguageClient) {}

  onDidChange?: vscode.Event<vscode.Uri> | undefined;

  async provideTextDocumentContent(
    uri: vscode.Uri,
    token: vscode.CancellationToken
  ): Promise<string> {
    const response = await this.languageClient.sendRequest(
      bicepCacheRequestType,
      {
        target: this.getTarget(uri),
      },
      token
    );

    if (!response) {
      // TODO: Signal failure somehow?
      return "";
    }

    // awaiting on openTextDocument() here causes a deadlock
    // because we are currently opening the same document
    // however we can fire and forget the promise chain
    vscode.workspace.openTextDocument(uri).then(async (document) => {
      const updated = await vscode.languages.setTextDocumentLanguage(
        document,
        this.selectDocumentLanguage()
      );


    });

    return response.content;
  }

  private getTarget(uri: vscode.Uri) {
    // the URIs have the format of bicep-cache:///<uri-encoded bicep module reference>
    // the path of a URI will also have a leading slash that needs to be removed
    return decodeURIComponent(uri.path.substring(1));
  }

  private selectDocumentLanguage() {
    const armToolsExtension = vscode.extensions.getExtension(
      "msazurermtools.azurerm-vscode-tools"
    );

    // if ARM Tools extension is installed and active, use a more specific language ID
    return armToolsExtension && armToolsExtension.isActive
      ? "arm-template"
      : "json";
  }
}
