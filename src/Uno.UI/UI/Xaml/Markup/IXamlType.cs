using System;

namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Provides the means to report XAML-type system specifics about XAML types. Using this interface contract,
/// XAML parsers can load any custom types and members thereof that are defined in your app and are referenced in XAML files.
/// </summary>
public partial interface IXamlType
{
	/// <summary>
	/// Gets the IXamlType for the immediate base type of the XAML type.
	/// Determination of this value is based on the underlying type for core types.
	/// </summary>
	IXamlType BaseType { get; }

#if HAS_UNO_WINUI // In WUX this is in IXamlType2
	/// <summary>
	/// Gets the IXamlType for the boxed type of the XAML type.
	/// Determination of this value is based on the underlying type for core types.
	/// </summary>
	IXamlType BoxedType { get; }
#endif

	/// <summary>
	/// Gets the IXamlMember information for the XAML content property of this IXamlType.
	/// </summary>
	IXamlMember ContentProperty { get; }

	/// <summary>
	/// Gets the full class name of the underlying type.
	/// </summary>
	string FullName { get; }

	/// <summary>
	/// Gets a value that indicates whether the IXamlType represents an array.
	/// </summary>
	bool IsArray { get; }

	/// <summary>
	/// Gets a value that declares whether the type is bindable.
	/// </summary>
	bool IsBindable { get; }

	/// <summary>
	/// Gets a value that indicates whether this IXamlType represents a collection.
	/// </summary>
	bool IsCollection { get; }

	/// <summary>
	/// Gets a value that indicates whether this IXamlType represents a constructible type, as per the XAML definition.
	/// </summary>
	bool IsConstructible { get; }

	/// <summary>
	/// Gets a value that indicates whether this IXamlType represents a dictionary/map.
	/// </summary>
	bool IsDictionary { get; }

	/// <summary>
	/// Gets a value that indicates whether the IXamlType represents a markup extension.
	/// </summary>
	bool IsMarkupExtension { get; }

	/// <summary>
	/// Gets a value that provides the type information for the Items property of this IXamlType.
	/// </summary>
	IXamlType ItemType { get; }

	/// <summary>
	/// Gets a value that provides the type information for the Key property of this IXamlType, if this IXamlType represents a dictionary/map.
	/// </summary>
	IXamlType KeyType { get; }

	/// <summary>
	/// Gets information for the backing type.
	/// </summary>
	Type UnderlyingType { get; }

	/// <summary>
	/// Given a XAML type, sets its values for initialization and returns a usable instance.
	/// </summary>
	/// <returns>The usable instance.</returns>
	object ActivateInstance();

	/// <summary>
	/// Creates a type system representation based on a string.
	/// The main scenario for this usage is creating an enumeration value and mapping the appropriate enumeration.
	/// </summary>
	/// <param name="value">The string to create from.</param>
	/// <returns>The resulting type system representation.</returns>
	object CreateFromString(string value);

	/// <summary>
	/// Returns the IXamlMember information for a specific named member from this IXamlType.
	/// </summary>
	/// <param name="name">The name of the member to get (as a string).</param>
	/// <returns>The IXamlMember information for the member, if a member as specified by name was found; otherwise, null.</returns>
	IXamlMember GetMember(string name);

	/// <summary>
	/// Adds an item to a custom vector type.
	/// </summary>
	/// <param name="instance">The type instance to set the item to.</param>
	/// <param name="value">The value of the item to add.</param>
	void AddToVector(object instance, object value);

	/// <summary>
	/// Adds an item to a custom map type.
	/// </summary>
	/// <param name="instance">The type instance to set the map item to.</param>
	/// <param name="key">The key of the map item to add.</param>
	/// <param name="value">The value of the map item to add.</param>
	void AddToMap(object instance, object key, object value);

	/// <summary>
	/// Invokes any necessary pre-activation logic as required by the XAML schema context and its platform dependencies.
	/// </summary>
	void RunInitializer();
}
