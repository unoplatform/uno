#nullable enable

namespace Microsoft.UI.Xaml;

public partial class DependencyObject
{
	// WinUI's DependencyObject_Name: holds x:Name for any DependencyObject, exposed publicly as FrameworkElement.NameProperty.
	internal static DependencyProperty NameProperty => NamePropertyHolder.Property;

	// Registered on first use rather than with DependencyObject's statics: FrameworkElement triggers it among its own
	// properties, so the name's storage shares a details bucket with theirs.
	private static class NamePropertyHolder
	{
		internal static readonly DependencyProperty Property = DependencyProperty.Register(
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
}
