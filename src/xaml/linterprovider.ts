import * as vscode from 'vscode';
import { languageId, globalSettings } from './XamlExt';
import { XamlSchemaProperties, XamlTagCollection, XamlSchemaPropertiesArray, XamlDiagnosticData } from './types';
import XsdParser from './helpers/xsdparser';
import XsdCachedLoader from './helpers/xsdcachedloader';
import XamlSimpleParser from './helpers/Xamlsimpleparser';

export default class XamlLinterProvider implements vscode.Disposable {
    private readonly documentListener: vscode.Disposable;
    private readonly diagnosticCollection: vscode.DiagnosticCollection;
    private delayCount: number = Number.MIN_SAFE_INTEGER;
    private textDocument: vscode.TextDocument;
    private linterActive = false;

    constructor (protected extensionContext: vscode.ExtensionContext, protected schemaPropertiesArray: XamlSchemaPropertiesArray) {
        this.schemaPropertiesArray = schemaPropertiesArray;
        this.diagnosticCollection = vscode.languages.createDiagnosticCollection();

        this.documentListener = vscode.workspace.onDidChangeTextDocument(async evnt =>
            await this.triggerDelayedLint(evnt.document), this, this.extensionContext.subscriptions);

        vscode.workspace.onDidOpenTextDocument(async doc =>
            await this.triggerDelayedLint(doc, 100), this, extensionContext.subscriptions);

        vscode.workspace.onDidCloseTextDocument(doc =>
            this.cleanupDocument(doc), null, extensionContext.subscriptions);

        // eslint-disable-next-line @typescript-eslint/no-misused-promises
        vscode.workspace.textDocuments.forEach(async doc => await this.triggerDelayedLint(doc, 100), this);
    }

    public dispose (): void {
        this.documentListener.dispose();
        this.diagnosticCollection.clear();
    }

    private cleanupDocument (textDocument: vscode.TextDocument): void {
        this.diagnosticCollection.delete(textDocument.uri);
    }

    private async triggerDelayedLint (textDocument: vscode.TextDocument, timeout = 2000): Promise<void> {
        if (this.delayCount > Number.MIN_SAFE_INTEGER) {
            this.delayCount = timeout;
            this.textDocument = textDocument;
            return;
        }
        this.delayCount = timeout;
        this.textDocument = textDocument;

        const tick = 100;

        while (this.delayCount > 0 || this.linterActive) {
            await new Promise(resolve => setTimeout(resolve, tick));
            this.delayCount -= tick;
        }

        try {
            this.linterActive = true;
            await this.triggerLint(this.textDocument);
        } finally {
            this.delayCount = Number.MIN_SAFE_INTEGER;
            this.linterActive = false;
        }
    }

    private async triggerLint (textDocument: vscode.TextDocument): Promise<void> {
        if (textDocument.languageId !== languageId) {
            return;
        }

        const t0 = new Date().getTime();
        const diagnostics: vscode.Diagnostic[][] = new Array<vscode.Diagnostic[]>();
        try {
            const documentContent = textDocument.getText();

            const xsdFileUris = (await XamlSimpleParser.getSchemaXsdUris(documentContent, textDocument.uri.toString(true), globalSettings.schemaMapping))
                .map(u => vscode.Uri.parse(u))
                .filter((v, i, a) => a.findIndex(u => u.toString() === v.toString()) === i)
                .map(u => ({ uri: u, parentUri: u }));

            const nsMap = await XamlSimpleParser.getNamespaceMapping(documentContent);

            const text = textDocument.getText();

            if (xsdFileUris.length === 0) {
                const plainXamlCheckResults = await XamlSimpleParser.getXamlDiagnosticData(text, new XamlTagCollection(), nsMap, false);
                diagnostics.push(this.getDiagnosticArray(plainXamlCheckResults));
            }

            while (xsdFileUris.length > 0) {
                const currentUriPair = xsdFileUris.shift() ?? { uri: vscode.Uri.parse(``), parentUri: vscode.Uri.parse(``) };
                const xsdUri = currentUriPair.uri;

                let schemaProperties = this.schemaPropertiesArray
                    .filterUris([xsdUri]);

                if (schemaProperties.length === 0) {
                    // eslint-disable-next-line @typescript-eslint/consistent-type-assertions
                    const schemaProperty = { schemaUri: currentUriPair.uri, parentSchemaUri: currentUriPair.parentUri, xsdContent: ``, tagCollection: new XamlTagCollection() } as XamlSchemaProperties;

                    try {
                        const xsdUriString = xsdUri.toString(true);
                        schemaProperty.xsdContent = await XsdCachedLoader.loadSchemaContentsFromUri(xsdUriString);
                        schemaProperty.tagCollection = await XsdParser.getSchemaTagsAndAttributes(schemaProperty.xsdContent, xsdUriString, (u) => xsdFileUris.push({ uri: vscode.Uri.parse(XamlSimpleParser.ensureAbsoluteUri(u, xsdUriString)), parentUri: currentUriPair.parentUri }));
                        void vscode.window.showInformationMessage(`Loaded ...${xsdUri.toString().substr(xsdUri.path.length - 16)}`);
                    } catch (err) {
                        void vscode.window.showErrorMessage(err.toString());
                    } finally {
                        this.schemaPropertiesArray.push(schemaProperty);
                        schemaProperties = [schemaProperty];
                    }
                }

                // eslint-disable-next-line @typescript-eslint/strict-boolean-expressions
                const strict = !globalSettings.schemaMapping.find(m => m.xsdUri === xsdUri.toString() && !m.strict);

                for (const sp of schemaProperties) {
                    const diagnosticResults = await XamlSimpleParser.getXamlDiagnosticData(text, sp.tagCollection, nsMap, strict);
                    diagnostics.push(this.getDiagnosticArray(diagnosticResults));
                }
            }

            this.diagnosticCollection.set(textDocument.uri, diagnostics
                .reduce((prev, next) => prev.filter(dp => next.some(dn => dn.range.start.compareTo(dp.range.start) === 0))));
        } catch (err) {
            void vscode.window.showErrorMessage(err.toString());
        } finally {
            const t1 = new Date().getTime();
            console.debug(`Linter took ${t1 - t0} milliseconds.`);
        }
    }

    private getDiagnosticArray (data: XamlDiagnosticData[]): vscode.Diagnostic[] {
        return data.map(r => {
            const position = new vscode.Position(r.line, r.column);
            const severity = (r.severity === "error") ? vscode.DiagnosticSeverity.Error
                : (r.severity === "warning") ? vscode.DiagnosticSeverity.Warning
                    : (r.severity === "info") ? vscode.DiagnosticSeverity.Information
                        : vscode.DiagnosticSeverity.Hint;

            return new vscode.Diagnostic(new vscode.Range(position, position), r.message, severity);
        });
    }
}
