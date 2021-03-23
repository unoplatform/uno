import * as vscode from 'vscode';
import * as path from 'path';
/* import * as fs from 'fs';
import { ExtensionUtils } from './ExtensionUtils'; */

/**
 * React to the first load of content
 * After the receive of the onload message we need to wait more 1s to load the XAML
 * @param panel
 * @param docFromClick
 */
function reactContent (panel: vscode.WebviewPanel,
    docFromClick: vscode.TextDocument | undefined): void {
    const editor = vscode.window.activeTextEditor;
    const doc = editor?.document;

    if (doc?.languageId === "xml" && doc.fileName.includes(".xaml")) {
        void panel.webview.postMessage(doc?.getText());
    } else {
        // at the moment we don't have a XAML document in focus
        // we will use the document reference from the moment of the click
        void panel.webview.postMessage(docFromClick?.getText());
    }
}

/**
 * Create the linter entries for show the erros received from the playground
 * @param line
 * @param column
 * @param msg
 */
function setErrorToDoc (line: number, column: number, msg: string): vscode.DiagnosticCollection | undefined {
    const editor = vscode.window.activeTextEditor;
    const doc = editor?.document;

    if (doc?.languageId === "xml" && doc.fileName.includes(".xaml")) {
        const diagnsCollection = vscode.languages.createDiagnosticCollection();
        const pos: vscode.Position = new vscode.Position(line, column);

        const diagns = new vscode.Diagnostic(
            new vscode.Range(pos, pos),
            msg,
            vscode.DiagnosticSeverity.Error
        );

        diagnsCollection.set(doc.uri, [diagns]);
        return diagnsCollection;
    }

    return undefined;
}

/**
 * Clear the linter entries
 * @param diagns
 */
function clearErrorsFromDoc (diagns: vscode.DiagnosticCollection | undefined): void {
    if (diagns != null) {
        diagns.clear();
    }
}

/**
 * Create a instance of a webview panel and load the Uno Playground
 * This will check for change events on documents of type XAML to make a fake
 * "hot reload"
 */
export function createXAMLPreview (): vscode.Webview {
    const panel = vscode.window.createWebviewPanel(
        'unoplatform.xamlPreview',
        'XAML Preview',
        {
            viewColumn: vscode.ViewColumn.Beside,
            preserveFocus: true
        },
        {
            enableScripts: true,
            retainContextWhenHidden: true
        }
    );

    // linter entries
    var diagns: vscode.DiagnosticCollection | undefined;
    // get the reference to the actual document
    const editor = vscode.window.activeTextEditor;
    const doc = editor?.document;
    // store the xaml already sent and the previous
    var xamlSent: string | undefined = "";
    var xamlSentPrev: string | undefined = doc?.getText();
    var prevTimeout: NodeJS.Timeout;

    var sendAndStoreXAML = (xaml: string | undefined): void => {
        xamlSentPrev = xaml;
        xamlSent = xaml;

        void panel.webview.postMessage(xamlSent);
    };

    // panel icon
    panel.iconPath = {
        light: vscode.Uri.file(path.join(__filename, "..", "..", "media", "light", "eye.svg")),
        dark: vscode.Uri.file(path.join(__filename, "..", "..", "media", "dark", "eye.svg"))
    };

    // load the custom playground
    panel.webview.html = `
        <script>
            window.origin = "https://microhobby.com.br/X/";
            window.preview = undefined;
            const vscode = acquireVsCodeApi();

            window.onload = () => {
                console.log("webview loaded");

                // get the iframe reference
                let elem = document.getElementById("preview");
                window.preview = elem;
                // set the iframe height
                elem.height = window.innerHeight;
            };

            // refresh the iframe height
            window.onresize = () => {
                if (window.preview)
                    window.preview.height = window.innerHeight;
            };

            // receive messages from vscode and from iframe
            window.onmessage = (event) => {
                // iframe -> onload
                if (event.data === "uno_playground_loaded") {
                    vscode.postMessage(event.data);
                    return;
                }

                // iframe -> oncmd
                if (event.data && event.data.cmd) {
                    vscode.postMessage(event.data);
                }

                // vscode -> onchange
                if (window.preview)
                    window.preview
                        .contentWindow
                        .postMessage(event.data, window.origin);
            }
        </script>

        <iframe
            id="preview"
            width="100%"
            height="350"
            style="border: none"
            src="https://microhobby.com.br/X/">
        </iframe>
    `;

    // we are receiving data from playground
    var ondidrm = panel.webview.onDidReceiveMessage((data) => {
        console.log(data);

        // onload
        if (data === "uno_playground_loaded") {
            reactContent(panel, doc);
            return;
        }

        // oncmd == xaml error
        if (data.cmd === "error") {
            clearInterval(prevTimeout);
            sendAndStoreXAML(xamlSentPrev);
            clearErrorsFromDoc(diagns);
            diagns = setErrorToDoc(data.line - 4, data.column, data.msg);
            return;
        }

        // oncmd == invalid xaml property value
        if (data.cmd === "invalid") {
            clearInterval(prevTimeout);
            sendAndStoreXAML(xamlSentPrev);
            const editor = vscode.window.activeTextEditor;

            clearErrorsFromDoc(diagns);

            if (editor != null) {
                diagns = setErrorToDoc(editor.selection.start.line, editor.selection.end.line, data.msg);
            }
        }
    });

    // checking if the user is editing the xaml file
    var ondidchange = vscode.workspace.onDidChangeTextDocument(() => {
        const editor = vscode.window.activeTextEditor;
        const doc = editor?.document;

        // ok, so lets reload the xaml changes
        if (doc?.languageId === "xml" && doc.fileName.includes(".xaml")) {
            clearInterval(prevTimeout);
            if (xamlSent !== doc?.getText()) {
                clearErrorsFromDoc(diagns);
                xamlSent = doc?.getText();

                prevTimeout = setTimeout(() => {
                    xamlSentPrev = xamlSent;
                    void panel.webview.postMessage(xamlSent);
                }, 500);
            }
        }
    });

    // generate the xaml symbols on the save of a xaml file
    // TODO: we need to best think about the auto build on save
    var ondidsave = vscode.workspace.onDidSaveTextDocument(() => {
        const editor = vscode.window.activeTextEditor;
        const doc = editor?.document;

        // ok, so lets rebuild to generate the xaml.cs
        if (doc?.languageId === "xml" && doc.fileName.includes(".xaml")) {
            void vscode.commands
                .executeCommand("workbench.action.tasks.runTask", "build");
        }
    });

    // do not forget to dispose everything on the dispose of panel
    panel.onDidDispose(() => {
        ondidrm.dispose();
        ondidchange.dispose();
        ondidsave.dispose();
        if (diagns != null) {
            diagns.dispose();
        }
    });

    return panel.webview;
}
