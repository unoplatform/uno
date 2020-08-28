using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class IconSourceElement : IconElement
	{
		public IconSourceElement()
		{
		}

		public IconSource IconSource
		{
			get => (IconSource)GetValue(IconSourceProperty);
			set => SetValue(IconSourceProperty, value);
		}

		public static DependencyProperty IconSourceProperty { get; } =
			DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(IconSource), new PropertyMetadata(null, OnIconSourceChanged));

		private static void OnIconSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var iconSourceElement = (IconSourceElement)dependencyObject;
			if (args.NewValue == null)
			{
				iconSourceElement.RemoveIconElementView(); //TODO:MZ:Verify child is actually removed properly
			}
			else
			{
				switch (args.NewValue)
				{
					case SymbolIconSource symbolIconSource:
						var icon = new FontIcon();
						iconSourceElement.AddIconElementView(icon);
						icon.Glyph = new string((char)symbolIconSource.Symbol, 1); //TODO:MZ:Update icon on changes
						break;
					default:
						throw new InvalidOperationException("Other icon sources are not yet implemented"); //TODO:MZ:Add support for other icon sources
				}
			}
		}
	}
}
