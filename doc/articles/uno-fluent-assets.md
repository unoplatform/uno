---
title: Uno Fluent UI assets
author: alextrepa
description: adding new multiplateform font
---

# Uno Fluent UI assets
Uno has a new multiplateform font. The new font must be added to app build before the change.

> [!IMPORTANT] \
This is a breaking change on most platform the font needs to be changed or most control will display incorrectly.

## Font files
The font is in this repository https://github.com/unoplatform/uno.fonts

## Changes

| Device family | Description   |
| -- | -- |
| iOS & macOS | Once Uno has been updated, it will start looking for a font named symbols (Not necessarily the name of the file). For this font to be available it needs to be in the resource folder.  ![image](Assets/font-ios.png) ![image](Assets/font-macos.png)  The `info.plist` should also be updated for both platforms, replacing winjs-symbols.ttf by uno-fluentui-assets.ttf|
| Android | Once Uno has been updated, it will start looking for a font file named uno-fluentui-assets.ttf in its assets folder:  ![image](Assets/font-android.png) |
| WebAssembly | A wasm build won't break after the update but to access the new symbols the file Font.css should be chnaged. The font is passed as a base64 file. ![image](Assets/font-wasm.png) |

## Limitations
On iOS and macOS the indeterminate state for a check box is not the right color.


## Related Topics
- [#3011](https://github.com/unoplatform/uno/issues/3011)
- [967](https://github.com/unoplatform/uno/issues/967)
