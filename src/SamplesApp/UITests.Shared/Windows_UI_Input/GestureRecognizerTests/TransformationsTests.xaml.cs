using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Android.AccessibilityServices;
using Android.Hardware.Input;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Uno.UI;
using Uno.UI.Samples.Controls;


namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[SampleControlInfo("Gesture recognizer")]
	public sealed partial class TransformationsTests : Page
	{
		public TransformationsTests()
		{
			this.InitializeComponent();

			//var automation = ContextHelper.Current.GetSystemService(Android.Content.Context.AccessibilityService);
			//var svc = (AccessibilityService)automation;

			var inst = new Android.App.Instrumentation();
			var automation = inst.UiAutomation;
			

			//var abc = automation as Android.App.UiAutomation;

			var manager = (InputManager)ContextHelper.Current.GetSystemService(Android.Content.Context.InputService);
			var stylus = manager
				.GetInputDeviceIds()
				.Select(id => manager.GetInputDevice(id))
				.FirstOrDefault(dev => dev.SupportsSource(InputSourceType.Stylus));

			var now = SystemClock.UptimeMillis();

			//var manager = new InputManager()
			//{

			//}
			var evt = MotionEvent.Obtain(now, now, MotionEventActions.HoverEnter, 100, 100, default(MetaKeyStates));

			automation.InjectInputEvent(evt, true);



			//Android.App.UiAutomation.

			//var javaType = Java.Lang.Class.FromType(typeof(InputManager));
			//var injectMethod = javaType.GetMethod("injectInputEvent");
			//injectMethod.Accessible = true;
			//injectMethod.Invoke(manager, evt, new Java.Lang.Integer())
		}

		//private static class InjectMode

		private void OnParentPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			var parentRelToTarget = e.GetCurrentPoint(Target).Position;
			var parentRelToParent = e.GetCurrentPoint(Parent).Position;

			ParentRelToTarget.Text = F(parentRelToTarget);
			ParentRelToParent.Text = F(parentRelToParent);
		}

		private void OnTargetPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			var targetRelToTarget = e.GetCurrentPoint(Target).Position;
			var targetRelToParent = e.GetCurrentPoint(Parent).Position;

			TargetRelToTarget.Text = F(targetRelToTarget);
			TargetRelToParent.Text = F(targetRelToParent);
		}

		private static string F(Point position) => $"({position.X:F2},{position.Y:F2})";
	}
}
