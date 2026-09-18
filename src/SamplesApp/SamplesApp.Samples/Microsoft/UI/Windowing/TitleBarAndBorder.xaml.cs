using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using SamplesApp;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "TitleBar and Border", IsManualTest = true, Description = "Toggle Window/AppWindow TitleBar extension and apply OverlappedPresenter.SetBorderAndTitleBar.")]
public sealed partial class TitleBarAndBorder : Page
{
	private TitleBarAndBorderViewModel _vm = new TitleBarAndBorderViewModel();

	public TitleBarAndBorder()
	{
		InitializeComponent();
		DataContext = _vm;
	}
}
