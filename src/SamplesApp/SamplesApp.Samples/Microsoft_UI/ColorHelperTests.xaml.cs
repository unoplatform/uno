using System.Drawing;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;

namespace UITests.Shared.Microsoft_UI;

[Sample("Microsoft.UI", Description = "Demonstrates use of Microsoft.UI.ColorHelper. Until WinUI3 1.6+ this currently only works on Uno targets", ViewModelType = typeof(ColorHelperTestsViewModel), IgnoreInSnapshotTests = true)]
public sealed partial class ColorHelperTests : UserControl
{
	public ColorHelperTests()
	{
		this.InitializeComponent();
	}
}

[Bindable]
internal class ColorHelperTestsViewModel(UnitTestDispatcherCompat dispatcher) : ViewModelBase(dispatcher)
{
	private string _colorName;
	private string _colorValue = "6f2526";

	public string ColorValue
	{
		get => _colorValue;
		set
		{
			_colorValue = value;
			RaisePropertyChanged();
		}
	}

	public Command ConvertCommand => new((p) =>
	{
		//UNO TODO: Remove after updating to WinUI 1.6+
#if HAS_UNO
		ColorName = Windows.UI.ColorHelper.ToDisplayName(Windows.UI.ColorHelper.ConvertColorFromHexString(ColorValue));
#endif
	});

	public string ColorName
	{
		get => _colorName;
		private set
		{
			_colorName = value;
			RaisePropertyChanged();
		}
	}
}
