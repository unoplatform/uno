import * as vscode from 'vscode';
import { XamlSchemaPropertiesArray } from './types';
import { globalSettings } from './XamlExt';
import XamlSimpleParser from './helpers/Xamlsimpleparser';

export default class XamlRangeFormatProvider implements vscode.DocumentRangeFormattingEditProvider {
    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
    }

    async provideDocumentRangeFormattingEdits (textDocument: vscode.TextDocument, range: vscode.Range, options: vscode.FormattingOptions, token: vscode.CancellationToken): Promise<vscode.TextEdit[]> {
        const indentationString = options.insertSpaces ? Array(options.tabSize).fill(' ').join("") : "\t";

        const before = textDocument.getText(new vscode.Range(textDocument.positionAt(0), range.start)).trim();

        const selection = textDocument.getText(new vscode.Range(range.start, range.end)).trim();

        const after = textDocument.getText(new vscode.Range(range.end, textDocument.lineAt(textDocument.lineCount - 1).range.end)).trim();

        const selectionSeparator = "<!--352cf605-57c7-48a8-a5eb-2da215536443-->";
        const text = [before, selection, after].join(selectionSeparator);

        if (!await XamlSimpleParser.checkXaml(text)) {
            return [];
        }

        const emptyLines = /^\s*[\r?\n]|\s*[\r?\n]$/g;

        const formattedText: string =
            (await XamlSimpleParser.formatXaml(text, indentationString, textDocument.eol === vscode.EndOfLine.CRLF ? `\r\n` : `\n`, globalSettings.formattingStyle))
                .split(selectionSeparator)[1]
                .replace(emptyLines, "");

        if (formattedText == null) {
            return [];
        }

        if (token.isCancellationRequested) {
            return [];
        }

        return [vscode.TextEdit.replace(range, formattedText)];
    }
}
