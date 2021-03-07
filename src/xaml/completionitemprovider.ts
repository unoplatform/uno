/* eslint-disable no-return-assign */
import * as vscode from 'vscode';
import { XamlSchemaPropertiesArray, CompletionString } from './types';
import { languageId } from './XamlExt';
import XamlSimpleParser from './helpers/Xamlsimpleparser';
import unoPlatform from './uno5.json';

export default class XamlCompletionItemProvider implements vscode.CompletionItemProvider {
    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
    }

    async provideCompletionItems (textDocument: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, _context: vscode.CompletionContext): Promise<vscode.CompletionItem[] | vscode.CompletionList> {
        const documentContent = textDocument.getText();
        const offset = textDocument.offsetAt(position);
        const scope = await XamlSimpleParser.getScopeForPosition(documentContent, offset);
        let resultTexts: CompletionString[] = [];
        const controls: any[] = (unoPlatform as any[]);

        // only for xaml
        if (textDocument.languageId === languageId && textDocument.fileName.includes(".xaml")) {
            if (token.isCancellationRequested) {
                resultTexts = [];
            } else if (scope.context === "text") {
                resultTexts = [];
            } else if (scope.tagName === undefined) {
                resultTexts = [];
            } else if (scope.context === "element" && !scope.tagName.includes(".")) {
                resultTexts = [];

                for (var i = 0; i < controls.length; i++) {
                    resultTexts.push(new CompletionString(controls[i].name));
                }

                resultTexts.map(t => t.type = vscode.CompletionItemKind.Class);
            } else if (scope.context === "attribute") {
                resultTexts = [];
                const findTag = controls.find(t => t.name === scope.tagName);

                if (findTag !== undefined) {
                    for (let i = 0; i < findTag.props.length; i++) {
                        const attr = new CompletionString(findTag.props[i].name);

                        if (typeof (findTag.props[i].type) === 'string' &&
                            (findTag.props[i].type as string).includes("Event")
                        ) {
                            attr.type = vscode.CompletionItemKind.Event;
                        } else {
                            attr.type = vscode.CompletionItemKind.Property;
                        }

                        resultTexts.push(attr);
                    }
                }

                // value?
            } else if (scope.context !== undefined) {
                resultTexts = [];

                const findTag = controls.find(t => t.name === scope.tagName);
                if (findTag !== undefined) {
                    const findProp = findTag.props
                        .find(t => t.name === scope.tagAttr);

                    if (findProp !== undefined) {
                        if (Array.isArray(findProp.type)) {
                            for (let i = 0; i < findProp.type.length; i++) {
                                var enom = findProp.type[i];
                                const enomTyped = new CompletionString(enom);
                                enomTyped.type = vscode.CompletionItemKind.Enum;
                                resultTexts.push(enomTyped);
                            }
                        } else {
                            const normalTyped = new CompletionString(JSON.stringify(findProp));
                            normalTyped.type = vscode.CompletionItemKind.Text;
                            resultTexts.push(normalTyped);
                        }
                    }
                }
            } else {
                resultTexts = [];
            }
        }

        resultTexts = resultTexts.filter((v, i, a) => a.findIndex(e => e.name === v.name && e.comment === v.comment) === i);

        return resultTexts
            .map(t => {
                const ci = new vscode.CompletionItem(t.name, t.type);
                ci.detail = scope.context;
                ci.documentation = t.comment;

                if (t.type === vscode.CompletionItemKind.Property ||
                    t.type === vscode.CompletionItemKind.Event
                ) {
                    ci.insertText = `${t.name}=`;
                }

                if (t.type === vscode.CompletionItemKind.Text) {
                    ci.label = ".";
                    ci.detail = undefined;

                    const bj = JSON.parse(t.name);

                    // mount markdown
                    let docStr = "";
                    const parts = (bj.type as string).split("[");
                    const title = parts[0];
                    let body = "";

                    if (parts.length > 1) {
                        const parts2 = parts[1].split(",");

                        for (let i = 0; i < parts2.length; i++) {
                            if (i === parts2.length - 1) {
                                parts2[i] = parts2[i].replace("]", "");
                            }

                            body += `- ${parts2[i]}\n`;
                        }
                    }

                    let link = "";
                    if (bj.namespace.indexOf("Microsoft") !== -1) {
                        link = `https://docs.microsoft.com/en-us/windows/winui/api/${(bj.namespace as string)}`;
                    } else {
                        link = `https://docs.microsoft.com/en-us/uwp/api/${(bj.namespace as string)}`;
                    }

                    if (bj.type.indexOf("Event") !== -1) {
                        link += "#events";
                    }

                    docStr =
                        `[${(bj.namespace as string)}${bj.type.indexOf("Event") !== -1 ? `.${(bj.name as string)}` : ""}](${link})
### ${title}
${body}`;

                    ci.documentation = new vscode.MarkdownString(docStr);
                    ci.documentation.isTrusted = true;
                }

                return ci;
            });
    }
}
