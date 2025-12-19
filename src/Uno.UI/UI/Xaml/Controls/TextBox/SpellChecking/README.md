# Spell Checking Implementation for Skia TextBox

## Overview

This document outlines the implementation status of spell checking support for TextBox on Skia platforms.

## Current Implementation (Phase 1)

### ✅ Completed Features

1. **Browser Spell Checking for Skia WebAssembly**
   - Modified `BrowserInvisibleTextBoxViewExtension` to pass `IsSpellCheckEnabled` property to the native HTML input element
   - Updated TypeScript layer to respect the `IsSpellCheckEnabled` setting
   - When `IsSpellCheckEnabled="True"`, the browser's native spell checking is now active
   - Sample page added: `TextBox_SpellCheck.xaml` demonstrating the feature

2. **Foundation for Future Spell Checking Services**
   - Created `ISpellCheckService` interface for cross-platform spell checking
   - Implemented `DefaultSpellCheckService` as a no-op default implementation
   - Platform-specific implementations can now be added by implementing this interface

### 🎯 How to Use (WebAssembly)

```xml
<TextBox IsSpellCheckEnabled="True" 
         AcceptsReturn="True"
         TextWrapping="Wrap"
         PlaceholderText="Type some text here..."/>
```

On Skia WebAssembly, the browser will now provide:
- Red wavy underlines for misspelled words
- Right-click context menu with spelling suggestions (browser-native)

## Pending Implementation (Phase 2)

### 📋 Remaining Work

1. **Visual Spell Check Indicators**
   - Implement custom red squiggle/wavy underline rendering in `TextBoxView`
   - Track misspelled word ranges from spell checking service
   - Update visual indicators when text changes

2. **Desktop Spell Checking Integration**
   - Integrate with platform-specific spell checkers:
     - macOS: NSSpellChecker
     - Windows: Windows Spell Check API
     - Linux: Hunspell or other system spell checker
   - Implement platform-specific `ISpellCheckService` implementations

3. **Enhanced Context Menu (Proofing Submenu)**
   - Add "Proofing" submenu to TextBox context menu
   - Show spelling suggestions from spell checking service
   - Add "Ignore" option (session-based)
   - Add "Add to dictionary" option (persistent)
   - Integrate with `TextCommandBarFlyout` when available

4. **Word Boundary Detection**
   - Implement robust word boundary detection
   - Handle multiple languages and special characters
   - Support for hyphenated words and contractions

5. **Performance Optimization**
   - Implement incremental spell checking (check only changed portions)
   - Add debouncing for spell check requests
   - Cache spell check results

## Architecture

```
┌──────────────────────────────────────────┐
│          TextBox (Skia)                  │
├──────────────────────────────────────────┤
│  - IsSpellCheckEnabled property          │
│  - Context menu with proofing (future)   │
└──────────────────────┬───────────────────┘
                       │
         ┌─────────────┴─────────────┐
         │                           │
┌────────▼──────────┐    ┌──────────▼────────────┐
│  TextBoxView      │    │  ISpellCheckService   │
│  (Visual Layer)   │    │  (Spell Checking API) │
├───────────────────┤    ├───────────────────────┤
│ - Red squiggles   │◄───┤ - IsMisspelledAsync   │
│ - Word tracking   │    │ - GetSuggestionsAsync │
└───────────────────┘    │ - AddToDictionary     │
                         │ - IgnoreWord          │
                         └───────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            │                       │                       │
┌───────────▼────────┐  ┌──────────▼──────────┐  ┌────────▼──────────┐
│ BrowserSpellCheck  │  │ MacOSSpellCheck     │  │ WindowsSpellCheck │
│ (WebAssembly)      │  │ (Skia.macOS)        │  │ (Skia.Windows)    │
└────────────────────┘  └─────────────────────┘  └───────────────────┘
```

## Testing

1. **WebAssembly Test**
   - Run `SamplesApp.Skia.WebAssembly.Browser`
   - Navigate to "TextBox" > "TextBox_SpellCheck"
   - Type misspelled words (e.g., "recieve", "teh")
   - Verify browser shows red underlines
   - Right-click to see browser's spell check suggestions

2. **Future Desktop Tests**
   - Test on macOS Skia with system spell checker
   - Test on Windows Skia with Windows Spell Check API
   - Test on Linux Skia with Hunspell

## Related Issues

- Original issue: [Add issue number here]
- Related WinUI documentation: [TextBox.IsSpellCheckEnabled Property](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textbox.isspellcheckenabled)

## Notes

- The current implementation provides basic browser spell checking on WebAssembly
- Full WinUI parity (visual squiggles and proofing menu) requires Phase 2 implementation
- The architecture is designed to be extensible for future platform-specific implementations
