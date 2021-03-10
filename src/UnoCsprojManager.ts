import * as vscode from 'vscode';
import * as xml2js from 'xml2js';
import * as fs from 'fs';
import * as path from 'path';
import { PathLike } from 'node:fs';
import ip from "ip";

export class UnoCsprojManager {
    public static Register (): void {
        // this instance
        const unoCsprojManager = new UnoCsprojManager();

        // commands
        vscode.commands.registerCommand("setHotReloadHostAddress", unoCsprojManager.setHotReloadHostAddress, unoCsprojManager);
        vscode.commands.registerCommand("setDisableRoslynGenerators", unoCsprojManager.setDisableRoslynGenerators, unoCsprojManager);
        vscode.commands.registerCommand("setEnableRoslynGenerators", unoCsprojManager.setEnableRoslynGenerators, unoCsprojManager);
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

    private getPath (pattern: string, location?: PathLike): PathLike | undefined {
        // get the workspace directories
        let workspacePath: PathLike;

        if (location === undefined) {
            workspacePath = path.join(vscode.workspace.rootPath!);
        } else {
            workspacePath = location;
        }

        const dirs: string[] = fs.readdirSync(workspacePath);
        let getPath: PathLike;

        // check if the pattern exists
        const dirName = dirs.filter(dirname => dirname.includes(pattern));
        if (dirName.length === 1) {
            getPath = path.join(workspacePath.toString(), dirName[0], `${dirName[0]}.csproj`);
            return getPath;
        }

        return undefined;
    }

    public setHotReloadHostAddress (location?: PathLike): void {
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
                }
            });
        }
    }

    public setEnableRoslynGenerators (): void {
        // get the workspace directories
        const skiaGtkPath: PathLike | undefined = this.getPath(".Skia.Gtk");
        const skiaWasmPath: PathLike | undefined = this.getPath(".Wasm");

        // check if the .Skia.Gtk exists
        if (skiaGtkPath !== undefined) {
            const gtkCsproj = fs.readFileSync(skiaGtkPath, "utf-8");

            xml2js.parseString(gtkCsproj, (err, result): void => {
                if (err === null) {
                    this.setEnableRoslynGeneratorsProperty(result, skiaGtkPath);
                }
            });
        }

        // check if the .Skia.Wasm exists
        if (skiaWasmPath !== undefined) {
            const wasmCsproj = fs.readFileSync(skiaWasmPath, "utf-8");

            xml2js.parseString(wasmCsproj, (err, result): void => {
                if (err === null) {
                    this.setEnableRoslynGeneratorsProperty(result, skiaWasmPath);
                }
            });
        }
    }

    public setDisableRoslynGenerators (location?: PathLike): void {
        // get the workspace directories
        const skiaGtkPath: PathLike | undefined = this.getPath(".Skia.Gtk", location);
        const skiaWasmPath: PathLike | undefined = this.getPath(".Wasm", location);

        // check if the .Skia.Gtk exists
        if (skiaGtkPath !== undefined) {
            const gtkCsproj = fs.readFileSync(skiaGtkPath, "utf-8");

            xml2js.parseString(gtkCsproj, (err, result): void => {
                if (err === null) {
                    this.setDisableRoslynGeneratorsProperty(result, skiaGtkPath);
                }
            });
        }

        // check if the .Skia.Wasm exists
        if (skiaWasmPath !== undefined) {
            const wasmCsproj = fs.readFileSync(skiaWasmPath, "utf-8");

            xml2js.parseString(wasmCsproj, (err, result): void => {
                if (err === null) {
                    this.setDisableRoslynGeneratorsProperty(result, skiaWasmPath);
                }
            });
        }
    }
}
