namespace Windows.UI.Xaml
{
	public partial class ExceptionRoutedEventArgs : RoutedEventArgs
	{
		public ExceptionRoutedEventArgs(string errorMessage)
		{
			ErrorMessage = errorMessage;
		}

		public string ErrorMessage { get; }
	}
}