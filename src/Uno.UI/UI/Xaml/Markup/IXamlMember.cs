namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Provides the means to report XAML-type system specifics about XAML members. Using this interface contract,
/// XAML parsers can load any custom types and members thereof that are defined in your app and are referenced in XAML files.
/// </summary>
public partial interface IXamlMember
{
	/// <summary>
	/// Gets a value that indicates whether the XAML member is an attachable member.
	/// </summary>
	bool IsAttachable { get; }

	/// <summary>
	/// Gets a value that indicates whether the XAML member is implemented as a dependency property.
	/// </summary>
	bool IsDependencyProperty { get; }

	/// <summary>
	/// Gets whether the XAML member is read-only in its backing implementation.
	/// </summary>
	bool IsReadOnly { get; }

	/// <summary>
	/// Gets the XamlName name string that declares the XAML member.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the IXamlType of the type where the member can exist.
	/// </summary>
	IXamlType TargetType { get; }

	/// <summary>
	/// Gets the IXamlType of the type that is used by the member.
	/// </summary>
	IXamlType Type { get; }

	/// <summary>
	/// Provides a get-value utility for this IXamlMember.
	/// </summary>
	/// <param name="instance">The object instance to get the member value from.</param>
	/// <returns>The member value.</returns>
	object GetValue(object instance);

	/// <summary>
	/// Provides a set-value utility for this IXamlMember.
	/// </summary>
	/// <param name="instance">The object instance to set the member value on.</param>
	/// <param name="value">The member value to set.</param>
	void SetValue(object instance, object value);
}
