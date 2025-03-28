using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	public sealed partial class UIElement_InitializationSequence_Materializer : Control
	{
		public UIElement_InitializationSequence_Materializer()
		{
			UIElement_InitializationSequence.Instance.Log("ControlTemplate materialized.").Dispose();
		}
	}
}
