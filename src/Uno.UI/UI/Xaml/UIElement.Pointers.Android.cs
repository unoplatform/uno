using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Extensions;
using Windows.UI.Xaml.Input;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
// Avoids ambiguity between Windows.UI.Core.PointerEventArgs and Microsoft.UI.Input.PointerEventArgs
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
#endif

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		partial void InitializePointersPartial()
		{
			ArePointersEnabled = true;
		}


		partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode)
			=> IsNativeMotionEventsInterceptForbidden = newMode == ManipulationModes.None;

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
		{
			recognizer.ManipulationConfigured += (snd, manip) =>
			{
				var scrollableDirection = this
					.GetAllParents()
					.Aggregate(
						(h: false, v: false),
						(direction, parent) =>
							parent switch
							{
								ScrollContentPresenter scp => (direction.h || scp.CanHorizontallyScroll, direction.v || scp.CanVerticallyScroll),
								IScrollContentPresenter iscp => (direction.h || iscp.CanHorizontallyScroll, direction.v || iscp.CanVerticallyScroll),
								_ => direction
							});

				if ((scrollableDirection.h && manip.IsTranslateXEnabled)
					|| (scrollableDirection.v && manip.IsTranslateYEnabled))
				{
					RequestDisallowInterceptTouchEvent(true);
				}
			};
			recognizer.ManipulationStarted += (snd, args) =>
			{
				RequestDisallowInterceptTouchEvent(true);
			};

			// The manipulation can be aborted by the user before the pointer up, so the auto release on pointer up is not enough
			recognizer.ManipulationCompleted += (snd, args) =>
			{
				if (ManipulationMode != ManipulationModes.None)
				{
					RequestDisallowInterceptTouchEvent(false);
				}
			};
			recognizer.ManipulationAborted += (snd, args) =>
			{
				if (ManipulationMode != ManipulationModes.None)
				{
					RequestDisallowInterceptTouchEvent(false);
				}
			};

			// This event means that the touch was long enough and any move will actually start the manipulation,
			// so we use "Started" instead of "Starting"
			recognizer.DragReady += (snd, manip) =>
			{
				RequestDisallowInterceptTouchEvent(true);
			};
			recognizer.Dragging += (snd, args) =>
			{
				switch (args.DraggingState)
				{
					case DraggingState.Started:
						RequestDisallowInterceptTouchEvent(true); // Still usefull for mouse and pen
						break;
					case DraggingState.Completed when ManipulationMode != ManipulationModes.None:
						RequestDisallowInterceptTouchEvent(false);
						break;
				}
			};
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			UpdateHitTest();
		}

		// This section is using the UnoViewGroup overrides for performance reasons
		// where most of the work is performed on the java side.

		protected override bool NativeHitCheck()
			=> IsViewHit();
	}
}
