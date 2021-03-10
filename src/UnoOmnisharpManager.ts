import * as vscode from 'vscode';
import * as fs from 'fs-extra';
import * as path from 'path';
import { PathLike } from 'node:fs';
import replace from 'replace-in-file';

export class UnoOmnisharpManager {
    public context: vscode.ExtensionContext;

    public static Register (context: vscode.ExtensionContext): void {
        const unoOmnisharpManager = new UnoOmnisharpManager();
        unoOmnisharpManager.context = context;
    }

    public async createSkiaGtkConfiguration (projectName: string, projectLocation: PathLike): Promise<void> {
        // get the .vscode template
        const vscodeTemplate = path.join(this.context.extensionPath, "templates", "gtk", ".vscode");
        // move to the new project workspace
        fs.copySync(vscodeTemplate, path.join(projectLocation.toString(), ".vscode"));
        // replace pattern
        const options = {
            files: [
                projectLocation.toString() + path.sep + ".vscode" + path.sep + "launch.json",
                projectLocation.toString() + path.sep + ".vscode" + path.sep + "tasks.json"
            ],
            from: [
                // eslint-disable-next-line no-template-curly-in-string
                new RegExp("[$]{projectName}", "g")
            ],
            to: [
                projectName
            ]
        };

        await replace(options);
    }

    public createWasmConfiguration (): void { }

    public createSkiaGtkWasmConfiguration (): void { }
}
