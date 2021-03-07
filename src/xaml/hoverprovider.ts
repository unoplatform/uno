/* eslint-disable @typescript-eslint/require-array-sort-compare */
/* eslint-disable no-useless-escape */
import * as vscode from 'vscode';
import { XamlSchemaPropertiesArray, CompletionString } from './types';
import { languageId } from './XamlExt';
import XamlSimpleParser from './helpers/Xamlsimpleparser';
import unoPlatform from './uno5.json';

export default class XamlHoverProvider implements vscode.HoverProvider {
    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
    }

    async provideHover (textDocument: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): Promise<vscode.Hover> {
        const documentContent = textDocument.getText();
        const offset = textDocument.offsetAt(position);
        const scope = await XamlSimpleParser.getScopeForPosition(documentContent, offset);
        const wordRange = textDocument.getWordRangeAtPosition(position, /(-?\d*\.\d\w*)|([^\`\~\!\@\#\$\%\^\&\*\(\)\=\+\[\{\]\}\\\|\;\:\'\"\,\<\>\/\?\s]+)/g);
        const word = textDocument.getText(wordRange);
        const controls: any[] = (unoPlatform as any[]);

        let resultTexts: CompletionString[] = [];

        // only for xaml
        if (textDocument.languageId === languageId && textDocument.fileName.includes(".xaml")) {
            if (token.isCancellationRequested) {
                resultTexts = [];
            } else if (scope.context === "text") {
                resultTexts = [];
            } else if (scope.tagName === undefined) {
                resultTexts = [];
            } else if (scope.context === "element") {
                resultTexts = [];
                const filtered = controls.filter(e => e.name === word);

                if (filtered.length > 0) {
                    const doclink = new CompletionString("");
                    let link = "";
                    const namespace = (filtered[0].namespace as string);

                    if (namespace.includes("Microsoft")) {
                        link = `https://docs.microsoft.com/en-us/windows/winui/api/${namespace}`;
                    } else {
                        link = `https://docs.microsoft.com/en-us/uwp/api/${namespace}`;
                    }
                    doclink.comment = `$(link-external)  [${namespace}](${link})`;
                    resultTexts.push(doclink);
                }
            } else if (scope.context === "attribute") {
                /* resultTexts = this.schemaPropertiesArray
                    .filterUris(xsdFileUris)
                    .flatMap(sp => sp.tagCollection.loadAttributesEx(scope.tagName, nsMap).map(s => sp.tagCollection.fixNs(s, nsMap)))
                    .filter(e => e.name === word); */

                resultTexts = [];
                const filtered = controls.filter(e => e.name === scope.tagName);

                if (filtered.length > 0) {
                    const attrFiltered = filtered[0].props.filter(e => e.name === word);

                    if (attrFiltered.length > 0) {
                        const doclink = new CompletionString("");
                        let link = "";
                        const namespace = (attrFiltered[0].namespace as string);
                        const isEvent = attrFiltered[0].type.includes("Event");

                        if (namespace.includes("Microsoft")) {
                            link = `https://docs.microsoft.com/en-us/windows/winui/api/${namespace}`;
                        } else {
                            link = `https://docs.microsoft.com/en-us/uwp/api/${namespace}`;
                        }

                        if (isEvent === true) {
                            link += "#events";
                        }

                        doclink.comment = `$(link-external)  [${namespace}${(attrFiltered[0].type as string).includes("Event") ? `.${(attrFiltered[0].name as string)}` : ""}](${link})`;
                        resultTexts.push(doclink);
                    }
                }
            }
        }

        resultTexts = resultTexts
            .filter((v, i, a) => a.findIndex(e => e.name === v.name && e.comment === v.comment) === i)
            .sort();

        return {
            contents: resultTexts.map(t => new vscode.MarkdownString(t.comment, true)),
            range: wordRange
        };
    }
}
