import * as vscode from 'vscode';
import { CmdOption } from './CmdOption';

export class UnoPlatformCmdProvider implements vscode.TreeDataProvider<vscode.TreeItem> {
    refresh (): void {
        console.log("refreshing...");
    }

    getTreeItem (element: vscode.TreeItem): vscode.TreeItem {
        return element;
    }

    getChildren (element?: vscode.TreeItem): Thenable<vscode.TreeItem[]> {
        /* first time */
        if (element === undefined) {
            return this.createCmdList();
        }

        return Promise.resolve([]);
    }

    private async createCmdList (): Promise<CmdOption[]> {
        return await new Promise(resolve => {
            var cmds: CmdOption[] = [];

            // create cmds
            cmds.push(new CmdOption(
                "New Uno Skia GTK project",
                "cmd0",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "createSkiaGtkProject",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            cmds.push(new CmdOption(
                "New Uno Skia GTK/WASM project",
                "cmd1",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            cmds.push(new CmdOption(
                "Setup Uno Debug Configuration",
                "cmd2",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            cmds.push(new CmdOption(
                "Disable Uno Roslyn Generators",
                "cmd3",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "setDisableRoslynGenerators",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            cmds.push(new CmdOption(
                "Enable Uno Roslyn Generators",
                "cmd4",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "setEnableRoslynGenerators",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            cmds.push(new CmdOption(
                "Set Hot Reload Server Address",
                "cmd5",
                vscode.TreeItemCollapsibleState.None,
                "",
                {
                    command: "setHotReloadHostAddress",
                    title: "",
                    arguments: []
                },
                "run.svg"
            ));

            resolve(cmds);
        });
    }
}
