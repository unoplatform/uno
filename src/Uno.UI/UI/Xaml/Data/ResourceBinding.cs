#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Binding used for resource resolution at load time. 
	/// </summary>
	internal class ResourceBinding : BindingBase
	{
		/// <summary>
		/// The resource key.
		/// </summary>
		public SpecializedResourceDictionary.ResourceKey ResourceKey { get; }

		/// <summary>
		/// True if the original assignation used the ThemeResource extension, false if it used StaticResource. (This determines whether it
		/// should be updated when the active theme changes.)
		/// </summary>
		public bool IsThemeResourceExtension { get; }

		public object? ParseContext { get; }

		public DependencyPropertyValuePrecedences Precedence { get; }

		/// <summary>
		/// The binding path that this resource binding is targeting, if it was created by a VisualState <see cref="Setter"/>.
		/// </summary>
		public BindingPath? SetterBindingPath { get; }

		public ResourceBinding(SpecializedResourceDictionary.ResourceKey resourceKey, bool isThemeResourceExtension, object? parseContext, DependencyPropertyValuePrecedences precedence, BindingPath? setterBindingPath)
		{
			ResourceKey = resourceKey;
			IsThemeResourceExtension = isThemeResourceExtension;
			ParseContext = parseContext;
			Precedence = precedence;
			SetterBindingPath = setterBindingPath;
		}
	}
}
