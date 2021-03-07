import * as vscode from 'vscode';
import * as path from 'path';

type ProgressCallback = (resolve?: any, progress?: vscode.Progress<{
    message?: string | undefined;
    increment?: number | undefined;
}>) => Promise<void>;

/* static class */
export class ExtensionUtils {
    static outputChannel: vscode.OutputChannel = vscode.window.createOutputChannel("Uno Platform Ext");

    static getTimeStampFormated (): string {
        const date = new Date();
        const month = `0${(date.getMonth() + 1)}`.slice(-2);
        const day = `0${(date.getDate() + 1)}`.slice(-2);
        const hours = `0${(date.getHours() + 1)}`.slice(-2);
        const minutes = `0${(date.getMinutes() + 1)}`.slice(-2);
        const seconds = `0${(date.getSeconds() + 1)}`.slice(-2);
        const milliseconds = `00${date.getMilliseconds()}`.slice(-3);

        return `${month}-${day} ${hours}:${minutes}:${seconds}.${milliseconds}`;
    }

    static writeln (msg: string, show: boolean = false): void {
        if (msg.trim() !== "") {
            const timeformatted = `[${ExtensionUtils.getTimeStampFormated()}] `;
            ExtensionUtils.outputChannel.appendLine(timeformatted + msg.trim());

            if (show) {
                ExtensionUtils.outputChannel.show(true);
            }
        }
    }

    static showProgress (title: string, msg: string,
        resFunc: ProgressCallback, cancellable: boolean = false): void {
        void vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: title,
            cancellable: cancellable
        }, async progressNotification => {
            progressNotification.report({ message: msg });

            return await new Promise(resolve => {
                void resFunc(resolve, progressNotification);
            });
        });
    }

    static showError (message: string): void {
        ExtensionUtils.writeln(message, true);
        void vscode.window.showErrorMessage(message);
    }

    static showSuccess (message: string): void {
        ExtensionUtils.writeln(message, true);
        void vscode.window.showInformationMessage(message);
    }

    static showWarning (message: string, modal?: boolean): void {
        ExtensionUtils.writeln(message, true);
        void vscode.window.showWarningMessage(message, {
            modal: modal
        });
    }

    static createTerminal (context: vscode.ExtensionContext, name: string,
        shellCwd: string, shellPath: string | undefined, shellArgs: string[]): void {
        var cwd = path.join(context.extensionPath, shellCwd);

        var toUnoRemoteHost: vscode.TerminalOptions = { shellArgs: [] };
        toUnoRemoteHost.name = name;
        toUnoRemoteHost.cwd = cwd;
        toUnoRemoteHost.shellPath = shellPath;
        toUnoRemoteHost.shellArgs = shellArgs;

        vscode.window.createTerminal(toUnoRemoteHost);
    }
}
