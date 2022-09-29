#nullable disable

using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIcon : IconElement
	{
		private bool _initialized = false;
		private bool _applyTemplateCalled = false;

		private void EnsureInitialized()
		{
			InitializeVisualTree();

			// Uno workaround: OnApplyTemplate is not called when there is no template.
			if (!_applyTemplateCalled)
			{
				OnApplyTemplate();
				_applyTemplateCalled = true;
			}
		}

		private void InitializeVisualTree()
		{
			if (!_initialized)
			{
				// TODO Uno specific - We must add the child element manually.
				AddIconElementView(new Grid());
				_initialized = true;
			}
		}
	}
}
