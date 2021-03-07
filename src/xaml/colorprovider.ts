
import * as vscode from "vscode";

function RGBAToHex ({ red, green, blue, alpha }: {
    red: number,
    green: number,
    blue: number,
    alpha: number;
}): string {
    const r = red.toString(16).padStart(2, "0");
    const g = green.toString(16).padStart(2, "0");
    const b = blue.toString(16).padStart(2, "0");
    const a = alpha.toString(16).padStart(2, "0");

    return `#${r}${g}${b}${a === "ff" ? "" : a}`;
}

function hexToRGBA (hex: string): { red: number, green: number, blue: number, alpha: number; } | undefined {
    // Parses "#ffffff" and "#ffffffff"
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})?$/i.exec(hex);
    if (result !== null) {
        return {
            red: parseInt(result[1], 16),
            green: parseInt(result[2], 16),
            blue: parseInt(result[3], 16),
            alpha: 255
        };
    }

    // Parses "#fff" and "#ffff"
    const shorthandResult = /^#?([a-f\d])([a-f\d])([a-f\d])([a-f\d])?$/i.exec(hex);
    if (shorthandResult !== null) {
        return {
            red: parseInt(shorthandResult[1] + shorthandResult[1], 16),
            green: parseInt(shorthandResult[2] + shorthandResult[2], 16),
            blue: parseInt(shorthandResult[3] + shorthandResult[3], 16),
            alpha: shorthandResult[4] === null
                ? 255 : parseInt(shorthandResult[4] + shorthandResult[4], 16)
        };
    }

    return undefined;
}

export default class XamlColorProvider implements vscode.DocumentColorProvider {
    private vscodeColorToHex (vscodeColor: vscode.Color): string {
        const { red, green, blue, alpha } = vscodeColor;

        return RGBAToHex({
            red: Math.floor(red * 255),
            green: Math.floor(green * 255),
            blue: Math.floor(blue * 255),
            alpha: Math.floor(alpha * 255)
        });
    }

    private hexToVscodeColor (hex: string): vscode.Color | undefined {
        const rgba = hexToRGBA(hex);

        if (rgba === null) {
            return undefined;
        }

        return new vscode.Color(
            rgba!.red / 255,
            rgba!.green / 255,
            rgba!.blue / 255,
            rgba!.alpha / 255
        );
    }

    public provideColorPresentations (color: vscode.Color,
        _context: { document: vscode.TextDocument, range: vscode.Range; },
        _cancel: vscode.CancellationToken): vscode.ColorPresentation[] {
        return [
            new vscode.ColorPresentation(this.vscodeColorToHex(color))
        ];
    }

    public provideDocumentColors (document: vscode.TextDocument,
        _cancel: vscode.CancellationToken): any[] {
        const colorRegex = /#([a-f0-9]{3}){1,2}/g;
        const colors: any[] = [];

        for (let lineNo = 0; lineNo < document.lineCount; lineNo++) {
            const text = document.lineAt(lineNo).text;

            while (true) {
                const colorMatch = colorRegex.exec(text);
                if (colorMatch === null) {
                    break;
                }

                const colorHex = colorMatch[0];
                const colorRGB = this.hexToVscodeColor(colorHex);

                colors.push(new vscode.ColorInformation(
                    new vscode.Range(
                        lineNo,
                        colorMatch.index,
                        lineNo,
                        colorRegex.lastIndex
                    ),
                    colorRGB!
                ));
            }
        }

        return colors;
    }
}
