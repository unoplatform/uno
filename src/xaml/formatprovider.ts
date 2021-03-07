import * as vscode from 'vscode';
import { XamlSchemaPropertiesArray } from './types';
import { globalSettings } from './XamlExt';
import XamlSimpleParser from './helpers/Xamlsimpleparser';

export default class XamlFormatProvider implements vscode.DocumentFormattingEditProvider {
    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
    }

    async provideDocumentFormattingEdits (textDocument: vscode.TextDocument, options: vscode.FormattingOptions, token: vscode.CancellationToken): Promise<vscode.TextEdit[]> {
        const indentationString = options.insertSpaces ? Array(options.tabSize).fill(' ').join("") : "\t";

        const documentRange = new vscode.Range(textDocument.positionAt(0), textDocument.lineAt(textDocument.lineCount - 1).range.end);
        const text = textDocument.getText();

        const formattedText: string =
            (await XamlSimpleParser.formatXaml(text, indentationString, textDocument.eol === vscode.EndOfLine.CRLF ? `\r\n` : `\n`, globalSettings.formattingStyle))
                .trim();

        if (formattedText == null) {
            return [];
        }

        if (token.isCancellationRequested) {
            return [];
        }

        return [vscode.TextEdit.replace(documentRange, formattedText)];
    }
}
