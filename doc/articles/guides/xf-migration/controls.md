---
uid: Uno.XamarinFormsMigration.Controls
---

# Controls

Your apps commonly leverage the framework's native controls to create a third-party view of the same building blocks. This pattern improves coherency, allowing for easy reuse of intricate layouts between multiple pages like a calendar, charts, or the elements of a data list. Likewise, third-party controls may instead use a graphics library to render drawings from scratch and uniquely respond to behaviors. While this category of **custom-drawn** controls isn't as common compared to its native alternative, it is often desirable when your team is striving for greater visual fidelity than included controls can provide.

Both types of third-party controls enable your team to achieve a coherent app experience without the need to define complex UI elements from scratch multiple times. In fact, this pattern of defining a control's appearance in XAML and its behavior in C# is conceptually identical between Xamarin.Forms and Uno. The types used to do so are different. For that reason, some third-party controls defined in your Xamarin.Forms project are not directly compatible with Uno.

We provide the steps to help you smoothly migrate both types of controls to Uno Platform.

## Native controls

Migrating custom controls generally involves several key steps, depending on the features in use. Updates to XAML and C# code-behind are usually needed to match the equivalent types in Uno Platform. Certain names of properties, elements, and attributes may need to be adjusted to match WinUI. Notably, scenarios that leverage template overrides to alter a custom control's default appearance need only minimal changes which won't disrupt how you define its XAML tree.

## Custom-drawn controls

It is common to use **SkiaSharp**, a cross-platform .NET API for 2D graphics, to create custom-drawn controls that work on **Uno Platform**.

See the [tutorial]() which introduces **Microcharts**, an open-source charting library that uses SkiaSharp to draw various types of charts. It demonstrates how to add **Uno support** to Microcharts by creating a new project and linking the existing code.