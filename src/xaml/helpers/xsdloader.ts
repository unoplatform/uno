import { ReadStream } from 'fs';

export default class XsdLoader {
    public static async loadSchemaContentsFromUri (schemaLocationUri: string): Promise<string> {
        return await new Promise<string>(
            (resolve, reject) => {
                let resultContent: string = ``;
                // eslint-disable-next-line @typescript-eslint/no-var-requires
                const getUri = require('get-uri');

                getUri(schemaLocationUri, function (err: any, rs: ReadStream): void {
                    if (err != null) {
                        reject(new Error(`Error getting XSD:\n${(err.toString() as string)}`));
                        return;
                    }

                    rs.on('data', (buf: any) => {
                        resultContent += (buf.toString() as string);
                    });

                    rs.on('end', () => {
                        resolve(resultContent);
                    });
                });
            });
    }
};
