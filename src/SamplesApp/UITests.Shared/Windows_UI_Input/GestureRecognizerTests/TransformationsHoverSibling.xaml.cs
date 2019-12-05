using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Android.Hardware.Input;
using Android.OS;
using Android.Views;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[SampleControlInfo("Gesture recognizer")]
	public sealed partial class TransformationsHoverSibling : Page
	{
		public TransformationsHoverSibling()
		{
			this.InitializeComponent();


			////var inst = new Android.App.Instrumentation();
			////var automation = inst.UiAutomation;


			////var abc = automation as Android.App.UiAutomation;

			//var manager = (InputManager)ContextHelper.Current.GetSystemService(Android.Content.Context.InputService);
			//var stylus = manager
			//	.GetInputDeviceIds()
			//	.Select(id => manager.GetInputDevice(id))
			//	.FirstOrDefault(dev => dev.SupportsSource(InputSourceType.Stylus));

			//var now = SystemClock.UptimeMillis();

			////var manager = new InputManager()
			////{

			////}
			//var evt = MotionEvent.Obtain(now, now, MotionEventActions.HoverEnter, 100, 100, default(MetaKeyStates));

			//var javaType = Java.Lang.Class.FromType(typeof(InputManager));
			//var p1 = Java.Lang.Class.FromType(typeof(InputEvent));
			//var p2 = Java.Lang.Class.FromType(typeof(Java.Lang.Integer));
			//var injectMethod = javaType.GetMethod("injectInputEvent", p1, p2);
			//injectMethod.Accessible = true;
			//injectMethod.Invoke(manager, evt, new Java.Lang.Integer(INJECT_INPUT_EVENT_MODE_WAIT_FOR_FINISH));


			////automation.InjectInputEvent(evt, true);
		}


		/**
     * Input Event Injection Synchronization Mode: None.
     * Never blocks.  Injection is asynchronous and is assumed always to be successful.
     * @hide
     */
		public const int INJECT_INPUT_EVENT_MODE_ASYNC = 0; // see InputDispatcher.h
		/**
		* Input Event Injection Synchronization Mode: Wait for result.
		* Waits for previous events to be dispatched so that the input dispatcher can
		* determine whether input event injection will be permitted based on the current
		* input focus.  Does not wait for the input event to finish being handled
		* by the application.
		* @hide
		*/
		public const int INJECT_INPUT_EVENT_MODE_WAIT_FOR_RESULT = 1;  // see InputDispatcher.h
		/**
		* Input Event Injection Synchronization Mode: Wait for finish.
		* Waits for the event to be delivered to the application and handled.
		* @hide
		*/
		//@UnsupportedAppUsage
		public const int INJECT_INPUT_EVENT_MODE_WAIT_FOR_FINISH = 2;  // see InputDispatcher.h

		private void OnMovedOnPink(object sender, PointerRoutedEventArgs e)
			=> PinkLocation.Text = F(e.GetCurrentPoint(PinkZone).Position);

		private void OnEnteredPink(object sender, PointerRoutedEventArgs e)
			=> PinkState.Text = "Hover";

		private void OnExitedPink(object sender, PointerRoutedEventArgs e)
			=> PinkState.Text = "Out";

		private void OnMovedOnBlue(object sender, PointerRoutedEventArgs e)
			=> BlueLocation.Text = F(e.GetCurrentPoint(BlueZone).Position);

		private void OnEnteredBlue(object sender, PointerRoutedEventArgs e)
			=> BlueState.Text = "Hover";

		private void OnExitedBlue(object sender, PointerRoutedEventArgs e)
			=> BlueState.Text = "Out";

		private static string F(Point position) => $"({position.X:F2},{position.Y:F2})";
	}
}
