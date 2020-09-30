using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample(
		"Pointers",
		Description = "[Automated test] Validates that disabled button does not receive pointer events.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class Enability : UserControl
	{
		public Enability()
		{
			this.InitializeComponent();

			RegisterEvents(DisabledButton);
			RegisterEvents(DisablingButton);

			DisablingButton.Click += (snd, e) => DisablingButton.IsEnabled = false;
		}

		public void RegisterEvents(Button button)
		{
			button.Click += (snd, e) => _output.Text = "Click";
			button.PointerPressed += (snd, e) => _output.Text = "Pressed";
			button.PointerReleased += (snd, e) => _output.Text = "Released";
			button.PointerMoved += (snd, e) => _output.Text = "Moved";
			button.PointerEntered += (snd, e) => _output.Text = "Entered";
			button.PointerExited += (snd, e) => _output.Text = "Exited";
		}
	}
}
