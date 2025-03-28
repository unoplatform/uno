using Windows.UI.Xaml;

namespace Uno.UI.Views.Controls
{
	public partial class StyleSelector2 : DependencyObject
	{
		Style _style;

		public Style Style
		{
			get
			{
				return _style;
			}
			set
			{
				_style = value;
			}
		}
	}
}
