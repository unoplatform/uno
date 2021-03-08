import * as vscode from 'vscode';
import * as path from 'path';

export class CmdOption extends vscode.TreeItem {
    constructor (
        public readonly label: string,
        public ip: string,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public desc: string,
        public readonly command: vscode.Command,
        public readonly icon: string
    ) {
        super(label, collapsibleState);

        this.tooltip = `${this.label}`;
        this.description = `${this.desc}`;
    }

    iconPath = {
        light: path.join(
            __filename,
            '..',
            '..',
            'media',
            this.icon
        ),
        dark: path.join(
            __filename,
            '..',
            '..',
            'media',
            this.icon
        )
    };

    contextValue = 'cmd';
}
