using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToolTip
{
	public sealed partial class ShowPathToRootControl : UserControl
	{
		public ShowPathToRootControl()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private void OnLoaded(object snd, RoutedEventArgs evt)
		{
			var ctl = snd as FrameworkElement;
			var sb = new StringBuilder();

			while (ctl != null)
			{
				sb.AppendLine($"{ctl.GetType()} - {ctl.Name}");
				ctl = ctl.Parent as FrameworkElement;
			}

			pathToRoot.Text = sb.ToString();
		}
	}
}
