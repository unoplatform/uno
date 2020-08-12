using System;
using System.Collections.Generic;
using System.Text;
using Uno;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Implemented by types that have <see cref="DependencyObject"/> children that are not reachable via a <see cref="DependencyProperty"/>, which may
	/// have theme resource bindings needing update when the app theme changes.
	/// </summary>
	internal interface INeedsThemeBindingUpdates
	{
		/// <summary>
		/// Return additional <see cref="DependencyObject"/> children that are not reachable via a <see cref="DependencyProperty"/>.
		/// </summary>
		IEnumerable<DependencyObject> GetAdditionalChildObjects();
	}
}
