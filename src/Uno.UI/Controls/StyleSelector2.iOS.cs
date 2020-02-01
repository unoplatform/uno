using Windows.UI.Xaml;

namespace Uno.UI.Views.Controls
{
	public partial class StyleSelector2 : DependencyObject
	{
		private Style _style = default(Style);

		public Style Style {
			get {
				return _style;
			}
			set {
				_style = value;
			}
		}
	}
}