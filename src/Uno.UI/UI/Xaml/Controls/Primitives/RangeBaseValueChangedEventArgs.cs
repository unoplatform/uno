namespace Windows.UI.Xaml.Controls.Primitives
{
	/// <summary>
	/// Provides data about a change in range value for the ValueChanged event.
	/// </summary>
	public sealed partial class RangeBaseValueChangedEventArgs : RoutedEventArgs
	{
		internal RangeBaseValueChangedEventArgs(object originalSource)
			: base(originalSource)
		{
		}

		/// <summary>
		/// Gets the new value of a range value property.
		/// </summary>
		public double NewValue { get; internal set; }

		/// <summary>
		/// Gets the previous value of a range value property.
		/// </summary>
		public double OldValue { get; internal set; }
	}
}
