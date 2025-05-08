---
uid: Uno.HotDesign.Properties.AdvancedFlyout.ResponsiveExtensions
---

# Responsive Extensions

Responsive Extensions let you define different values for a property depending on the screen size. This helps you build adaptive UIs that look and behave better on different devices or window sizes.

In the Advanced Flyout, the **Responsive Extension** toggle will only be enabled if the selected property supports responsive values. When toggled on, it allows you to configure different values for various screen sizes using collapsible sections.

For more advanced usage and technical details, see the [Responsive Extensions documentation](xref:Toolkit.Helpers.ResponsiveExtension).

## Setting Values According to the Screen Size

Inside the **Advanced Flyout**, you'll find a toggle called **Responsive Extension Values**. Turning it on enables responsive editing for the selected property.

Once enabled, collapsible sections appear for each screen size category:

- **Narrowest**
- **Narrow**
- **Normal**
- **Wide**
- **Widest**

Each category has its own toggle switch, so you can choose which breakpoints to use. Below the section title, the specific pixel width that activates it is shown to help you understand when that value will apply.

After activating a breakpoint, an editor for that property will appear. You can now enter a value that will only apply when the screen is within that size range. This editor behaves just like the regular one you use for the default value - if it’s a number, you’ll get a text field; if it’s a Brush, you’ll get suggestions, and so on.

<img src="Assets/properties-flyout-responsive-extensions.gif" height="600" alt="How to use Responsive Extensions on the Advanced Flyout" />

## Resetting Values

To remove a value set for a specific breakpoint, click the **trash icon** next to the editor. This will clear the responsive override and fall back to the default property value or another applicable one.

## Next Steps

- **[Different Editors](xref:Uno.HotDesign.Properties.Editors)**

  The Properties panel automatically selects the editor best suited for each property’s data type. Visit this page to explore all available editor types and when to use them.

- **[Advanced Flyout Editor](xref:Uno.HotDesign.Properties.AdvancedFlyout)**

  Use the **Advanced Flyout** to choose how a property value is provided: enter a literal **Value**, set up a **Binding**, reference a **Resource**, or apply **Responsive Extensions** for adaptive layouts.

- **[Template Editor](xref:Uno.HotDesign.Properties.TemplateEditor)**

  The **Template Editor** provides a visual canvas for creating and customizing control templates, enabling you to design complex UI structures without hand-coding XAML.

- **[Counter App Tutorial](xref:Uno.HotDesign.GetStarted.CounterTutorial)**

  A hands-on walkthrough for building the [Counter App](xref:Uno.Workshop.Counter) using **Hot Design**, showcasing its features and workflow in action.
