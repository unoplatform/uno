#if NET461 || __NETSTD_REFERENCE__

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private RootVisual _rootVisual;
		private bool _isActive;
		private UIElement _content;

		public Window()
		{
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();

			InitializeCommon();
		}

		partial void InternalActivate()
		{
			_isActive = true;
			TryLoadContent();
		}

		private void InternalSetContent(UIElement value)
		{
			_rootVisual = new RootVisual(CoreServices.Instance);
			if (_content != null)
			{
				_content.IsWindowRoot = false;
				_content.IsVisualTreeRoot = false;
			}

			_content = value;

			_content.IsWindowRoot = true;
			_content.IsVisualTreeRoot = true;

			TryLoadContent();
		}

		private void TryLoadContent()
		{
			if (_isActive)
			{
				(_content as FrameworkElement)?.ForceLoaded();
			}
		}

		private UIElement InternalGetContent() => _content;

		private UIElement InternalGetRootElement() => _content;

		private static Window InternalGetCurrentWindow()
		{
			if (_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal void SetWindowSize(Size size)
		{
			if (Bounds.Size != size)
			{
				Bounds = new Rect(0, 0, size.Width, size.Height);

				RaiseSizeChanged(new Windows.UI.Core.WindowSizeChangedEventArgs(size));
			}
		}
	}
}
#endif
