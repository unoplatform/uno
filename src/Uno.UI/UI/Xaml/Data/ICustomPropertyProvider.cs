using System;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Provides lookup service for ICustomProperty support.
	/// This interface is implemented by objects so that their
	/// custom defined properties can be used as run-time binding sources.
	/// </summary>
	public partial interface ICustomPropertyProvider
	{
		/// <summary>
		/// Gets the underlying type of the custom property.
		/// </summary>
		Type Type { get; }

		/// <summary>
		/// Gets a custom property's ICustomProperty support object by specifying
		/// a property name.
		/// </summary>
		/// <param name="name">The name of the property to get the support object for.</param>
		/// <returns>The returned support object for the custom property, or null.</returns>
		ICustomProperty GetCustomProperty(string name);

		/// <summary>
		/// Gets a custom property's ICustomProperty support object by specifying
		/// a property name and the type of the indexed collection.
		/// </summary>
		/// <param name="name">The name of the property to get the support object for.</param>
		/// <param name="type">The type of the indexed collection, specified as a TypeName wrapper.</param>
		/// <returns>The returned support object for the custom property, or null.</returns>
		ICustomProperty GetIndexedProperty(string name, Type type);

		/// <summary>
		/// Provides support for "GetStringFromObject" and/or "ToString" logic
		/// on the assumption that the implementation supports System.Object.
		/// This logic might be accessed by features or services such as generating
		/// UI Automation values based on data content.
		/// </summary>
		/// <returns>The provided string.</returns>
		string GetStringRepresentation();
	}
}
