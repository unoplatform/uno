namespace Windows.UI.Xaml
{
	/// <summary>
	/// Internal implemenation for <see cref="DependencyObject"/>
	/// </summary>
	internal partial interface IDependencyObjectInternal
	{
		/// <summary>
		/// Invoked on every <see cref="DependencyProperty"/> changes, automatically generated for every <see cref="DependencyObject"/> implementing type.
		/// </summary>
		/// <param name="args"></param>
		void OnPropertyChanged2(DependencyPropertyChangedEventArgs args);
	}
}
