#if NET461
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Windows.UI.Xaml
{
	public sealed partial class Window
	{
		private static Window _current;
		private bool _isActive;
		private UIElement _content;

		public Window()
		{
			InitializeCommon();
		}

		partial void InternalActivate()
		{
			_isActive = true;
			TryLoadContent();
		}

		private void InternalSetContent(UIElement value)
		{
			_content = value;
			TryLoadContent();
		}

		private void TryLoadContent()
		{
			if (_isActive)
			{
				(_content as FrameworkElement)?.ForceLoaded();
			}
		}

		private UIElement InternalGetContent()
		{
			throw new NotImplementedException();
		}

		private static Window InternalGetCurrentWindow()
		{
			if(_current == null)
			{
				_current = new Window();
			}

			return _current;
		}

		internal void SetWindowSize(Size size)
		{
			if(Bounds.Size != size)
			{
				Bounds = new Rect(0, 0, size.Width, size.Height);

				RaiseSizeChanged(new WindowSizeChangedEventArgs(size));
			}
		}
	}
}
#endif
