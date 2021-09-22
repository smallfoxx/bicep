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
        target: uri.toString(),
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
    vscode.workspace.openTextDocument(uri).then((document) => {
      vscode.languages.setTextDocumentLanguage(
        document,
        this.selectDocumentLanguage()
      );
    });

    return response.content;
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
