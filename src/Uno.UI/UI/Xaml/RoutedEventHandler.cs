namespace Windows.UI.Xaml
{
	public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

	public delegate void RoutedEventHandler<in TArgs>(object sender, TArgs e)
		where TArgs : RoutedEventArgs;

#if __WASM__
	public delegate bool RoutedEventHandlerWithHandled(object sender, RoutedEventArgs e);
#endif
}
