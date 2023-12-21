#if IS_UNIT_TESTS || __NETSTD_REFERENCE__

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml
{
	public sealed partial class Window
	{
		private Border _rootBorder;
		private bool _isActive;

		partial void InitPlatform()
		{
			CoreWindow = CoreWindow.GetOrCreateForCurrentThread();
		}

		partial void ShowPartial()
		{
			_isActive = true;
			TryLoadRootVisual();
		}

		private void InternalSetContent(UIElement content)
		{
			if (_rootVisual is null)
			{
				_rootBorder = new Border();
				var coreServices = Uno.UI.Xaml.Core.CoreServices.Instance;
				coreServices.PutVisualRoot(_rootBorder);
				_rootVisual = coreServices.MainRootVisual;

				if (_rootVisual == null)
				{
					throw new InvalidOperationException("The root visual could not be created.");
				}

				TryLoadRootVisual();
			}

			if (_rootBorder != null)
			{
				_rootBorder.Child = _content = content;
			}
		}

		private void TryLoadRootVisual()
		{
			if (_isActive)
			{
				(_rootVisual as FrameworkElement)?.ForceLoaded();
			}
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
