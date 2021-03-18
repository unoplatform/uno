using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class DataTemplateSelector
	{
		public DataTemplateSelector ()
		{
			Initialize ();
		}

		void Initialize ()
		{
		}

		public DataTemplate SelectTemplate(object item)
		{
			return SelectTemplateCore(item);
		}

		public DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			return SelectTemplateCore(item, container);
		}

		protected virtual DataTemplate SelectTemplateCore(object item)
		{
			return null;
		}

		protected virtual DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			return null;
		}
	}
}

