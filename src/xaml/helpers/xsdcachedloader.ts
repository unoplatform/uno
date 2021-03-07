import XsdLoader from './xsdloader';
import XamlSimpleParser from './Xamlsimpleparser';

export default class XsdCachedLoader {
    private static readonly cachedSchemas: Map<string, string> = new Map<string, string>();

    public static async loadSchemaContentsFromUri (schemaLocationUri: string, formatXsd = true): Promise<string> {
        if (!XsdCachedLoader.cachedSchemas.has(schemaLocationUri)) {
            let content = await XsdLoader.loadSchemaContentsFromUri(schemaLocationUri);

            if (formatXsd) {
                content = await XamlSimpleParser.formatXaml(content, "\t", "\n", "multiLineAttributes");
            }

            XsdCachedLoader.cachedSchemas.set(schemaLocationUri, content);
        }

        const result = XsdCachedLoader.cachedSchemas.get(schemaLocationUri);

        if (result !== undefined) {
            return result;
        }

        throw new Error(`Cannot get schema contents from '${schemaLocationUri}'`);
    }
}
