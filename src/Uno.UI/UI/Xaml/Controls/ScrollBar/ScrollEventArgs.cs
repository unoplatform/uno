namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class ScrollEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		public double NewValue
		{
			get; internal set;
		}

		public ScrollEventType ScrollEventType
		{
			get; internal set;
		}

		public ScrollEventArgs() : base()
		{
		}
	}
}
