using System;
using System.Runtime.InteropServices;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Provides the static <see cref="SetBinding" /> method.
	/// </summary>
	public sealed partial class BindingOperations
	{
		/// <summary>
		/// Associates a <see cref="Binding"/> with a target property on a target object. 
		/// This method is the code equivalent to using a {Binding} markup extension in XAML markup. 
		/// </summary>
		/// <param name="target">The object that should be the target of the evaluated binding.</param>
		/// <param name="dp">The property on the target to bind, specified by its identifier. These identifiers are usually available as static read-only properties on the type that defines the target object, or one of its base types.</param>
		/// <param name="binding">The binding to assign to the target property. This <see cref="Binding"/> should be initialized: important <see cref="Binding"/> properties such as <see cref="PropertyPath"/> Path should already be set before passing it as the parameter.</param>
		public static void SetBinding(DependencyObject target, DependencyProperty dp, BindingBase binding)
		{
			(target as IDependencyObjectStoreProvider)?.Store.SetBinding(dp, binding);
		}
	}
}
