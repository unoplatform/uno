**GitHub Issue:** closes #16944

## PR Type:

✨ Feature

## What changed? 🚀

Added a focused manual SamplesApp page that picks a disposable `.txt` file with `FileOpenPicker`, replaces its contents with `FileIO.WriteTextAsync`, and instructs the tester to verify the change in the original source location.

## Validation

- `dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/Windows_Storage/Pickers/FileOpenPicker_FileIOWrite.xaml`
- `git diff --check`
- Focused SamplesApp build attempted for `net9.0`; blocked by the existing `Uno.WinUI.SpellChecking` project requiring C# `14.0`, unsupported by installed SDK `9.0.316`.

## PR Checklist ✅

- [x] 🧪 Added a manual test sample
- [ ] 📚 Docs have been added/updated (not applicable; instructions are in the sample)
- [ ] 🖼️ Validated PR `Screenshots Compare Test Run` results (manual picker interaction required)
- [x] ❗ Contains **NO** breaking changes
- [ ] 👀 Reviewed 2 other open pull requests
