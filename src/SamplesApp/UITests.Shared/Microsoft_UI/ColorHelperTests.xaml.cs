using System.Drawing;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;

namespace UITests.Shared.Microsoft_UI;

[SampleControlInfo("Microsoft.UI", "ColorHelper", description: "Demonstrates use of Microsoft.UI.ColorHelper", viewModelType: typeof(ColorHelperTestsViewModel), ignoreInSnapshotTests: true)]
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
		ColorName = ColorHelper.ToDisplayName(ColorHelper.ConvertColorFromHexString(ColorValue));
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
