import { PathLike } from 'node:fs';
import * as vscode from 'vscode';
import { ExtensionUtils } from './ExtensionUtils';
import * as fs from 'fs';
import * as path from 'path';
import { UnoCsprojManager } from './UnoCsprojManager';
import { UnoOmnisharpManager } from './UnoOmnisharpManager';

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

    private async prepareProjectLocation (projectName: string): Promise<PathLike | undefined> {
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
                } else {
                    resolve(undefined);
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

    private async createGenericProject (specificTitle: string,
        specific: (prjName: string | undefined, prjLocate: PathLike | undefined) => Promise<boolean>): Promise<void> {
        ExtensionUtils.writeln(specificTitle);
        ExtensionUtils.showProgress(specificTitle, "", async (res, prog) => {
            // choose app name
            prog?.report({
                message: "Choosing unoapp name"
            });
            const projectName = await this.getProjectName();
            if (projectName === undefined) {
                ExtensionUtils.writeln(`Aborting creation, name cannot be undefined`);
                res();
            }

            // choose folder location
            prog?.report({
                message: "Choosing unoapp location"
            });
            const projectLocation = await this.prepareProjectLocation(projectName!);
            if (projectLocation === undefined) {
                ExtensionUtils.writeln(`Aborting creation, project location cannot be undefined`);
                res();
            }

            // creation is specific
            prog?.report({
                message: `Creating ${projectName!}`
            });
            const createSuccess = await specific(projectName, projectLocation);
            if (!createSuccess) {
                ExtensionUtils.writeln(`Aborting creation, errors during dotnet new`);
                res();
            }

            // csproj automations
            prog?.report({
                message: `Configuring ${projectName!}`
            });
            const unoCsprojManager = new UnoCsprojManager();
            // fix roslyn generators
            unoCsprojManager.setDisableRoslynGenerators(projectLocation);
            // add the localhost to the hot reload address
            unoCsprojManager.setHotReloadHostAddress(projectLocation);

            // create the .vscode
            prog?.report({
                message: `Setting debug targets for ${projectName!}`
            });
            const unoOmnisharpManager = new UnoOmnisharpManager();
            unoOmnisharpManager.context = this.context;
            await unoOmnisharpManager.createSkiaGtkConfiguration(projectName!, projectLocation!);

            // first build to generate code behind
            prog?.report({
                message: `Building ${projectName!} unoapp`
            });
            const buildSuccess = await this.executeDotnetWithArgs(projectLocation!,
                [
                    "build"
                ]
            );
            if (!buildSuccess) {
                // TODO: in this case maybe we need a cleanup 🤷‍♂️
                ExtensionUtils.writeln(`Aborting creation, errors during dotnet build`);
                res();
            }

            // reload the workspace
            await vscode.commands.executeCommand('vscode.openFolder', vscode.Uri.file(projectLocation!.toString()), false);
        });
    }

    public async createSkiaGtkProject (): Promise<void> {
        await this.createGenericProject("New Uno Skia.Gtk app",
            async (projectName: string | undefined, projectLocation: PathLike | undefined): Promise<boolean> => {
                const success = await this.executeDotnetWithArgs(projectLocation!,
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

                if (success) {
                    const unoOmnisharpManager = new UnoOmnisharpManager();
                    unoOmnisharpManager.context = this.context;
                    await unoOmnisharpManager.createSkiaGtkConfiguration(projectName!, projectLocation!);
                    ExtensionUtils.writeln(`.vscode settings for Skia.Gtk ${projectName!} App created`);
                }

                return success;
            });
    }
}
