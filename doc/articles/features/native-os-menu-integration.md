---
uid: Uno.Features.NativeOSMenuIntegration
---

# Native OS Menu Integration API

> [!IMPORTANT]
> This document is a design specification and proposal for community and team input. The API described here is not yet implemented and may change based on feedback.

## Overview

The Native OS Menu Integration API provides a unified cross-platform abstraction for integrating with native operating system menu bars and application menus. This allows Uno Platform applications to seamlessly expose standard application menus (such as "File", "Edit", "Help") that integrate with platform-specific conventions on macOS (NSMenu), iPadOS menu bar, Linux desktop environments, and Windows.

## Motivation

Developers building cross-platform applications need their apps to feel native on each operating system. Application and menu bar integration is an essential part of user expectations:

- **macOS**: Users expect standard menu bar items accessible via the global menu bar at the top of the screen
- **iPadOS**: Stage Manager and external display support brought menu bar support to iPad applications
- **Linux**: Desktop environments like GNOME and KDE support global menus and application indicators
- **Windows**: Traditional application menus integrated with the window chrome

Currently, Uno Platform provides `MenuBar` and `MenuFlyout` controls that render in-app menus following WinUI patterns. However, these do not integrate with native OS menu systems. This proposal addresses that gap.

## Goals

1. **Native Integration**: Provide an API that maps to native menu systems (macOS NSMenu, iPadOS UIMenu, Linux DBusMenu, Windows menus)
2. **Cross-Platform Consistency**: Offer a unified programming model across all platforms
3. **WinUI Compatibility**: Design an API that complements and can work alongside existing WinUI `MenuBar` control
4. **Extensibility**: Allow the API to be extended for future platforms and capabilities
5. **Dynamic Menus**: Support dynamic modification of menu items at runtime
6. **Command Integration**: Integrate with the command pattern (`ICommand`) for menu item actions
7. **Accessibility**: Ensure menu items are accessible and work with platform assistive technologies

## Non-Goals

- Replacing the existing `MenuBar` WinUI control (the new API complements it)
- Supporting tray/notification area icons (this can be a separate feature)
- Custom menu rendering (the focus is on native menus)

## Proposed API Design

### Core Types

```csharp
namespace Uno.UI.NativeMenu
{
    /// <summary>
    /// Represents the application's native menu bar.
    /// </summary>
    public sealed class NativeMenuBar
    {
        /// <summary>
        /// Gets the singleton instance of the native menu bar for the application.
        /// </summary>
        public static NativeMenuBar Instance { get; }

        /// <summary>
        /// Gets the collection of top-level menu items.
        /// </summary>
        public IList<NativeMenuItem> Items { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the native menu bar is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether native menu integration is supported on the current platform.
        /// </summary>
        public static bool IsSupported { get; }

        /// <summary>
        /// Applies the current menu structure to the native OS menu system.
        /// </summary>
        public void Apply();
    }

    /// <summary>
    /// Base class for items that can appear in a native menu.
    /// </summary>
    public abstract class NativeMenuItemBase
    {
        /// <summary>
        /// Gets or sets whether this menu item is visible.
        /// </summary>
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// Represents a menu item that can have child items (submenu).
    /// </summary>
    public sealed class NativeMenuItem : NativeMenuItemBase
    {
        /// <summary>
        /// Gets or sets the text displayed for this menu item.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the icon for this menu item.
        /// </summary>
        public IconSource Icon { get; set; }

        /// <summary>
        /// Gets or sets the command to execute when this menu item is invoked.
        /// </summary>
        public ICommand Command { get; set; }

        /// <summary>
        /// Gets or sets the command parameter.
        /// </summary>
        public object CommandParameter { get; set; }

        /// <summary>
        /// Gets or sets whether this menu item is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the keyboard accelerator for this menu item.
        /// </summary>
        public KeyboardAccelerator KeyboardAccelerator { get; set; }

        /// <summary>
        /// Gets the collection of child menu items (submenu).
        /// </summary>
        public IList<NativeMenuItemBase> Items { get; }

        /// <summary>
        /// Occurs when the menu item is invoked.
        /// </summary>
        public event EventHandler<NativeMenuItemInvokedEventArgs> Invoked;
    }

    /// <summary>
    /// Represents a separator line in a native menu.
    /// </summary>
    public sealed class NativeMenuSeparator : NativeMenuItemBase
    {
    }

    /// <summary>
    /// Represents a toggleable (checkable) menu item.
    /// </summary>
    public sealed class NativeMenuToggleItem : NativeMenuItemBase
    {
        /// <summary>
        /// Gets or sets the text displayed for this menu item.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets whether this toggle item is checked.
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// Gets or sets the command to execute when this menu item is invoked.
        /// </summary>
        public ICommand Command { get; set; }

        /// <summary>
        /// Gets or sets the command parameter.
        /// </summary>
        public object CommandParameter { get; set; }

        /// <summary>
        /// Gets or sets whether this menu item is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the keyboard accelerator for this menu item.
        /// </summary>
        public KeyboardAccelerator KeyboardAccelerator { get; set; }

        /// <summary>
        /// Occurs when the menu item is invoked.
        /// </summary>
        public event EventHandler<NativeMenuItemInvokedEventArgs> Invoked;
    }

    /// <summary>
    /// Event arguments for when a native menu item is invoked.
    /// </summary>
    public sealed class NativeMenuItemInvokedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the menu item that was invoked.
        /// </summary>
        public NativeMenuItemBase Item { get; }
    }
}
```

### XAML Support

The API should support XAML declaration for ease of use:

```xml
xmlns:native="using:Uno.UI.NativeMenu"

<Page.Resources>
    <native:NativeMenuBar x:Key="AppMenu">
        <native:NativeMenuItem Text="File">
            <native:NativeMenuItem Text="New" 
                                   Command="{Binding NewCommand}"
                                   KeyboardAccelerator="Ctrl+N" />
            <native:NativeMenuItem Text="Open..." 
                                   Command="{Binding OpenCommand}"
                                   KeyboardAccelerator="Ctrl+O" />
            <native:NativeMenuSeparator />
            <native:NativeMenuItem Text="Save" 
                                   Command="{Binding SaveCommand}"
                                   KeyboardAccelerator="Ctrl+S" />
            <native:NativeMenuItem Text="Save As..." 
                                   Command="{Binding SaveAsCommand}"
                                   KeyboardAccelerator="Ctrl+Shift+S" />
            <native:NativeMenuSeparator />
            <native:NativeMenuItem Text="Exit" 
                                   Command="{Binding ExitCommand}" />
        </native:NativeMenuItem>
        
        <native:NativeMenuItem Text="Edit">
            <native:NativeMenuItem Text="Undo" 
                                   Command="{Binding UndoCommand}"
                                   KeyboardAccelerator="Ctrl+Z" />
            <native:NativeMenuItem Text="Redo" 
                                   Command="{Binding RedoCommand}"
                                   KeyboardAccelerator="Ctrl+Y" />
            <native:NativeMenuSeparator />
            <native:NativeMenuItem Text="Cut" 
                                   Command="{Binding CutCommand}"
                                   KeyboardAccelerator="Ctrl+X" />
            <native:NativeMenuItem Text="Copy" 
                                   Command="{Binding CopyCommand}"
                                   KeyboardAccelerator="Ctrl+C" />
            <native:NativeMenuItem Text="Paste" 
                                   Command="{Binding PasteCommand}"
                                   KeyboardAccelerator="Ctrl+V" />
        </native:NativeMenuItem>
        
        <native:NativeMenuItem Text="View">
            <native:NativeMenuToggleItem Text="Show Toolbar" 
                                         IsChecked="{Binding IsToolbarVisible}" />
            <native:NativeMenuToggleItem Text="Show Status Bar" 
                                         IsChecked="{Binding IsStatusBarVisible}" />
        </native:NativeMenuItem>
        
        <native:NativeMenuItem Text="Help">
            <native:NativeMenuItem Text="Documentation" 
                                   Command="{Binding ShowDocumentationCommand}" />
            <native:NativeMenuSeparator />
            <native:NativeMenuItem Text="About" 
                                   Command="{Binding ShowAboutCommand}" />
        </native:NativeMenuItem>
    </native:NativeMenuBar>
</Page.Resources>
```

### Code-Behind Usage

```csharp
// Check if native menus are supported
if (NativeMenuBar.IsSupported)
{
    var menuBar = NativeMenuBar.Instance;
    
    // Create File menu
    var fileMenu = new NativeMenuItem { Text = "File" };
    fileMenu.Items.Add(new NativeMenuItem 
    { 
        Text = "New", 
        Command = NewCommand,
        KeyboardAccelerator = new KeyboardAccelerator 
        { 
            Key = VirtualKey.N, 
            Modifiers = VirtualKeyModifiers.Control 
        }
    });
    fileMenu.Items.Add(new NativeMenuItem { Text = "Open...", Command = OpenCommand });
    fileMenu.Items.Add(new NativeMenuSeparator());
    fileMenu.Items.Add(new NativeMenuItem { Text = "Exit", Command = ExitCommand });
    
    menuBar.Items.Add(fileMenu);
    
    // Apply the menu to the native OS
    menuBar.Apply();
}
```

## Platform Implementation Details

### macOS (NSMenu)

On macOS, the API maps to `NSMenu` and `NSMenuItem`:

- `NativeMenuBar` → Main application `NSMenu`
- `NativeMenuItem` → `NSMenuItem` with optional submenu
- `NativeMenuSeparator` → `NSMenuItem.separator()`
- `NativeMenuToggleItem` → `NSMenuItem` with `state` property
- Keyboard accelerators → `NSMenuItem.keyEquivalent` and `keyEquivalentModifierMask`

macOS-specific considerations:
- The "App" menu (with Quit, Preferences, etc.) follows Apple Human Interface Guidelines
- Standard menu items like "Quit" should use `terminateApp:` selector
- Consider supporting the Services submenu

### iPadOS (UIMenu)

On iPadOS 13+, the API maps to `UIMenu` and `UIAction`/`UICommand`:

- `NativeMenuBar` → `UIMenuBuilder` modifications
- `NativeMenuItem` → `UIMenu` (for submenus) or `UIAction`/`UICommand`
- `NativeMenuSeparator` → `UIMenu.Options.displayInline`
- `NativeMenuToggleItem` → `UIAction` with `state` property
- Keyboard accelerators → `UIKeyCommand`

iPadOS-specific considerations:
- Menu bar support requires iOS 13+ and appropriate hardware (external keyboard, Stage Manager)
- Need to implement `buildMenu(with:)` in the app delegate

### Linux (D-Bus Menu)

On Linux, integration depends on the desktop environment:

- **GNOME/GTK**: GMenu/GAction via D-Bus
- **KDE/Qt**: DBusMenu protocol
- **Ubuntu Unity**: AppIndicator with DBusMenu

Linux-specific considerations:
- D-Bus integration for global menu support
- Fallback to in-window menu if global menus are not supported
- Support for the freedesktop.org application menu specification

### Windows

On Windows, native menus can integrate with:

- Traditional Win32 menus (`CreateMenu`, `HMENU`)
- WinUI 3 `MenuBar` (already supported)

Windows-specific considerations:
- Most WinUI apps use in-app menus; native Win32 menus are optional
- Consider taskbar jump list integration as a related feature

## Platform Support Matrix

| Feature | macOS | iPadOS | Linux (X11) | Linux (FB) | Windows | WebAssembly |
|---------|-------|--------|-------------|------------|---------|-------------|
| Basic menu items | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Submenus | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Separators | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Toggle items | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Icons | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Keyboard accelerators | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |
| Dynamic updates | ✔ | ✔ | ✔ | ✖ | ✔ | ✖ |

**Notes:**
- Linux Framebuffer has no window manager integration
- WebAssembly runs in browsers without native OS menu access
- On unsupported platforms, `NativeMenuBar.IsSupported` returns `false`

## Integration with Existing WinUI MenuBar

The native menu API is designed to complement, not replace, the existing WinUI `MenuBar` control. Developers can choose to:

1. Use only WinUI `MenuBar` (in-app menus, cross-platform rendering)
2. Use only native OS menus (native look and feel)
3. Use both (show native menus on supported platforms, fall back to WinUI `MenuBar`)

### Recommended Pattern

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        
        // Try to use native menus, show in-app MenuBar as fallback
        if (NativeMenuBar.IsSupported)
        {
            SetupNativeMenu();
            InAppMenuBar.Visibility = Visibility.Collapsed;
        }
        else
        {
            InAppMenuBar.Visibility = Visibility.Visible;
        }
    }
}
```

## Extensibility Considerations

The API is designed for future extensibility:

1. **Additional Item Types**: Radio button groups, custom items
2. **Context Menus**: Native context menu integration
3. **System Tray**: Notification area icons with menus
4. **Recent Files**: Platform-specific recent file menus
5. **Services Menu**: macOS Services integration
6. **Touch Bar**: macOS Touch Bar integration

## Accessibility

The native menu API inherits accessibility from each platform's native menu system:

- Screen reader support via native accessibility APIs
- Keyboard navigation built into native menus
- High contrast mode support on Windows
- VoiceOver support on macOS and iPadOS

## Open Questions

1. **Should there be an automatic MenuBar-to-NativeMenu converter?** This would allow developers to write one `MenuBar` definition and have it automatically map to native menus.

2. **How should platform-specific menu items (like macOS "Quit" or "Preferences") be handled?** Should they be auto-generated or explicitly defined?

3. **Should the API support menu validation (enabling/disabling based on app state)?** The current design uses `ICommand.CanExecute`, but platform-specific validation patterns exist.

4. **What about localization of standard menu items?** Should common items like "File", "Edit", "Help" be auto-localized?

5. **How should the API handle platform differences in keyboard modifier keys?** (Ctrl vs Cmd)

## References

- [Apple NSMenu Documentation](https://developer.apple.com/documentation/appkit/nsmenu)
- [Apple UIMenu Documentation (iPadOS)](https://developer.apple.com/documentation/uikit/adding-menus-and-shortcuts-to-the-menu-bar-and-user-interface)
- [D-Bus Menu Specification](https://github.com/AyatanaIndicators/libdbusmenu)
- [freedesktop.org Desktop Menu Specification](https://specifications.freedesktop.org/menu-spec/menu-spec-latest.html)
- [WinUI MenuBar](https://learn.microsoft.com/windows/winui/api/microsoft.ui.xaml.controls.menubar)

## Related Issues

- [GitHub Issue #20466: Cross-platform Native OS Menu Integration API](https://github.com/unoplatform/uno/issues/20466)

## Feedback

This is a proposal for community and team input. Please provide feedback on:

- API design and naming
- Platform-specific requirements
- Use cases not covered
- Implementation priorities

Discussion can take place on the [GitHub Issue](https://github.com/unoplatform/uno/issues/20466) or [Uno Platform Discord](https://platform.uno/discord).
