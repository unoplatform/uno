using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[SampleControlInfo("Pointers", "Capture")]
	public sealed partial class Capture : Page
	{
		public Capture()
		{
			this.InitializeComponent();

			// Simple capture test
			SetupEvents(SimpleTarget, SimpleResult);
			SetupEvents(VisibilityTarget, VisibilityResult, Hide(VisibilityTarget));
			SetupEvents(NestedVisibilityTarget, NestedVisibilityResult, Hide(NestedVisibilityHost));
		}

		private (Action onPressed, Action onReleased) Hide(UIElement element)
			=> (() => element.Visibility = Visibility.Collapsed, () => element.Visibility = Visibility.Visible);

		private void SetupEvents(UIElement target, TextBlock result, (Action onPressed, Action onReleased) actions = default((Action, Action)))
		{
			target.PointerPressed += (snd, e) =>
			{
				result.Text = target.CapturePointer(e.Pointer)
					? "CAPTURED"
					: "FAILED (capture refused)";

				actions.onPressed?.Invoke();
			};

			target.PointerCaptureLost += (snd, e) =>
			{
				result.Text = "SUCCESS";
			};

			if (actions.onReleased != null)
			{
				target.PointerReleased += (snd, e) => actions.onReleased();
			}
		}
	}
}
