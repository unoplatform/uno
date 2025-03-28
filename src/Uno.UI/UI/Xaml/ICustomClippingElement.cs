#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
namespace Windows.UI.Xaml;

internal interface ICustomClippingElement
{
	/// <summary>
	/// Define if the control allows to be clipped to its bounds
	/// when the size is constrained.
	/// </summary>
	/// <remarks>
	/// This is called during the Arrange phase, can be dynamic.
	/// </remarks>
	bool AllowClippingToLayoutSlot { get; }

	/// <summary>
	/// Define if the control is forcing clipping to its bounds.
	/// </summary>
	/// <remarks>
	/// This is called during the Arrange phase, can be dynamic.
	/// </remarks>
	bool ForceClippingToLayoutSlot { get; }
}
#endif
