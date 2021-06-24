#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class IconSource : DependencyObject
	{
		private List<WeakReference<IconElement>> m_createdIconElements;

		protected IconSource()
		{
		}

		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconSource), new PropertyMetadata(null, OnPropertyChanged));

		public IconElement? CreateIconElement()
		{
			var element = CreateIconElementCore();
			if (element != null)
			{
				m_createdIconElements.Add(new WeakReference<IconElement>(element));
			}
			return element;
		}

		internal static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var iconSource = sender as IconSource;
			iconSource?.OnPropertyChanged(args);
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (GetIconElementPropertyCore(args.Property) is { } iconProp)
			{
				m_createdIconElements.RemoveAll(
					weakElement =>
					{
						if (weakElement.TryGetTarget(out var target))
						{
							target.SetValue(iconProp, args.NewValue);
							return false;
						}
						return true;
					});
			}
		}

		internal protected virtual IconElement? CreateIconElementCore() => default;

		internal protected virtual DependencyProperty? GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == ForegroundProperty)
			{
				return IconElement.ForegroundProperty;
			}

			return null;
		}
	}
}
