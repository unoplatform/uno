using System;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Implements custom property definition support for data binding sources that are implemented using COM.
	/// </summary>
	public partial interface ICustomProperty
	{
		/// <summary>
		/// Gets a value that determines whether the custom property supports read access.
		/// </summary>
		bool CanRead { get; }

		/// <summary>
		/// Gets a value that determines whether the custom property supports write access.
		/// </summary>
		bool CanWrite { get; }

		/// <summary>
		/// Gets the path-relevant name of the property.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the underlying type of the custom property.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets the value of the custom property from a particular instance.
		/// </summary>
		/// <param name="target">The owning instance.</param>
		/// <returns>The retrieved value.</returns>
		object GetValue(object target);

		/// <summary>
		/// Sets the custom property value on a specified instance.
		/// </summary>
		/// <param name="target">The owning instance.</param>
		/// <param name="value">The value to set.</param>
		void SetValue(object target, object value);

		/// <summary>
		/// Gets the value at an index location, for cases where the custom property has indexer support.
		/// </summary>
		/// <param name="target">The owning instance.</param>
		/// <param name="index">The index to get.</param>
		/// <returns>The retrieved value at the index.</returns>
		object GetIndexedValue(object target, object index);

		/// <summary>
		/// Sets the value at an index location, for cases where the custom property has indexer support.
		/// </summary>
		/// <param name="target">The owning instance.</param>
		/// <param name="value">The value to set.</param>
		/// <param name="index">The index location to set to.</param>
		void SetIndexedValue(object target, object value, object index);
	}
}
