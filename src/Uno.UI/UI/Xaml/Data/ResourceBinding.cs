using System;
using System.Collections.Generic;
using System.Text;

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
		public object ResourceKey { get; }
		/// <summary>
		/// True if the original assignation used the ThemeResource extension, false if it used StaticResource. (This determines whether it
		/// should be updated when the active theme changes.)
		/// </summary>
		public bool IsThemeResourceExtension { get; }

		public ResourceBinding(object resourceKey, bool isThemeResourceExtension)
		{
			ResourceKey = resourceKey;
			IsThemeResourceExtension = isThemeResourceExtension;
		}
	}
}
