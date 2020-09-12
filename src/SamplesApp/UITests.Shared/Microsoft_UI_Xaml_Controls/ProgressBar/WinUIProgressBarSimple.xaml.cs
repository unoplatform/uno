using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Extensions;

namespace UITests.Microsoft_UI_Xaml_Controls.ProgressBar
{
	[Sample("Progress")]
	public sealed partial class WinUIProgressBarSimple : Page
	{
		public WinUIProgressBarSimple()
		{
			this.InitializeComponent();

			Loaded += (snd, evt) =>
			{
				var root = Uno.UI.Extensions.DependencyObjectExtensions.FindFirstChild<Grid>(progressBar);
				var stateGroups = VisualStateManager.GetVisualStateGroups(root);
				stateGroups
					.First()
					.CurrentStateChanging += (s, e) =>
				{
					states.Text += $">>{e.NewState.Name}>>  ";
				};
				stateGroups
					.First()
					.CurrentStateChanged += (s, e) =>
				{
					states.Text += $"[{e.NewState.Name}]\n";
				};
			};
		}
	}
}
