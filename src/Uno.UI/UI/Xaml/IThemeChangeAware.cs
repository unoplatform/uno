using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Implemented by <see cref="DependencyObject"/> types that need to react when the visual theme changes.
	/// </summary>
	internal interface IThemeChangeAware
	{
		void OnThemeChanged();
	}
}
