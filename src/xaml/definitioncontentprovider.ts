import * as vscode from 'vscode';
import { XamlSchemaPropertiesArray } from './types';
import XsdCachedLoader from './helpers/xsdcachedloader';
import { schemaId } from './XamlExt';

export default class XamlDefinitionContentProvider implements vscode.TextDocumentContentProvider {
    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
    }

    async provideTextDocumentContent (uri: vscode.Uri): Promise<string> {
        const trueUri = Buffer.from(uri.toString(true).replace(`${schemaId}://`, ''), 'hex').toString();
        return await XsdCachedLoader.loadSchemaContentsFromUri(trueUri);
    }
}
