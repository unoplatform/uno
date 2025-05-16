---
uid: Uno.HotDesign.Canvas
---

# Canvas

The **Canvas** is the central design surface where you build and interact with your running app's user interface. It reflects the live visual output of your XAML page and allows you to select, move, edit, and organize controls directly. Actions you take on the Canvas are reflected in real time, making it the most visual and interactive part of **Hot Design**.

Whether you're adjusting layout, editing a `UserControl`, or testing structure with live feedback, the Canvas helps you work faster and more intuitively.

## Zoom and Scroll

### Zoom

To zoom in or out of the **Canvas**:

- **Mouse**: Hold `Ctrl` (or `Cmd` on macOS) and scroll with your mouse wheel.
- **Keyboard**: Press `Ctrl +` / `Ctrl -` (or `Cmd +` / `Cmd -` on macOS) to zoom in or out.

<img src="Assets/canvas-zoom.gif" height="600" alt="How to zoom in and out of the Canvas" />

### Scroll

To scroll the **Canvas** area:

- Scroll vertically using your mouse wheel.
- Scroll horizontally by holding `Shift` while using the mouse wheel.

<img src="Assets/canvas-scroll.gif" height="600" alt="How to scroll the Canvas" />

## Select Elements on the Canvas

To select a control or layout element, click it directly on the **Canvas**. The selected element is highlighted with a blue border, known as a **visual adorner**.

![Canvas Selection](Assets/canvas-selection.png)

### Select Multiple Elements

Hold `Ctrl` (or `Cmd` on macOS) and click additional elements to select them together. This is useful for applying shared properties or moving groups of elements.

![Canvas Multi-selection](Assets/canvas-multi-selection.png)

## Visual Adorner Details

When an element is selected, a blue border (called a *visual adorner*) appears around it. It includes:

- The control’s name (e.g., `Button`, `TextBlock`)
- The control’s size in pixels (width × height)

![Visual Adorner on Canvas](Assets/canvas-visual-adorner.png)

## Rearranging Elements with Drag and Drop

You can reposition controls directly in the layout:

1. Click and hold an element.
2. Drag it to a new location on the **Canvas**.
3. Release to drop it.

If the layout allows it, the element will be moved and the structure updated accordingly.

<img src="Assets/canvas-drag-drop.gif" height="600" alt="How to drag and drop on the Canvas" />

## Wrap an Element with a Parent Container

To surround an existing element with a new layout container:

1. Right-click the element.
2. Choose **Add parent**.
3. Select a parent control such as `Grid` or `StackPanel`.

The new parent will be added to the visual tree and reflected on the **Canvas**.

![Canvas Add Parent](Assets/canvas-add-parent.png)

## Select an Element’s Parent

If you're trying to select a layout container of a specific control:

1. Right-click the child element.
2. Choose **Select parent** from the context menu.

This selects the parent element on both the **Canvas** and in the **Elements** panel.

<img src="Assets/canvas-select-parent.gif" height="450" alt="How to select a parent element on the Canvas" />

## Delete an Element

To remove a control from the **Canvas**:

1. Right-click the element.
2. Choose **Delete [ControlName]** from the menu (e.g., **Delete Button**).

The element will be removed from both the **Canvas** and the **Elements** panel.

<img src="Assets/canvas-delete-element.gif" height="450" alt="How to delete an element from the Canvas" />

## Edit a UserControl from the Canvas

A `UserControl` is a reusable component that encapsulates a set of UI elements and their associated behavior. It's commonly used to organize parts of your interface into self-contained, maintainable units that can be reused across different parts of your application.

If your layout includes a `UserControl`, you can open it directly from the **Canvas** for editing:

1. Right-click the `UserControl`.
2. Choose **Edit [UserControlName]**.

This opens the editor for the `UserControl`, allowing you to modify its internal structure or layout. To return to your previous page edition, click the `../` back icon in the top-left corner of the interface or the **Elements** panel.

<img src="Assets/canvas-edit-user-control.gif" height="600" alt="How to edit a UserControl from the canvas" />

## Edit Flyouts from the Canvas

You can edit flyouts in Hot Design in two main ways.

### Using the Property Window

You can locate the `Flyout` property in the **Property Window** and edit it using the **Complex Type Editor**. To learn more about how the Complex Type Editor works, refer to the [Complex Type Editor documentation](xref:Uno.HotDesign.Properties.Editors#complex-types).

### Using the Flyout Editor

The **Flyout Editor** allows you to edit the contents of the flyout directly. You can add, remove, or modify individual elements inside the flyout.
To open the Flyout Editor, follow these steps:

1. In the **Toolbar**, click the **More Options** button (three dots).
2. Hover over the **Window** menu and uncheck the option **Show tools in-app**.
   This will move the Toolbox, Elements window, and Property window to external windows, leaving only the Canvas in the main window.
3. Enable **Interactive Mode** by clicking the **Play** button in the toolbar.
4. Perform the action that opens the flyout (e.g., click the button that has a flyout).
5. With the flyout still opened, disable **Interactive Mode** by clicking the **Pause** button in the Toolbar.
   This will activate the **Flyout Editor**.

Now you can freely make any changes to the flyout.

<img src="Assets/canvas-edit-flyouts.gif" alt="How to edit flyouts from the canvas" />

To exit the **Flyout Editor**, you can:

- Click the **Enter Interactive Mode** button in the Toolbar (Play icon)
- Or click the **Back** icon in the top-left corner of the **Canvas** or **Tree window**

  ![Leave Flyout Editor via Canvas](Assets/canvas-leave-flyout-editor.png)

  ![Leave Flyout Editor via Tree window](Assets/tree-leave-flyout-editor.png)

> [!NOTE]
> In external tool windows, zoom features are disabled. This means you won’t be able to adjust form factor, zoom, or scroll while editing a Flyout.
> [!TIP]
> **UserControl** and **DataTemplate** editors are fully supported inside the Flyout Editor, so you can edit them directly within the Flyout context.
> [!IMPORTANT]
> Editing *nested flyouts* is not supported at the moment.

## Layout Separators

When working with layout containers like `Grid` or `StackPanel`, pink dashed lines appear to show row and column boundaries. These **visual separators** help you understand the layout structure and spacing.

![Canvas Layout Separation](Assets/canvas-layout-separator.png)

## Helpful Notifications

While designing on the Canvas, notifications may appear:

- When changes are applied, a loading indicator may display briefly.
- If an error occurs (e.g., invalid XAML), an error notification appears in the bottom-right corner.

These notifications help you track changes and issues in real time.

## Next Step

- [Properties](xref:Uno.HotDesign.Properties)
