import * as vscode from 'vscode';
import { XamlCompleteSettings, XamlSchemaPropertiesArray } from './types';
import XamlLinterProvider from './linterprovider';
import XamlCompletionItemProvider from './completionitemprovider';
import XamlFormatProvider from './formatprovider';
import XamlRangeFormatProvider from './rangeformatprovider';
import AutoCompletionProvider from './autocompletionprovider';
import XamlHoverProvider from './hoverprovider';
import XamlDefinitionProvider from './definitionprovider';
import XamlDefinitionContentProvider from './definitioncontentprovider';
import XamlColorProvider from './colorprovider';

export declare let globalSettings: XamlCompleteSettings;

export const languageId = 'xml';

export const schemaId = 'xml2xsd-definition-provider';

export function XamlActivate (context: vscode.ExtensionContext): void {
    console.debug(`Activate XamlComplete`);

    vscode.workspace.onDidChangeConfiguration(loadConfiguration, undefined, context.subscriptions);
    loadConfiguration();

    const schemaPropertiesArray = new XamlSchemaPropertiesArray();
    const completionitemprovider = vscode.languages.registerCompletionItemProvider(
        { language: languageId, scheme: 'file' },
        new XamlCompletionItemProvider(context, schemaPropertiesArray));

    const formatprovider = vscode.languages.registerDocumentFormattingEditProvider(
        { language: languageId, scheme: 'file' },
        new XamlFormatProvider(context, schemaPropertiesArray));

    const rangeformatprovider = vscode.languages.registerDocumentRangeFormattingEditProvider(
        { language: languageId, scheme: 'file' },
        new XamlRangeFormatProvider(context, schemaPropertiesArray));

    const hoverprovider = vscode.languages.registerHoverProvider(
        { language: languageId, scheme: 'file' },
        new XamlHoverProvider(context, schemaPropertiesArray));

    const definitionprovider = vscode.languages.registerDefinitionProvider(
        { language: languageId, scheme: 'file' },
        new XamlDefinitionProvider(context, schemaPropertiesArray));

    const linterprovider = new XamlLinterProvider(context, schemaPropertiesArray);

    const autocompletionprovider = new AutoCompletionProvider(context, schemaPropertiesArray);

    const definitioncontentprovider = vscode.workspace.registerTextDocumentContentProvider(schemaId, new XamlDefinitionContentProvider(context, schemaPropertiesArray));

    const colorprovider = vscode.languages.registerColorProvider(
        { language: languageId, scheme: 'file' }, new XamlColorProvider());

    context.subscriptions.push(
        completionitemprovider,
        formatprovider,
        rangeformatprovider,
        hoverprovider,
        definitionprovider,
        linterprovider,
        autocompletionprovider,
        definitioncontentprovider,
        colorprovider);
}

function loadConfiguration (): void {
    const section = vscode.workspace.getConfiguration('xamlComplete', null);
    globalSettings = new XamlCompleteSettings();
    globalSettings.schemaMapping = section.get('schemaMapping', []);
    globalSettings.formattingStyle = section.get('formattingStyle', "singleLineAttributes");
}

export function deactivate (): void {
    console.debug(`Deactivate XamlComplete`);
}
