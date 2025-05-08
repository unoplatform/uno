---
uid: Uno.HotDesign.Toolbox
---

# Toolbox

The **Toolbox** panel lets you easily find and add controls and layout elements to build your app's UI. Located in the top-left corner of the Hot Design interface, it provides a searchable, categorized list of available controls that you can drag into the **Canvas** or the visual tree with the **Elements** panel.

Whether you're adding a button, creating a layout with a `Grid`, or inserting a custom control, the Toolbox helps you quickly locate and add what you need.

## Find the Right Control

At the top of the Toolbox, there’s a search box. As you type, the list instantly updates to show matching controls and elements. This helps you locate what you need without scrolling through the full list.

![Search](Assets/toolbox-search.png)

## Browse Controls by Category

When the search box is empty, the Toolbox displays all available controls organized into collapsible categories. These include standard WinUI 3 controls, layout containers, collection views, and any custom or third-party controls available in your project.

- **Common**: Frequently used WinUI 3 controls such as `Button`, `TextBox`, `CheckBox`, and `TextBlock`.
- **Layout**: Layout elements (called panels) like `Grid`, `StackPanel`, and `Border` are used to structure the visual hierarchy of your XAML UI.
- **Collections**: Data-bound controls like `ListView`, `GridView`, and `ItemsRepeater`, typically used to present lists or repeated content.
- **Project**: Custom user controls and components defined in your current project or solution.
- **Custom**: Third-party controls, grouped by their assembly name.
- **All**: A complete, ungrouped list of all available controls and elements in the Toolbox.

Click the arrow beside a category name to expand or collapse its contents.

<img src="Assets/toolbox-expand-section.gif" height="400" alt="How to expand and collapse sections in the Toolbox." />

## Add a Control to the Canvas

To insert a control into your layout in the interactive **Canvas**:

1. Drag a control from the **Toolbox** panel.
2. Drop it onto the **Canvas**, inside the element where you want it to appear.

<img src="Assets/toolbox-add-to-canvas.gif" height="600" alt="How to drag and drop an element from the Toolbox panel to the Canvas." />

## Add a Control to the Visual Tree

To insert a control into the **Elements** panel:

1. Drag a control from the **Toolbox** panel.
2. Drop it into the desired parent node in the visual tree inside the **Elements** panel.

<img src="Assets/toolbox-add-to-tree.gif" height="600" alt="How to drag and drop a control from the Toolbox panel into the Elements panel." />

## Insert a Control Using Double-Click

To quickly insert a control:

1. Select a parent element on the **Canvas** or in the visual tree in the **Elements** panel.
2. Double-click the wanted new control in the **Toolbox**. It will be added as a child of the previously selected element.

<img src="Assets/toolbox-add-to-tree-double-click.gif" height="600" alt="How to double-click a control in the Toolbox to add it as a child of the parent selected element." />

## Next Step

- [Elements](xref:Uno.HotDesign.Elements)
