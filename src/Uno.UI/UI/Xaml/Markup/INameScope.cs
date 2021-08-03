using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Windows.UI.Xaml.Markup
{
	/// <summary>
	/// Defines a contract for how names of elements should be accessed within a particular XAML namescope, and how to enforce uniqueness of names within that XAML namescope.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface INameScope
	{
		/// <summary>
		/// Returns the top-level <see cref="DependencyObject"/> that defined this namescope
		/// </summary>
		DependencyObject Owner { get; }

		/// <summary>
		/// Returns an object that has the provided identifying name.
		/// </summary>
		/// <param name="name">The name identifier for the object being requested.</param>
		/// <returns>The object, if found. Returns null if no object of that name was found.</returns>
		object FindName(string name);

		/// <summary>
		/// Registers the provided name into the current XAML namescope.
		/// </summary>
		/// <param name="name">The name to register.</param>
		/// <param name="scopedElement">The specific element that the provided name refers to.</param>
		void RegisterName(string name, object scopedElement);

		/// <summary>
		/// Unregisters the provided name from the current XAML namescope.
		/// </summary>
		/// <param name="name">The name to unregister.</param>
		void UnregisterName(string name);
	}
}
