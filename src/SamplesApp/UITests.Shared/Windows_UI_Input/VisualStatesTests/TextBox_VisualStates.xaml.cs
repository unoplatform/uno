using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.VisualStatesTests
{
	[SampleControlInfo("Gesture Recognizer", "VisualStates TextBox")]
	public sealed partial class TextBox_VisualStates : Page
	{
		public TextBox_VisualStates()
		{
			this.InitializeComponent();
		}

		private void ListenVisualStates(object sender, RoutedEventArgs e)
		{
			FrameworkElement target;
			IEnumerable<VisualStateGroup> groups;
			if (!(sender is FrameworkElement ctrl)
				|| (target = ctrl.FindFirstChild<FrameworkElement>(includeCurrent: false)) == null)
			{
				Console.WriteLine($"{sender?.GetType().Name ?? "-null-"} is not a FrameworkElement or has no child.");
			}
			else if ((groups = VisualStateManager.GetVisualStateGroups(target))?.None() ?? true)
			{
				Console.WriteLine($"Not visual states groups found on {target.Name}.");
			}
			else
			{
				foreach (var group in groups)
				{
					group.CurrentStateChanged += (snd, args) => VisualStatesLog.Text += $"{ctrl.Name}:{group.Name}.{args.NewState.Name}\r\n";
				}
			}
		}
	}
}
