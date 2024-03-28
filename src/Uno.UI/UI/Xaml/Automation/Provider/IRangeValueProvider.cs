namespace Windows.UI.Xaml.Automation.Provider
{
	/// <summary>
	/// Exposes methods and properties to support access by a Microsoft UI Automation client to controls that can be set to a value within a range.
	/// Implement this interface in order to support the capabilities that an automation client requests with a GetPattern call and PatternInterface.RangeValue.
	/// </summary>
	public partial interface IRangeValueProvider
	{
		/// <summary>
		/// Gets a value that indicates whether the value of a control is read-only.
		/// </summary>
		bool IsReadOnly { get; }

		/// <summary>
		/// Gets the value that is added to or subtracted from the Value property when a large change is made, such as with the PAGE DOWN key.
		/// </summary>
		double LargeChange { get; }

		/// <summary>
		/// Gets the maximum range value that is supported by the control.
		/// </summary>
		double Maximum { get; }

		/// <summary>
		/// Gets the maximum range value that is supported by the control.
		/// </summary>
		double Minimum { get; }

		/// <summary>
		/// Gets the value that is added to or subtracted from the Value property when a small change is made, such as with an arrow key.
		/// </summary>
		double SmallChange { get; }

		/// <summary>
		/// Gets the value of the control.
		/// </summary>
		double Value { get; }

		/// <summary>
		/// Sets the value of the control.
		/// </summary>
		/// <param name="value">The value to set.</param>
		void SetValue(double value);
	}
}
