import * as vscode from 'vscode';
import * as xamlpreview from './unoplayground';
import * as XamlComplete from './xaml/XamlExt';
import { ExtensionUtils } from './ExtensionUtils';
import which from 'which';
import * as path from 'path';
import { UnoPlatformCmdProvider } from './UnoPlatformCmdProvider';
import { UnoCsprojManager } from './UnoCsprojManager';
import { UnoNewProjectManager } from './UnoNewProjectManager';

export function activate (context: vscode.ExtensionContext): void {
    // load the xaml contributions
    XamlComplete.XamlActivate(context);

    // now try to load the uno playground xaml preview
    context.subscriptions.push(
        vscode.commands.registerCommand(
            'unoplatform.xamlPreview',
            xamlpreview.createXAMLPreview
        )
    );

    context.subscriptions.push(
        vscode.commands.registerCommand(
            'unoplatform.xamlBuild', () => {
                // build solution to update XAML symbols
                void vscode.commands
                    .executeCommand("workbench.action.tasks.runTask", "build");
            }
        )
    );

    context.subscriptions.push(
        vscode.commands.registerCommand(
            'unoplatform.xamlCodeBehind', () => {
                // open the code behind
                const editor = vscode.window.activeTextEditor;
                const doc = editor?.document;

                if (doc?.languageId === "xml" && doc.fileName.includes(".xaml")) {
                    const codeBUri: vscode.Uri = vscode.Uri
                        .file((doc.uri.fsPath + ".cs"));

                    void vscode.commands
                        .executeCommand("vscode.open", codeBUri);
                }
            }
        )
    );

    ExtensionUtils.showProgress("Initializing Uno Platform Ext ...", "",
        async (res, pro): Promise<void> => {
            // validate environment
            // check dotnet
            var dotnetPath: string | undefined;
            await which("dotnet", (err, path) => {
                if (err === null) {
                    dotnetPath = path;
                } else {
                    ExtensionUtils.showError(
                        "dotnet was not found. Make sure you have the dotnet SDK installed."
                    );
                    res();
                }
            });

            // TODO: it would be nice to have some way to update it through CI
            // artifacts instead of distributing it in the repository
            ExtensionUtils.writeln("Creating HotReload Server", true);
            const term = ExtensionUtils.createTerminal(context, "HotReload Server",
                path.join(context.extensionPath, "uno-remote-host"), dotnetPath,
                [
                    "Uno.UI.RemoteControl.Host.dll",
                    // TODO: make this configurable
                    "--httpPort=8090"
                ]
            );

            // monitor for the hot reload server close
            const termMonitor = vscode.window.onDidCloseTerminal(terminal => {
                if (terminal === term && (terminal.exitStatus?.code !== 0)) {
                    process.env.UNO_HOT_RELOAD_HOST_RUNNING = "false";
                    // we had an error
                    ExtensionUtils.showWarning(
                        "Uno Platform Hot Reload server does not running."
                    );
                    ExtensionUtils.writeln("Please, make sure there is no other instance of uno-host-reload running.");
                    ExtensionUtils.writeln("Please, make sure you have dotnet runtime 3.1 installed.");
                    ExtensionUtils.writeln("Please, make sure that the port 8090 is not in use.");

                    termMonitor.dispose();
                }
            });

            // register the commands
            ExtensionUtils.writeln("Registering commands");
            UnoCsprojManager.Register(context);
            UnoNewProjectManager.Register(context);

            // ok now create the activity bar view content
            const cmdNodesProvider = new UnoPlatformCmdProvider();
            vscode.window.registerTreeDataProvider("unoDevCmdView", cmdNodesProvider);

            ExtensionUtils.writeln("Uno Platform VS Code extension running 😎", true);
            res();
        });

    // all ok?
    console.log('Uno Platform extension loaded');
}

// this method is called when your extension is deactivated
export function deactivate (): void { }
