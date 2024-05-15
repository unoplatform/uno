namespace Windows.Foundation;

/// <summary>
/// Enables arbitrary enumerations, structures, and delegate types to be used as an array of property values. You can't implement this interface, see Remarks.
/// </summary>
/// <remarks>
/// You can't implement the IReferenceArray interface or include it in a signature. IReferenceArray is mainly an internal implementation detail
/// of how the Windows Runtime implements boxing and nullable values.
/// </remarks>
/// <typeparam name="T">Item type.</typeparam>
public partial interface IReferenceArray<T> : IPropertyValue
{
	/// <summary>
	/// Gets the type that is represented as an IPropertyValue array.
	/// </summary>
	T[] Value { get; }
}
