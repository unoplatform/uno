#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A delegate that creates an instance of a type.
	/// </summary>
	public delegate object ActivatorDelegate();

	/// <summary>
	/// A delegate that gets the value of a indexer.
	/// </summary>
	public delegate object? StringIndexerGetterDelegate(object instance, string name);

	/// <summary>
	/// A delegate that sets the value using an indexer.
	/// </summary>
	public delegate void StringIndexerSetterDelegate(object instance, string name, object? value);

	/// <summary>
	/// Defines a bindable type.
	/// </summary>
	public interface IBindableType
	{
		/// <summary>
		/// Provides the Type of this bindable type
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets a method that will create an instance of the currentype.
		/// </summary>
		/// <returns>An initialized instance.</returns>
		ActivatorDelegate? CreateInstance();

		/// <summary>
		/// Gets a bindable property for the current type.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>A bindable property instance, otherwise null.</returns>
		IBindableProperty? GetProperty(string name);

		/// <summary>
		/// Returns a function that can be called to get the indexer value.
		/// </summary>
		/// <returns>A bindable property instance, otherwise null.</returns>
		StringIndexerGetterDelegate? GetIndexerGetter();

		/// <summary>
		/// Returns an action that can be called to set the indexer value.
		/// </summary>
		/// <returns></returns>
		StringIndexerSetterDelegate? GetIndexerSetter();
	}
}
