using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

			if (m_foregroundColorPropertyChangedRevoker.Disposable is null)
			{
				OnForegroundPropertyChanged(this, null);
			}
		}

		private void InitializeVisualTree()
		{
			if (!_initialized)
			{
				// TODO Uno specific - We must add the child element manually.
				InitializeRootGrid();
				_initialized = true;
			}
		}

		private void OnIconUnloaded(object sender, RoutedEventArgs args)
		{
			m_foregroundColorPropertyChangedRevoker.Disposable = null;
		}
	}
}
