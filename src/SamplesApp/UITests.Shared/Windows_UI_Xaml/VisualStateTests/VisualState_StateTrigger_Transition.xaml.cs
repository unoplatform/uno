using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.VisualStateTests
{
	[Sample("Visual states", Description = "Resize window past 800px to trigger state changes. VisualTransitions should animate smoothly (not snap). Event log should show CHANGING before CHANGED.", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class VisualState_StateTrigger_Transition : Page
	{
		public VisualState_StateTrigger_Transition()
		{
			this.InitializeComponent();
		}

		private void OnCurrentStateChanging(object sender, VisualStateChangedEventArgs e)
		{
			EventLog.Text += $"CHANGING: {e.OldState?.Name ?? "(null)"} -> {e.NewState?.Name ?? "(null)"}\n";
		}

		private void OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
		{
			EventLog.Text += $"CHANGED: {e.OldState?.Name ?? "(null)"} -> {e.NewState?.Name ?? "(null)"}\n";
		}
	}
}
