#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ThumbTests;

[Sample("Thumb")]
public sealed partial class Thumb_CapturePreventScroll : Page
{
	public Thumb_CapturePreventScroll()
	{
		this.InitializeComponent();
	}

	private void OnScrolled(object? sender, ScrollViewerViewChangedEventArgs e)
	{
		if (sender is ScrollViewer sv)
		{
			Output.Text = $"v: {sv.VerticalOffset:G2} | h: {sv.HorizontalOffset:G2}";
		}
	}
}
