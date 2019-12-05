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
		}

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
