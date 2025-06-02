using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Silk.NET.Core.Native;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.XamlRoot;

[Sample(
	"Windowing",
	Description =
		"Try to change the size of the window, or rotate the device on mobile. Both size values should stay in sync.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class XamlRoot_Sizing : UserControl
{
	public XamlRoot_Sizing()
	{
		this.InitializeComponent();

		Loaded += (s, e) =>
		{
			XamlRoot.Changed += OnXamlRootChanged;
			(XamlRoot.Content as FrameworkElement).SizeChanged += OnSizeChanged;
		};

		Unloaded += (s, e) =>
		{
			XamlRoot.Changed -= OnXamlRootChanged;
			(XamlRoot.Content as FrameworkElement).SizeChanged -= OnSizeChanged;
		};
	}

	public ObservableCollection<string> ChangeLog { get; } = new();

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		PageSize.Text = "X: " + args.NewSize.Width + ", Y: " + args.NewSize.Height;
		var previousSizeString = "X: " + args.PreviousSize.Width + ", Y: " + args.PreviousSize.Height;
		System.Diagnostics.Debug.WriteLine($"MainPage::OnSizeChanged: NewSize={PageSize.Text}, PreviousSize={previousSizeString}");
	}

	private int _count;
	private void OnXamlRootChanged(Microsoft.UI.Xaml.XamlRoot sender, XamlRootChangedEventArgs args)
	{
		_count++;
		XamlRootChangeCount.Text = _count.ToString();
		XamlRootSize.Text = "X: " + sender.Size.Width + ", Y: " + sender.Size.Height;

		System.Diagnostics.Debug.WriteLine($"MainPage::OnXamlRootChanged: size={XamlRootSize.Text}");
	}
}
