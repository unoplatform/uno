using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Windows.Devices.Input;
using Windows.UI.Input;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", Name = "MousePointer", IsManualTest = true,
		Description = "Demonstrates iPadOS mouse/trackpad support: hover, pointer position, button state, right-click and per-element cursors.")]
	public sealed partial class MousePointer : Page
	{
		private int _rightTapCount;

		public MousePointer()
		{
			this.InitializeComponent();

			Surface.PointerEntered += (s, e) => Update("Entered", e);
			Surface.PointerExited += (s, e) => Update("Exited", e);
			Surface.PointerMoved += (s, e) => Update("Moved", e);
			Surface.PointerPressed += (s, e) => Update("Pressed", e);
			Surface.PointerReleased += (s, e) => Update("Released", e);
			Surface.PointerWheelChanged += (s, e) => Update("Wheel", e);
			Surface.RightTapped += OnRightTapped;
		}

		private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			_rightTapCount++;
			EventLog.Text = $"RightTapped #{_rightTapCount} ({e.PointerDeviceType}) at {Format(e.GetPosition(Surface))}";
		}

		private void Update(string action, PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint(Surface);
			var p = point.Properties;

			SurfaceState.Text =
				$"Last event: {action}\n" +
				$"Device: {e.Pointer.PointerDeviceType}\n" +
				$"Position: {Format(point.Position)}\n" +
				$"InContact: {point.IsInContact}   InRange: {p.IsInRange}\n" +
				$"Buttons: L={p.IsLeftButtonPressed} R={p.IsRightButtonPressed} M={p.IsMiddleButtonPressed}\n" +
				$"UpdateKind: {p.PointerUpdateKind}\n" +
				$"WheelDelta: {p.MouseWheelDelta} (horizontal: {p.IsHorizontalMouseWheel})\n" +
				$"Modifiers: {e.KeyModifiers}";
		}

		private static string Format(Windows.Foundation.Point p)
			=> string.Create(CultureInfo.InvariantCulture, $"{p.X:F1}, {p.Y:F1}");
	}
}
