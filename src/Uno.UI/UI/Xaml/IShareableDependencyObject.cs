using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyObject"/> that supports association with multiple 'owners'.
	/// </summary>
	internal interface IShareableDependencyObject
	{
		bool IsClone { get; }
		DependencyObject Clone();
	}
}
