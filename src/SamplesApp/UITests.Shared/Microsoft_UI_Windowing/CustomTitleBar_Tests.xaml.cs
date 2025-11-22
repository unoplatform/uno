using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp;
using Uno.Disposables;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using WindowingSamples;
using Windows.Graphics;

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	IsManualTest = true,
	Description = "This sample tests custom titlebar in secondary windows.")]
public sealed partial class CustomTitleBar_Tests : Page
{
	public CustomTitleBar_Tests()
	{
		this.InitializeComponent();
	}

	public void DraggableRegionWindowClick()
	{
		var draggableRegionWindow = new DraggableRegionWindow();
		draggableRegionWindow.Activate();
	}

	public void CustomTitleBarClick()
	{
		var customTitleBarWindow = new CustomTitleBarWindow();
		customTitleBarWindow.Activate();
	}

	public void StandardExtendClick()
	{
		var standardExtendWindow = new ExtendContentIntoTitleBarWindow(TitleBarHeightOption.Standard);
		standardExtendWindow.Activate();
	}

	public void TallExtendClick()
	{
		var tallExtendWindow = new ExtendContentIntoTitleBarWindow(TitleBarHeightOption.Tall);
		tallExtendWindow.Activate();
	}
}
