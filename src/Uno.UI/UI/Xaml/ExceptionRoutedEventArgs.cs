namespace Windows.UI.Xaml
{
	public partial class ExceptionRoutedEventArgs : RoutedEventArgs
	{
		internal ExceptionRoutedEventArgs(object originalSource, string errorMessage)
			: base(originalSource)
		{
			ErrorMessage = errorMessage;
		}

		public string ErrorMessage { get; }
	}
}
