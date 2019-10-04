namespace Windows.UI.Xaml
{
	internal interface ICustomClippingElement
	{
		/// <summary>
		/// Define if the control allows to be clipped to its bounds
		/// when the size is constrained.
		/// </summary>
		/// <remarks>
		/// This is called during the Arrange phase.
		/// </remarks>
		bool AllowClippingToBounds { get; }
	}
}
