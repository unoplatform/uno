using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ElementFactoryGetArgs
	{
		internal int Index { get; set; }

		public UIElement Parent { get; set; }

		public object Data { get; set; }
	}
}
