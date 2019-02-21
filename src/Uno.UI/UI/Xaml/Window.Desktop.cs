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

		public Window()
		{
		}

		private void InternalSetContent(UIElement value)
		{

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
