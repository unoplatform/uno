#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Markup;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A set of Uno specific markup helpers
	/// </summary>
	public static class MarkupHelper
	{
		/// <summary>
		/// Sets the x:Uid member on a element implementing <see cref="IXUidProvider"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="uid">The new uid to set</param>
		public static void SetXUid(object target, string uid)
		{
			if(target is IXUidProvider provider)
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
		/// Sets a builder for markup-lazy properties in <see cref="VisualState"/>
		/// </summary>
		public static void SetVisualStateLazy(VisualState target, Action builder)
			=> target.LazyBuilder = builder;

		/// <summary>
		/// Sets a builder for markup-lazy properties in <see cref="VisualTransition"/>
		/// </summary>
		public static void SetVisualTransitionLazy(VisualTransition target, Action builder)
			=> target.LazyBuilder = builder;
		
		public static IXamlServiceProvider CreateParserContext(object? target, ProvideValueTargetProperty? property)
			=> new XamlServiceProviderContext
			{
				TargetObject = target,
				TargetProperty = property,
			};

		public static IXamlServiceProvider CreateParserContext(object? target, Type propertyDeclaringType, string propertyName, Type propertyType)
			=> CreateParserContext(target, new ProvideValueTargetProperty
			{
				DeclaringType = propertyDeclaringType,
				Name = propertyName,
				Type = propertyType,
			});
	}
}
