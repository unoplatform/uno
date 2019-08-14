using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private readonly Lazy<GestureHandler> _gestures;

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				_gestures.Value.UpdateShouldHandle(routedEvent, true);
			}
		}

		partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				_gestures.Value.UpdateShouldHandle(routedEvent, true);
			}
		}

		partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				_gestures.Value.UpdateShouldHandle(routedEvent, false);
			}
		}

		partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				_gestures.Value.UpdateShouldHandle(routedEvent, false);
			}
		}

		partial void InitializeCapture();

		protected override void ClearCaptures()
		{
			_pointCaptures.Clear();
		}

		protected override bool IsPointerCaptured => _pointCaptures.Any();

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			base.SetNativeHitTestVisible(newValue);
		}

		// This section is using the UnoViewGroup overrides for performance reasons
		// where most of the work is performed on the java side.

		protected override bool NativeHitCheck()
			=> IsViewHit();
	}
}
