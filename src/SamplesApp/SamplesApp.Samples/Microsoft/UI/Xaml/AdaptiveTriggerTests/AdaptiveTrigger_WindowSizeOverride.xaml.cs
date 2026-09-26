using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Windows_UI_Xaml.AdaptiveTriggerTests;

[Sample("Visual states", Description = "Demonstrates the Uno Platform-specific AdaptiveTrigger.SetWindowSizeOverride host hook: toggle the override and resize the item across the 720px breakpoint to flip the trigger without resizing the window.", IsManualTest = true, IgnoreInSnapshotTests = true)]
public sealed partial class AdaptiveTrigger_WindowSizeOverride : Page
{
	public AdaptiveTrigger_WindowSizeOverride()
	{
		this.InitializeComponent();

		Unloaded += OnUnloaded;
		UpdateStatus();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
#if HAS_UNO
		// The override is process-global — clear it so the rest of the app reverts to window bounds.
		AdaptiveTrigger.SetWindowSizeOverride(null);
#endif
	}

	private void OnOverrideToggled(object sender, RoutedEventArgs e) => ApplyOverride();

	private void OnResizableItemSizeChanged(object sender, SizeChangedEventArgs e) => ApplyOverride();

	private void ApplyOverride()
	{
#if HAS_UNO
		if (OverrideToggle.IsOn)
		{
			AdaptiveTrigger.SetWindowSizeOverride(new Size(ResizableItem.ActualWidth, ResizableItem.ActualHeight));
		}
		else
		{
			AdaptiveTrigger.SetWindowSizeOverride(null);
		}
#endif
		UpdateStatus();
	}

	private void UpdateStatus()
	{
#if HAS_UNO
		StatusText.Text = OverrideToggle.IsOn
			? $"Override active: {ResizableItem.ActualWidth:F0} × {ResizableItem.ActualHeight:F0} — AdaptiveTriggers evaluate against the item size."
			: "Override off — AdaptiveTriggers evaluate against the window bounds.";
#else
		StatusText.Text = "AdaptiveTrigger.SetWindowSizeOverride is Uno Platform-specific and is not available on Windows (WinUI).";
#endif
	}
}
