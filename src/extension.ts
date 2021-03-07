import * as vscode from 'vscode';
import * as xamlpreview from './unoplayground';
import * as XamlComplete from './xaml/XamlExt';
import { ExtensionUtils } from './ExtensionUtils';
import which from 'which';
import * as path from 'path';

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

    ExtensionUtils.showProgress("Initializing Uno Platform Ext ...", "",
        async (res, pro): Promise<void> => {
            // check dotnet
            var dotnetPath: string | undefined;
            await which("dotnet", (err, path) => {
                if (err === null) {
                    dotnetPath = path;
                }
            });

            // TODO: it would be nice to have some way to update it through CI
            // artifacts instead of distributing it in the repository
            var cwd = path.join(context.extensionPath, "uno-remote-host");

            var toUnoRemoteHost: vscode.TerminalOptions = { shellArgs: [] };
            toUnoRemoteHost.name = "HotReload Server";
            toUnoRemoteHost.cwd = cwd;
            toUnoRemoteHost.shellPath = dotnetPath;
            toUnoRemoteHost.shellArgs = ["Uno.UI.RemoteControl.Host.dll", "--httpPort=8090"];

            ExtensionUtils.createTerminal(context, "HotReload Server",
                "uno-remote-host", dotnetPath,
                [
                    "Uno.UI.RemoteControl.Host.dll",
                    // TODO: make this configurable
                    "--httpPort=8090"
                ]
            );

            res();
        });

    // all ok?
    console.log('Uno Platform extension loaded');
}

// this method is called when your extension is deactivated
export function deactivate (): void { }
