#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// On Uno targets these helpers come from Uno.UI itself; this copy only serves the WinAppSDK build.
#if !HAS_UNO
namespace Uno.UI.Extensions;

internal static partial class UIElementExtensions
{
	internal static Thickness GetPadding(this UIElement uiElement)
	{
		if (uiElement is FrameworkElement fe && fe.TryGetPadding(out var padding))
		{
			return padding;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection("PaddingProperty");
		return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
	}

	internal static bool SetPadding(this UIElement uiElement, Thickness padding)
	{
		if (uiElement is FrameworkElement fe && fe.TrySetPadding(padding))
		{
			return true;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection("PaddingProperty");
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

	private static Dictionary<(Type type, string property), DependencyProperty?>? _dependencyPropertyReflectionCache;

	internal static DependencyProperty? FindDependencyPropertyUsingReflection(this UIElement uiElement, string propertyName)
	{
		var type = GetType(uiElement);
		var key = (ownerType: type, propertyName);

		_dependencyPropertyReflectionCache ??= new Dictionary<(Type, string), DependencyProperty?>(2);

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

		_dependencyPropertyReflectionCache[key] = property;

		return property;

		[UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "🤷‍♂️")]
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		static Type GetType(object value) => value.GetType();
	}
}
#endif
