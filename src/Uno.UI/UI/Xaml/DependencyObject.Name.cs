#nullable enable

namespace Microsoft.UI.Xaml;

public partial class DependencyObject
{
	// WinUI's DependencyObject_Name: holds x:Name for any DependencyObject, exposed publicly as FrameworkElement.NameProperty.
	internal static DependencyProperty NameProperty { get; } = DependencyProperty.Register(
		"Name",
		typeof(string),
		typeof(DependencyObject),
		new FrameworkPropertyMetadata(
			defaultValue: "",
			options: FrameworkPropertyMetadataOptions.None,
			propertyChangedCallback: static (instance, args) => (instance as FrameworkElement)?.OnNameChanged((string)args.OldValue, (string)args.NewValue),
			// WinUI stores the name as an HSTRING, so null reads back as "".
			coerceValueCallback: static (_, baseValue, _) => baseValue ?? "",
			backingFieldUpdateCallback: static (instance, newValue) =>
			{
				if (instance is FrameworkElement frameworkElement)
				{
					frameworkElement.NameCache = (string)newValue;
				}
			}));
}
