using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void CustomSetContent(UIElement value)
	{
		//if (_rootVisual == null)
		//{
			//_rootBorder = new Border();
			//CoreServices.PutVisualRoot(_rootBorder);
			//_rootVisual = coreServices.MainRootVisual;

			//if (_rootVisual is null)
			//{
			//	throw new InvalidOperationException("The root visual could not be created.");
			//}

			if (coreServices.ContentRootCoordinator.CoreWindowContentRoot is { } contentRoot)
			{
				contentRoot.SetHost(this); // Enables input manager
			}
			else
			{
				throw new InvalidOperationException("The content root could not be created.");
			}

			UIElement.LoadingRootElement(_rootVisual);

			_mainController.View = _rootVisual;
			_rootVisual.Frame = _window.Frame;
			_rootVisual.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;

			UIElement.RootElementLoaded(_rootVisual);
		}

		_rootBorder.Child?.RemoveFromSuperview();
		_rootBorder.Child = _content = value;

		// This is required to get the mouse move while not pressed!
		var options = NSTrackingAreaOptions.MouseEnteredAndExited
			| NSTrackingAreaOptions.MouseMoved
			| NSTrackingAreaOptions.ActiveInKeyWindow
			| NSTrackingAreaOptions.EnabledDuringMouseDrag // We want enter/leave events even if the button is pressed
			| NSTrackingAreaOptions.InVisibleRect; // Automagicaly syncs the bounds rect
		var trackingArea = new NSTrackingArea(Bounds, options, _rootVisual, null);

		_rootVisual.AddTrackingArea(trackingArea);
	}
}
