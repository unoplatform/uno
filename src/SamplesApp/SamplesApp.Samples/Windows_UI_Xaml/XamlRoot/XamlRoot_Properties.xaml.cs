using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.XamlRoot;

[Sample(
	"Windowing",
	Description =
		"Shows current values of various XamlRoot properties and their changes in time." +
		"Test RasterizationScale by changing the display DPI scaling in OS settings.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class XamlRoot_Properties : UserControl
{
	public XamlRoot_Properties()
	{
		this.InitializeComponent();

		Loaded += (s, e) =>
		{
			UpdateXamlRootProperties();
			XamlRoot.Changed += XamlRoot_Changed;
		};

		Unloaded += (s, e) =>
		{
			XamlRoot.Changed -= XamlRoot_Changed;
		};
	}

	public ObservableCollection<string> ChangeLog { get; } = new();

	private void XamlRoot_Changed(Microsoft.UI.Xaml.XamlRoot sender, XamlRootChangedEventArgs args) =>
		UpdateXamlRootProperties();

	private void UpdateXamlRootProperties()
	{
		RasterizationScaleRun.Text = XamlRoot.RasterizationScale.ToString();
		IsHostVisibleRun.Text = XamlRoot.IsHostVisible.ToString();
		SizeRun.Text = XamlRoot.Size.ToString();

		ChangeLog.Insert(0, $"[{DateTimeOffset.Now.ToString("HH:mm:ss")}] RasterizationScale: {XamlRoot.RasterizationScale}, IsHostVisible: {XamlRoot.IsHostVisible}, Size: {XamlRoot.Size}");
	}

	private void ClearClick() => ChangeLog.Clear();
}
