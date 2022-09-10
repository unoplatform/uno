namespace Windows.UI.Xaml.Controls.Primitives
{
	/// <summary>
	/// Represents the method that will handle a ValueChanged event.
	/// </summary>
	/// <param name="sender">The object where the event handler is attached.</param>
	/// <param name="e">The event data.</param>
	public delegate void RangeBaseValueChangedEventHandler(
	  object sender,
	  RangeBaseValueChangedEventArgs e
	);
}
