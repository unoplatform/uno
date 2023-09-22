using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.ViusalStateTests
{
	[SampleControlInfo("Visual states")]
	public sealed partial class VisualState_AdaptiveTrigger_Storyboard : Page
	{
		public VisualState_AdaptiveTrigger_Storyboard()
		{
			this.InitializeComponent();

			void OnSizeChanged(object snd, SizeChangedEventArgs evt)
			{
				var w = Window.Current is not null ? Window.Current.Bounds : new Windows.Foundation.Rect(default, XamlRoot.Size);

				txt.Text += $"Control Size: {evt?.NewSize.Width}, Window Size:{w}\n";
			}

			SizeChanged += OnSizeChanged;

			OnSizeChanged(null, null);
		}

		private void OnSmall(object sender, object e)
		{
			txt.Text += "Trigger: OnSmall()\n";
		}

		private void OnLarge(object sender, object e)
		{
			txt.Text += "Trigger: OnLarge()\n";
		}
	}
}
