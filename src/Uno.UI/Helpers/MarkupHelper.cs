#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.Collections;
using System.ComponentModel;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Markup;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A set of Uno specific markup helpers
	/// </summary>
	public static class MarkupHelper
	{
		private static WeakAttachedDictionary<object, string>? _weakProperties;

		private static WeakAttachedDictionary<object, string> WeakProperties
			=> _weakProperties ??= new();

		/// <summary>
		/// Sets the x:Uid member on a element implementing <see cref="IXUidProvider"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="uid">The new uid to set</param>
		public static void SetXUid(object target, string uid)
		{
			if (target is IXUidProvider provider)
			{
				provider.Uid = uid;
			}
		}

		/// <summary>
		/// Gets the Uid defined via <see cref="SetXUid(object, string)"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <returns>A the x:Uid value</returns>
		public static string GetXUid(object target)
			=> target is IXUidProvider provider ? provider.Uid : "";

		/// <summary>
		/// Gets a resource string for an x:Uid bound property.
		/// </summary>
		/// <remarks>
		/// Returns null when the resource is an empty string.
		/// </remarks>
		public static string? GetResourceStringForXUid(string viewName, string resourceName)
		{
			var loader = viewName is not null
				? ResourceLoader.GetForCurrentView(viewName)
				: ResourceLoader.GetForCurrentView();

			return loader.GetString(resourceName) is { Length: > 0 } value
						? value
						: null;
		}

		/// <summary>
		/// Sets a builder for markup-lazy properties in <see cref="VisualState"/>
		/// </summary>
		public static void SetVisualStateLazy(VisualState target, Action builder)
			=> target.LazyBuilder = builder;

		/// <summary>
		/// Sets a builder for markup-lazy properties in <see cref="VisualTransition"/>
		/// </summary>
		public static void SetVisualTransitionLazy(VisualTransition target, Action builder)
			=> target.LazyBuilder = builder;

		public static IXamlServiceProvider CreateParserContext(object? target, Type propertyDeclaringType, string propertyName, Type propertyType, object? rootObject = null)
			=> new XamlServiceProviderContext
			{
				TargetObject = target,
				TargetProperty = new ProvideValueTargetProperty
				{
					DeclaringType = propertyDeclaringType,
					Name = propertyName,
					Type = propertyType,
				},
				RootObject = rootObject,
			};

		/// <summary>
		/// Attaches a property to an object, using a weak reference.
		/// </summary>
		/// <remarks>This helper is mainly used for XAML Hot Reload</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetElementProperty<TInstance>(object target, string propertyName, TInstance value)
			=> WeakProperties.SetValue(target, propertyName, value);

		/// <summary>
		/// Gets a property to an object, using a weak reference.
		/// </summary>
		/// <remarks>This helper is mainly used for XAML Hot Reload</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TInstance? GetElementProperty<TInstance>(object target, string propertyName)
			=> WeakProperties.GetValue<TInstance>(target, propertyName);

		/// <summary>
		/// Helper for XAML code generation. Not intended to be used in apps outside of XAML generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static ResourceDictionary AddMergedDictionaries(this ResourceDictionary dictionary, params ResourceDictionary[] mergedDictionaries)
		{
			foreach (var mergedDictionary in mergedDictionaries)
			{
				dictionary.MergedDictionaries.Add(mergedDictionary);
			}

			return dictionary;
		}
	}
}
