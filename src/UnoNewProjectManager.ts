import { PathLike } from 'node:fs';
import * as vscode from 'vscode';
import { ExtensionUtils } from './ExtensionUtils';
import * as fs from 'fs';
import * as path from 'path';
import { UnoCsprojManager } from './UnoCsprojManager';

export class UnoNewProjectManager {
    public context: vscode.ExtensionContext;

    public static Register (context: vscode.ExtensionContext): void {
        // the instance
        const unoNewProjectManager = new UnoNewProjectManager();
        unoNewProjectManager.context = context;

        vscode.commands.registerCommand("createSkiaGtkProject", unoNewProjectManager.createSkiaGtkProject, unoNewProjectManager);
    }

    private async executeDotnetWithArgs (cwd: PathLike, args: string[]): Promise<boolean> {
        const term = ExtensionUtils.createTerminal(
            this.context,
            "Creating Uno Project",
            cwd.toString(),
            await ExtensionUtils.getDotnetPath(),
            args
        );
        term.show();

        return await new Promise(resolve => {
            const disp = vscode.window.onDidCloseTerminal(terminal => {
                if (terminal === term) {
                    disp.dispose();
                    resolve(!(term.exitStatus!.code! > 0));
                }
            });
        });
    }

    private async prepareProjectLocation (projectName: string): Promise<PathLike> {
        return await new Promise(resolve => {
            const options: vscode.OpenDialogOptions = {
                canSelectMany: false,
                openLabel: 'Choose Project Location',
                canSelectFiles: false,
                canSelectFolders: true,
                filters: {
                }
            };

            void vscode.window.showOpenDialog(options).then(fileUri => {
                if (fileUri?.[0] !== undefined) {
                    const projectLocation = path.join(fileUri[0].fsPath, projectName);
                    fs.mkdirSync(projectLocation);
                    resolve(projectLocation);
                }
            });
        });
    }

    private async getProjectName (): Promise<string | undefined> {
        return await new Promise(resolve => {
            const options: vscode.InputBoxOptions = {
                ignoreFocusOut: true,
                password: false,
                placeHolder: "project name"
            };

            void vscode.window.showInputBox(options).then(value => {
                resolve(value);
            });
        });
    }

    public async createSkiaGtkProject (): Promise<void> {
        ExtensionUtils.showProgress("Creating Uno Skia Gtk Project", "", async (res, prog) => {
            // choose app name
            prog?.report({
                message: "Choosing unoapp name"
            });
            const projectName = await this.getProjectName();
            if (projectName === undefined) {
                res();
            }

            // choose folder location
            prog?.report({
                message: "Choosing unoapp location"
            });
            const projectLocation = await this.prepareProjectLocation(projectName!);
            if (projectLocation === undefined) {
                res();
            }

            // use dotnet new
            prog?.report({
                message: `Creating ${projectName!} unoapp`
            });
            const createSuccess = await this.executeDotnetWithArgs(projectLocation,
                [
                    "new",
                    "unoapp",
                    "-wasm=false",
                    "-uwp=false",
                    "-ios=false",
                    "-android=false",
                    "-macos=false",
                    "-skia-wpf=false",
                    "-st=false"
                ]
            );
            if (!createSuccess) {
                res();
            }

            // create the .vscode
            // TODO: I will not automaticaly create the .vscode for Skia.Gtk let's omnisharp do it
            // csproj automations
            prog?.report({
                message: `Configuring ${projectName!} unoapp`
            });
            const unoCsprojManager = new UnoCsprojManager();
            // fix roslyn generators
            unoCsprojManager.setDisableRoslynGenerators(projectLocation);
            // add the localhost to the hot reload address
            unoCsprojManager.setHotReloadHostAddress(projectLocation);

            // first build to generate code behind
            prog?.report({
                message: `Building ${projectName!} unoapp`
            });
            const buildSuccess = await this.executeDotnetWithArgs(projectLocation,
                [
                    "build"
                ]
            );
            if (!buildSuccess) {
                res();
            }

            // reload the workspace
            await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(projectLocation.toString()), false);
        });
    }
}
