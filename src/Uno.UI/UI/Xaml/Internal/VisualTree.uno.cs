#nullable enable

using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core
{
	internal partial class VisualTree
	{
		private const int UnoTopZIndex = int.MaxValue - 100;
		private const int FocusVisualZIndex = UnoTopZIndex + 1;

		public Canvas? FocusVisualRoot { get; private set; }

		public void EnsureFocusVisualRoot()
		{
			if (FocusVisualRoot == null)
			{
				FocusVisualRoot = new Canvas()
				{
					Background = null,
					IsHitTestVisible = false
				};
				Canvas.SetZIndex(FocusVisualRoot, FocusVisualZIndex);
			}
		}
	}
}
