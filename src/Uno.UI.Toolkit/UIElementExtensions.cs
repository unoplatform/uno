using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

#if HAS_UNO
using Uno.Extensions;
using Uno.Foundation.Logging;
#endif

#if NETCOREAPP && !HAS_UNO
using Microsoft.UI;
#endif

#if __IOS__ || __MACOS__
using CoreGraphics;
#endif

#if NET6_0_OR_GREATER && (__IOS__ || __MACOS__)
using ObjCRuntime;
#endif

#if __SKIA__
using Uno.UI.Composition.Composition;
#endif

namespace Uno.UI.Toolkit
{
#if !NET6_0_OR_GREATER // Moved to the linker definition file
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
#endif
	public static class UIElementExtensions
	{
#region Elevation

		public static void SetElevation(this UIElement element, double elevation)
		{
			element.SetValue(ElevationProperty, elevation);
		}

		public static double GetElevation(this UIElement element)
		{
			return (double)element.GetValue(ElevationProperty);
		}

		public static DependencyProperty ElevationProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Elevation",
				typeof(double),
				typeof(UIElementExtensions),
				new PropertyMetadata(0, OnElevationChanged)
			);

		private static readonly Color ElevationColor = Color.FromArgb(64, 0, 0, 0);

		private static void OnElevationChanged(DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue is double elevation)
			{
				Uno.UI.Helpers.ElevationHelper.SetElevation(dependencyObject, elevation, ElevationColor);
			}
		}

#endregion

		internal static Thickness GetPadding(this UIElement uiElement)
		{
			if (uiElement is FrameworkElement fe && fe.TryGetPadding(out var padding))
			{
				return padding;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
		}

		internal static bool SetPadding(this UIElement uiElement, Thickness padding)
		{
			if (uiElement is FrameworkElement fe && fe.TrySetPadding(padding))
			{
				return true;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			if (property != null)
			{
				uiElement.SetValue(property, padding);
				return true;
			}

			return false;
		}

		internal static bool TryGetPadding(this FrameworkElement frameworkElement, out Thickness padding)
		{
			switch (frameworkElement)
			{
				case Grid g:
					padding = g.Padding;
					return true;

				case StackPanel sp:
					padding = sp.Padding;
					return true;

				case Control c:
					padding = c.Padding;
					return true;

				case ContentPresenter cp:
					padding = cp.Padding;
					return true;

				case Border b:
					padding = b.Padding;
					return true;
			}

			padding = default;
			return false;
		}

		internal static bool TrySetPadding(this FrameworkElement frameworkElement, Thickness padding)
		{
			switch (frameworkElement)
			{
				case Grid g:
					g.Padding = padding;
					return true;

				case StackPanel sp:
					sp.Padding = padding;
					return true;

				case Control c:
					c.Padding = padding;
					return true;

				case ContentPresenter cp:
					cp.Padding = padding;
					return true;

				case Border b:
					b.Padding = padding;
					return true;
			}

			return false;
		}

		private static Dictionary<(Type type, string property), DependencyProperty> _dependencyPropertyReflectionCache;

		internal static DependencyProperty FindDependencyPropertyUsingReflection<TProperty>(this UIElement uiElement, string propertyName)
		{
			var type = uiElement.GetType();
			var propertyType = typeof(TProperty);
			var key = (ownerType: type, propertyName);

			_dependencyPropertyReflectionCache ??= new Dictionary<(Type, string), DependencyProperty>(2);

			if (_dependencyPropertyReflectionCache.TryGetValue(key, out var property))
			{
				return property;
			}

			property =
				type
					.GetTypeInfo()
					.GetDeclaredProperty(propertyName)
					?.GetValue(null) as DependencyProperty
				?? type
					.GetTypeInfo()
					.GetDeclaredField(propertyName)
					?.GetValue(null) as DependencyProperty;

			if (property == null)
			{
#if HAS_UNO
				uiElement.Log().Warn($"The {propertyName} dependency property does not exist on {type}");
#endif
			}
#if HAS_UNO
			else if (property.Type != propertyType)
			{
				uiElement.Log().Warn($"The {propertyName} dependency property {type} is not of the {propertyType} Type.");
				property = null;
			}
#endif

			_dependencyPropertyReflectionCache[key] = property;

			return property;
		}
	}
}
