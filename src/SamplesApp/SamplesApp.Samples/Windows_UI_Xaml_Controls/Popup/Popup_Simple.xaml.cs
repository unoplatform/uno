using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Samples.Content.UITests.Popup
{
	[Sample("Popup", Name = "Popup_Simple", Description = "Description for sample of Popup_Simple")]
	public sealed partial class Popup_Simple : UserControl
	{
		public Popup_Simple()
		{
			this.InitializeComponent();

			Register(popup);
			Register(sampleContent);

			popupContent.Loaded += RegisterContent;
		}

		private void RegisterContent(object sender, RoutedEventArgs e)
		{
			var ctl = sender as FrameworkElement;
			while (ctl != null)
			{
				Register(ctl);
				Write($"{ctl}-registered, IsHitTestVisible={ctl.IsHitTestVisible}");
				ctl = ctl.Parent as FrameworkElement;
			}

			popupContent.Loaded -= RegisterContent;
		}

		private void Register(FrameworkElement control)
		{
			void OnPressed(object sender, PointerRoutedEventArgs e)
			{
				Write($"{sender}-pressed {e.GetCurrentPoint(this).Position}");
			}

			void OnReleased(object sender, PointerRoutedEventArgs e)
			{
				Write($"{sender}-released {e.GetCurrentPoint(this).Position}");
			}

			control.PointerPressed += OnPressed;
			control.PointerPressed += OnReleased;
		}


		private void Write(string msg) => output.Text += msg + "\n";
	}
}
