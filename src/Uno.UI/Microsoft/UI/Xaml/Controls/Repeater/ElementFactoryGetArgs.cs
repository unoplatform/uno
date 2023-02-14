using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ElementFactoryGetArgs
	{
		internal int Index { get; set; }

		public UIElement Parent { get; set; }

		public object Data { get; set; }
	}
}
