#if !HAS_UNO_WINUI
namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Provides the means to report XAML-type system specifics about XAML types. Using this interface contract,
/// XAML parsers can load any custom types and members thereof that are defined in your app and are referenced in XAML files.
/// </summary>
public partial interface IXamlType2 : IXamlType
{
	/// <summary>
	/// Gets the IXamlType for the boxed type of the XAML type.
	/// Determination of this value is based on the underlying type for core types.
	/// </summary>
	IXamlType BoxedType { get; }
}
#endif
