#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Globalization;

namespace Uno.UI.Extensions;

public static partial class UIElementExtensions
{
	internal static CornerRadius GetCornerRadius(this UIElement uiElement)
	{
		if (uiElement is FrameworkElement fe && fe.TryGetCornerRadius(out var cornerRadius))
		{
			return cornerRadius;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("CornerRadius");
		return property != null && uiElement.GetValue(property) is CornerRadius t ? t : default;
	}

	internal static Thickness GetPadding(this UIElement uiElement)
	{
		if (uiElement is FrameworkElement fe && fe.TryGetPadding(out var padding))
		{
			return padding;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
		return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
	}

	internal static Thickness GetBorderThickness(this UIElement uiElement)
	{
		if (uiElement is FrameworkElement fe && fe.TryGetBorderThickness(out var borderThickness))
		{
			return borderThickness;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("BorderThicknessProperty");
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

	internal static bool SetBorderThickness(this UIElement uiElement, Thickness borderThickness)
	{
		if (uiElement is FrameworkElement fe && fe.TrySetBorderThickness(borderThickness))
		{
			return true;
		}

		var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("BorderThicknessProperty");
		if (property != null)
		{
			uiElement.SetValue(property, borderThickness);
			return true;
		}

		return false;
	}

	private static Dictionary<(Type type, string property), DependencyProperty?>? _dependencyPropertyReflectionCache;

	internal static DependencyProperty? FindDependencyPropertyUsingReflection<TProperty>(this UIElement uiElement, string propertyName)
	{
		var type = uiElement.GetType();
		var propertyType = typeof(TProperty);
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

		if (property == null)
		{
			uiElement.Log().Warn($"The {propertyName} dependency property does not exist on {type}");
		}
		else if (property.Type != propertyType)
		{
			uiElement.Log().Warn($"The {propertyName} dependency property {type} is not of the {propertyType} Type.");
			property = null;
		}

		_dependencyPropertyReflectionCache[key] = property;

		return property;
	}
}
