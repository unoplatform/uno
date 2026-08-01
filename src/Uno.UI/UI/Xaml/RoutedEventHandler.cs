namespace Microsoft.UI.Xaml
{
	public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

	public delegate void RoutedEventHandler<in TArgs>(object sender, TArgs e)
		where TArgs : RoutedEventArgs;
}
