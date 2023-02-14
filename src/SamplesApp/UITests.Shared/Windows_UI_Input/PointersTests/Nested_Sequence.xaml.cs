using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample(
		"Pointers",
		Description = "Automated test that validates the bubbling of pointer events, noticeably enter and exit.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class Nested_Sequence : Page
	{
		public Nested_Sequence()
		{
			this.InitializeComponent();

			HookEvents(_container);
			HookEvents(_intermediate);
			HookEvents(_nested);
		}

		private void HookEvents(UIElement elt)
		{
			elt.PointerEntered += LogEvent("Entered");
			elt.PointerExited += LogEvent("Exited");
			elt.PointerPressed += LogEvent("Pressed");
			elt.PointerReleased += LogEvent("Released");
			elt.PointerCanceled += LogEvent("Canceled");
		}

		private PointerEventHandler LogEvent(string eventName) => (snd, args)
			=> Write($"[{((FrameworkElement)snd).Name.Trim('_').ToUpperInvariant()}] {eventName} (in_contact={args.Pointer.IsInContact} | in_range={args.Pointer.IsInRange})");

		private void Write(string text)
		{
			Console.WriteLine(text);
			_result.Text += text + "\r\n";
		}
	}
}
