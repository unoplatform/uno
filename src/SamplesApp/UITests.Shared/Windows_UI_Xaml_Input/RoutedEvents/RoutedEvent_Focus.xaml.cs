using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;

namespace UITests.Shared.Windows_UI_Xaml_Input.RoutedEvents
{
	[Sample("Routed Events", "GotFocus/LostFocus")]
	public sealed partial class RoutedEvent_Focus : Page
	{
		public RoutedEvent_Focus()
		{
			this.InitializeComponent();

			this.GotFocus += (snd, evt) => txtRoot.Text += $"GOTFOCUS (handler) sender={GetName(snd)}, originalSource={GetName(evt.OriginalSource)}\n";
			this.LostFocus += (snd, evt) => txtRoot.Text += $"LOSTFOCUS (handler) sender={GetName(snd)}, originalSource={GetName(evt.OriginalSource)}\n";
		}

		private static string GetName(object element)
		{
			if (element == null)
			{
				return "<null>";
			}
			if (element is FrameworkElement fe)
			{
				return string.IsNullOrWhiteSpace(fe.Name) ? fe.ToString() : fe.Name;
			}

			return element.ToString();
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			txtRoot.Text += $"GOTFOCUS (override) originalSource={GetName(e.OriginalSource)}\n";
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			txtRoot.Text += $"LOSTFOCUS (override) originalSource={GetName(e.OriginalSource)}\n";
		}
	}
}
