import * as vscode from 'vscode';
import * as xml2js from 'xml2js';
import * as fs from 'fs';
import * as path from 'path';
import { PathLike } from 'node:fs';
import ip from "ip";
import replace from 'replace-in-file';
import { ExtensionUtils } from './ExtensionUtils';

export class UnoCsprojManager {
    public context: vscode.ExtensionContext;

    public static Register (cxt: vscode.ExtensionContext): void {
        // this instance
        const unoCsprojManager = new UnoCsprojManager();
        unoCsprojManager.context = cxt;

        // commands
        vscode.commands.registerCommand("setHotReloadHostAddress", unoCsprojManager.setHotReloadHostAddress, unoCsprojManager);
        vscode.commands.registerCommand("setDisableRoslynGenerators", unoCsprojManager.setDisableRoslynGenerators, unoCsprojManager);
        vscode.commands.registerCommand("setEnableRoslynGenerators", unoCsprojManager.setEnableRoslynGenerators, unoCsprojManager);

        // on new file of the xaml type
        vscode.workspace.onDidCreateFiles(e => {
            if (e.files.length === 1 &&
                e.files[0].fsPath.includes(".Shared") &&
                e.files[0].fsPath.includes(".xaml")) {
                void unoCsprojManager.createXamlCs(e);
            }
        });
    }

    private writeJsonToXML (result: any, path: PathLike): void {
        var xmlBuilder = new xml2js.Builder();
        var xml = xmlBuilder.buildObject(result);

        fs.writeFileSync(path, xml);
    }

    private setHotReloadHostAddressProperty (result: any, path: PathLike): void {
        result.Project
            .PropertyGroup[0].UnoRemoteControlHost = ip.address();
        // TODO: make this configurable
        result.Project
            .PropertyGroup[0].unoRemoteControlPort = "8090";

        // write back
        this.writeJsonToXML(result, path);
        ExtensionUtils.writeln(`Hot Reload server address ${ip.address()}`);
        ExtensionUtils.writeln(`Hot Reload server port 8090`);
    }

    private setDisableRoslynGeneratorsProperty (result: any, path: PathLike): void {
        result.Project
            .PropertyGroup[0].UnoUIUseRoslynSourceGenerators = "false";

        // write back
        this.writeJsonToXML(result, path);
    }

    private setEnableRoslynGeneratorsProperty (result: any, path: PathLike): void {
        result.Project
            .PropertyGroup[0].UnoUIUseRoslynSourceGenerators = "true";

        // write back
        this.writeJsonToXML(result, path);
    }

    private setReferenceXamlCs (result: any, path: PathLike, className: string, subLevel: string): void {
        result.Project
            .ItemGroup[1]
            .Compile.push({
                $: {
                    Include: `$(MSBuildThisFileDirectory)${subLevel}${className}.xaml.cs`
                },
                DependentUpon: `${className}.xaml`
            });

        result.Project
            .ItemGroup[2]
            .Page.push({
                $: {
                    Include: `$(MSBuildThisFileDirectory)${subLevel}${className}.xaml`
                },
                SubType: `Designer`,
                Generator: `MSBuild:Compile`
            });

        // write back
        this.writeJsonToXML(result, path);
    }

    private getPath (pattern: string, location?: PathLike, projitems?: boolean): PathLike | undefined {
        // get the workspace directories
        let workspacePath: PathLike;

        if (location === undefined) {
            workspacePath = path.join(vscode.workspace.rootPath!);
        } else {
            workspacePath = location;
        }

        const dirs: string[] = fs.readdirSync(workspacePath);
        let getPath: PathLike = path.join("");

        // check if the pattern exists
        const dirName = dirs.filter(dirname => dirname.includes(pattern));
        if (dirName.length === 1) {
            if (projitems === undefined || !projitems) {
                getPath = path.join(workspacePath.toString(), dirName[0], `${dirName[0]}.csproj`);
            } else if (projitems) {
                getPath = path.join(workspacePath.toString(), dirName[0], `${dirName[0]}.projitems`);
            }

            return getPath;
        }

        return undefined;
    }

    public setHotReloadHostAddress (location?: PathLike): void {
        ExtensionUtils.writeln("Configuring Hot Reload server address");
        // get the workspace directories
        const skiaGtkPath: PathLike | undefined = this.getPath(".Skia.Gtk", location);
        const skiaWasmPath: PathLike | undefined = this.getPath(".Wasm", location);

        // check if the .Skia.Gtk exists
        if (skiaGtkPath !== undefined) {
            const gtkCsproj = fs.readFileSync(skiaGtkPath, "utf-8");

            xml2js.parseString(gtkCsproj, (err, result): void => {
                if (err === null) {
                    this.setHotReloadHostAddressProperty(result, skiaGtkPath);
                }
            });
        }

        // check if the .Skia.Wasm exists
        if (skiaWasmPath !== undefined) {
            const wasmCsproj = fs.readFileSync(skiaWasmPath, "utf-8");

            xml2js.parseString(wasmCsproj, (err, result): void => {
                if (err === null) {
                    this.setHotReloadHostAddressProperty(result, skiaWasmPath);
                    ExtensionUtils.writeln("Hot Reload server address configured");
                }
            });
        }
    }

    public setEnableRoslynGenerators (): void {
        ExtensionUtils.writeln(`Configuring UnoUIUseRoslynSourceGenerators`);
        // get the workspace directories
        const skiaGtkPath: PathLike | undefined = this.getPath(".Skia.Gtk");
        const skiaWasmPath: PathLike | undefined = this.getPath(".Wasm");

        // check if the .Skia.Gtk exists
        if (skiaGtkPath !== undefined) {
            const gtkCsproj = fs.readFileSync(skiaGtkPath, "utf-8");

            xml2js.parseString(gtkCsproj, (err, result): void => {
                if (err === null) {
                    this.setEnableRoslynGeneratorsProperty(result, skiaGtkPath);
                    ExtensionUtils.writeln(`Skia.Gtk UnoUIUseRoslynSourceGenerators seted to true`);
                }
            });
        }

        // check if the .Skia.Wasm exists
        if (skiaWasmPath !== undefined) {
            const wasmCsproj = fs.readFileSync(skiaWasmPath, "utf-8");

            xml2js.parseString(wasmCsproj, (err, result): void => {
                if (err === null) {
                    this.setEnableRoslynGeneratorsProperty(result, skiaWasmPath);
                    ExtensionUtils.writeln(`WASM UnoUIUseRoslynSourceGenerators seted to true`);
                }
            });
        }
    }

    public setDisableRoslynGenerators (location?: PathLike): void {
        ExtensionUtils.writeln(`Configuring UnoUIUseRoslynSourceGenerators`);
        // get the workspace directories
        const skiaGtkPath: PathLike | undefined = this.getPath(".Skia.Gtk", location);
        const skiaWasmPath: PathLike | undefined = this.getPath(".Wasm", location);

        // check if the .Skia.Gtk exists
        if (skiaGtkPath !== undefined) {
            const gtkCsproj = fs.readFileSync(skiaGtkPath, "utf-8");

            xml2js.parseString(gtkCsproj, (err, result): void => {
                if (err === null) {
                    this.setDisableRoslynGeneratorsProperty(result, skiaGtkPath);
                    ExtensionUtils.writeln(`Skia.Gtk UnoUIUseRoslynSourceGenerators seted to false`);
                }
            });
        }

        // check if the .Skia.Wasm exists
        if (skiaWasmPath !== undefined) {
            const wasmCsproj = fs.readFileSync(skiaWasmPath, "utf-8");

            xml2js.parseString(wasmCsproj, (err, result): void => {
                if (err === null) {
                    this.setDisableRoslynGeneratorsProperty(result, skiaWasmPath);
                    ExtensionUtils.writeln(`WASM UnoUIUseRoslynSourceGenerators seted to false`);
                }
            });
        }
    }

    public async createXamlCs (fileEvent: vscode.FileCreateEvent): Promise<void> {
        // create xaml
        const newXamlLocation = path.join(fileEvent.files[0].fsPath);
        const projectName = vscode.workspace.name;
        const fileName = path.basename(newXamlLocation).replace(".xaml", "");
        const xamlTemplateLocation = path.join(this.context.extensionPath, "templates", "xaml", "New.xaml");
        const xamlCsTemplateLocation = path.join(this.context.extensionPath, "templates", "xaml", "New.xaml.cs");
        const xamlTemplateContent = fs.readFileSync(xamlTemplateLocation);
        const sharedProjitemsLocation = this.getPath(".Shared", undefined, true);
        const sharedProjitemsContent = fs.readFileSync(sharedProjitemsLocation!, "utf-8");

        fs.writeFileSync(fileEvent.files[0].fsPath, xamlTemplateContent);

        // create xaml.cs
        fs.copyFileSync(xamlCsTemplateLocation, newXamlLocation + ".cs");

        // replace projectName and fileName
        const options = {
            files: [
                newXamlLocation,
                newXamlLocation + ".cs"
            ],
            from: [
                // eslint-disable-next-line no-template-curly-in-string
                new RegExp("[$]{projectName}", "g"),
                new RegExp("[$]{fileName}", "g")
            ],
            to: [
                projectName!,
                fileName
            ]
        };
        await replace(options);

        // add the reference to the Shared.projitems
        xml2js.parseString(sharedProjitemsContent, (err, result) => {
            if (err === null) {
                let subLevel = path.basename(newXamlLocation.replace(fileName + ".xaml", ""));

                if (subLevel.includes("Shared")) {
                    subLevel = "";
                } else {
                    subLevel += "\\";
                }

                this.setReferenceXamlCs(result, sharedProjitemsLocation!, fileName, subLevel);
                ExtensionUtils.writeln(`${fileName}.xaml and ${fileName}.xaml.cs created`);

                // buil the solution
                void vscode.commands
                    .executeCommand("workbench.action.tasks.runTask", "build");

                // open it automatically with previewer
                void vscode.commands
                    .executeCommand('unoplatform.xamlPreview');
            }
        });
    }
}
