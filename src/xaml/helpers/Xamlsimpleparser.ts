/* eslint-disable no-useless-escape */
import { XamlTagCollection, XamlDiagnosticData, XamlScope, CompletionString } from '../types';

export default class XamlSimpleParser {
    public static async getXamlDiagnosticData (XamlContent: string, xsdTags: XamlTagCollection, nsMap: Map<string, string>, strict = true): Promise<XamlDiagnosticData[]> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        return await new Promise<XamlDiagnosticData[]>(
            (resolve) => {
                const result: XamlDiagnosticData[] = [];
                const nodeCacheAttributes = new Map<string, CompletionString[]>();
                const nodeCacheTags = new Map<string, CompletionString | undefined>();

                const getAttributes = (nodeName: string): CompletionString[] | undefined => {
                    if (!nodeCacheAttributes.has(nodeName)) {
                        nodeCacheAttributes.set(nodeName, xsdTags.loadAttributesEx(nodeName, nsMap));
                    }

                    return nodeCacheAttributes.get(nodeName);
                };

                const getTag = (nodeName: string): CompletionString | undefined => {
                    if (!nodeCacheTags.has(nodeName)) {
                        nodeCacheTags.set(nodeName, xsdTags.loadTagEx(nodeName, nsMap));
                    }

                    return nodeCacheTags.get(nodeName);
                };

                parser.onerror = () => {
                    if (undefined === result.find(e => e.line === parser.line)) {
                        result.push({
                            line: parser.line,
                            column: parser.column,
                            message: parser.error.message,
                            severity: strict ? "error" : "warning"
                        });
                    }
                    parser.resume();
                };

                parser.onopentag = (tagData: { name: string, isSelfClosing: boolean, attributes: Map<string, string>; }) => {
                    const nodeNameSplitted: string[] = tagData.name.split('.');

                    if (getTag(nodeNameSplitted[0]) !== undefined) {
                        const schemaTagAttributes = getAttributes(nodeNameSplitted[0]) ?? [];
                        nodeNameSplitted.shift();

                        const XamlAllowed: string[] = [":schemaLocation", ":noNamespaceSchemaLocation", "Xaml:space"];
                        Object.keys(tagData.attributes).concat(nodeNameSplitted).forEach((a: string) => {
                            if (schemaTagAttributes.findIndex(sta => sta.name === a) < 0 && !a.includes(":!") &&
                                a !== "Xamlns" && !a.startsWith("Xamlns:") &&
                                XamlAllowed.findIndex(all => a.endsWith(all)) < 0) {
                                result.push({
                                    line: parser.line,
                                    column: parser.column,
                                    message: `Unknown Xaml attribute '${a}' for tag '${tagData.name}'`,
                                    severity: strict ? "info" : "hint"
                                });
                            }
                        });
                    } else if (!tagData.name.includes(":!") && xsdTags.length > 0) {
                        result.push({
                            line: parser.line,
                            column: parser.column,
                            message: `Unknown Xaml tag '${tagData.name}'`,
                            severity: strict ? "info" : "hint"
                        });
                    }
                };

                parser.onend = () => {
                    resolve(result);
                };

                parser.write(XamlContent).close();
            });
    }

    public static ensureAbsoluteUri (u: string, documentUri: string): string {
        return (u.indexOf("/") > 0 && u.indexOf(".") !== 0) ? u : documentUri.substring(0, documentUri.lastIndexOf("/") + 1) + u;
    }

    public static async getSchemaXsdUris (XamlContent: string, documentUri: string, schemaMapping: Array<{ Xamlns: string, xsdUri: string; }>): Promise<string[]> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        return await new Promise<string[]>(
            (resolve) => {
                const result: string[] = [];

                if (documentUri.startsWith("git")) {
                    resolve(result);
                    return;
                }

                parser.onerror = () => {
                    parser.resume();
                };

                parser.onattribute = (attr: { name: string, value: string; }) => {
                    if (attr.name.endsWith(":schemaLocation")) {
                        const uris = attr.value.split(/\s+/).filter((v, i) => i % 2 === 1 || v.toLowerCase().endsWith(".xsd"));
                        result.push(...uris.map(u => XamlSimpleParser.ensureAbsoluteUri(u, documentUri)));
                    } else if (attr.name.endsWith(":noNamespaceSchemaLocation")) {
                        const uris = attr.value.split(/\s+/);
                        result.push(...uris.map(u => XamlSimpleParser.ensureAbsoluteUri(u, documentUri)));
                    } else if (attr.name === "Xamlns") {
                        const newUriStrings = schemaMapping
                            .filter(m => m.Xamlns === attr.value)
                            .flatMap(m => m.xsdUri.split(/\s+/));
                        result.push(...newUriStrings);
                    } else if (attr.name.startsWith("Xamlns:")) {
                        const newUriStrings = schemaMapping
                            .filter(m => m.Xamlns === attr.value)
                            .flatMap(m => m.xsdUri.split(/\s+/));
                        result.push(...newUriStrings);
                    }
                };

                parser.onend = () => {
                    resolve([...new Set(result)]);
                };

                parser.write(XamlContent).close();
            });
    }

    public static async getNamespaceMapping (XamlContent: string): Promise<Map<string, string>> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        return await new Promise<Map<string, string>>(
            (resolve) => {
                const result: Map<string, string> = new Map<string, string>();

                parser.onerror = () => {
                    parser.resume();
                };

                parser.onattribute = (attr: { name: string, value: string; }) => {
                    if (attr.name.startsWith("Xamlns:")) {
                        result.set(attr.value, attr.name.substring("Xamlns:".length));
                    }
                };

                parser.onend = () => {
                    resolve(result);
                };

                parser.write(XamlContent).close();
            });
    }

    public static async getScopeForPosition (XamlContent: string, offset: number): Promise<XamlScope> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        return await new Promise<XamlScope>(
            (resolve) => {
                let result: XamlScope;
                let previousStartTagPosition = 0;
                const updatePosition = (): void => {
                    if ((parser.position >= offset) && result == null) {
                        let content = XamlContent.substring(previousStartTagPosition, offset);
                        content = content.lastIndexOf("<") >= 0 ? content.substring(content.lastIndexOf("<")) : content;

                        const normalizedContent = content.concat(" ").replace("/", "").replace("\t", " ").replace("\n", " ").replace("\r", " ");
                        const tagName = content.substring(1, normalizedContent.indexOf(" "));
                        const tagAttr = normalizedContent.match(/ .*?(?==)/g);

                        result = {
                            tagName: /^[a-zA-Z0-9_:\.\-]*$/.test(tagName) ? tagName : undefined,
                            tagAttr: (tagAttr !== null && tagAttr.length > 0) ? tagAttr[tagAttr.length - 1].trim() : undefined,
                            context: undefined
                        };

                        if (content.lastIndexOf("=") === (parser.position - 1)) {
                            result.context = "value";
                        } else if (content.lastIndexOf(">") >= content.lastIndexOf("<")) {
                            result.context = "text";
                        } else {
                            const lastTagText = content.substring(content.lastIndexOf("<"));
                            if (!/\s/.test(lastTagText)) {
                                result.context = "element";
                            } else if ((lastTagText.split(`"`).length % 2) !== 0) {
                                result.context = "attribute";
                            } else {
                                result.context = "value";
                            }
                        }
                    }

                    previousStartTagPosition = parser.startTagPosition - 1;
                };

                parser.onerror = () => {
                    parser.resume();
                };

                parser.ontext = () => {
                    updatePosition();
                };

                parser.onopentagstart = () => {
                    updatePosition();
                };

                parser.onattribute = () => {
                    updatePosition();
                };

                parser.onclosetag = () => {
                    updatePosition();
                };

                parser.onend = () => {
                    if (result === undefined) {
                        result = { tagName: undefined, tagAttr: undefined, context: undefined };
                    }
                    resolve(result);
                };

                parser.write(XamlContent).close();
            });
    }

    public static async checkXaml (XamlContent: string): Promise<boolean> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        let result = true;
        return await new Promise<boolean>(
            (resolve) => {
                parser.onerror = () => {
                    result = false;
                    parser.resume();
                };

                parser.onend = () => {
                    resolve(result);
                };

                parser.write(XamlContent).close();
            });
    }

    public static async formatXaml (XamlContent: string, indentationString: string, eol: string, formattingStyle: "singleLineAttributes" | "multiLineAttributes" | "fileSizeOptimized"): Promise<string> {
        // eslint-disable-next-line @typescript-eslint/no-var-requires
        const sax = require("sax");
        const parser = sax.parser(true);

        const result: string[] = [];
        const XamlDepthPath: Array<{ tag: string, selfClosing: boolean, isTextContent: boolean; }> = [];

        const multiLineAttributes = formattingStyle === "multiLineAttributes";
        indentationString = (formattingStyle === "fileSizeOptimized") ? "" : indentationString;

        const getIndentation = (): string =>
            (result[result.length - 1] == null || result[result.length - 1].includes("<") || result[result.length - 1].includes(">"))
                ? eol + Array(XamlDepthPath.length).fill(indentationString).join("")
                : "";

        const getEncodedText = (t: string): string =>
            t.replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&apos;');

        return new Promise<string>(
            (resolve) => {
                parser.onerror = () => {
                    parser.resume();
                };

                parser.ontext = (t) => {
                    result.push(/^\s*$/.test(t) ? `` : getEncodedText(`${(t as string)}`));
                };

                parser.ondoctype = (t) => {
                    result.push(`${eol}<!DOCTYPE${(t as string)}>`);
                };

                parser.onprocessinginstruction = (instruction: { name: string, body: string; }) => {
                    result.push(`${eol}<?${instruction.name} ${instruction.body}?>`);
                };

                parser.onsgmldeclaration = (t) => {
                    result.push(`${eol}<!${(t as string)}>`);
                };

                parser.onopentag = (tagData: { name: string, isSelfClosing: boolean, attributes: Map<string, string>; }) => {
                    const argString: string[] = [""];
                    for (const arg in tagData.attributes) {
                        argString.push(` ${arg}="${getEncodedText(tagData.attributes[arg])}"`);
                    }

                    if (XamlDepthPath.length > 0) {
                        XamlDepthPath[XamlDepthPath.length - 1].isTextContent = false;
                    }

                    const attributesStr = argString.join(multiLineAttributes ? `${getIndentation()}${indentationString}` : ``);
                    result.push(`${getIndentation()}<${tagData.name}${attributesStr}${tagData.isSelfClosing ? "/>" : ">"}`);

                    XamlDepthPath.push({
                        tag: tagData.name,
                        selfClosing: tagData.isSelfClosing,
                        isTextContent: true
                    });
                };

                parser.onclosetag = (t) => {
                    const tag = XamlDepthPath.pop();

                    if (tag != null && !tag.selfClosing) {
                        result.push(tag.isTextContent ? `</${(t as string)}>` : `${getIndentation()}</${(t as string)}>`);
                    }
                };

                parser.oncomment = (t) => {
                    result.push(`<!--${(t as string)}-->`);
                };

                parser.onopencdata = () => {
                    result.push(`${eol}<![CDATA[`);
                };

                parser.oncdata = (t) => {
                    result.push(t);
                };

                parser.onclosecdata = () => {
                    result.push(`]]>`);
                };

                parser.onend = () => {
                    resolve(result.join(``));
                };
                parser.write(XamlContent).close();
            });
    }
}
