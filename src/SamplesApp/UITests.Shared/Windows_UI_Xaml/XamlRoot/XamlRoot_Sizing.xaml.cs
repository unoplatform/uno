using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
			UpdateProperties();
			XamlRoot.Changed += OnXamlRootChanged;
		};

		Unloaded += (s, e) =>
		{
			XamlRoot.Changed -= OnXamlRootChanged;
		};

		SizeChanged += OnSizeChanged;
	}

	public ObservableCollection<string> ChangeLog { get; } = new();

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		Debug.WriteLine($"MainPage::OnSizeChanged: NewSize={PrettyPrint.FormatSize(args.NewSize)}, PreviousSize={PrettyPrint.FormatSize(args.PreviousSize)}");
		PageSize.Text = PrettyPrint.FormatSize(args.NewSize);
	}

	private int _count;
	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		_count++;
		XamlRootChangeCount.Text = _count.ToString();
		XamlRootSize.Text = PrettyPrint.FormatSize(sender.Size);

		Debug.WriteLine($"MainPage::OnXamlRootChanged: size={PrettyPrint.FormatSize(sender.Size)}");
	}
}
