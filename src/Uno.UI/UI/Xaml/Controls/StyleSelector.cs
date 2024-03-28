namespace Windows.UI.Xaml.Controls
{
	public partial class StyleSelector
	{
		public Style SelectStyle(object item, DependencyObject container)
		{
			return SelectStyleCore(item, container);
		}

		protected virtual Style SelectStyleCore(object item, DependencyObject container)
		{
			return null;
		}
	}
}

