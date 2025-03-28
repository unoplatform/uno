using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SamplesApp;
using Uno.Disposables;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Graphics;

#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
#endif

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	IsManualTest = true,
	Description =
		"- On Android and iOS, try to back out of this app to the main screen and reopen the app via its icon. " +
		"You should see the Samples app UI load normally - not a blank screen and the app should also not crash. \r\n" +
		"- On Android and iOS, try to back out of this app and then activate it by opening an URI like uno-samples-test:something. " +
		"You should see the Samples app UI load normally - not a blank screen and the app should also not crash. ")]
public sealed partial class SingleWindowClose : Page
{
	public SingleWindowClose()
	{
		this.InitializeComponent();
	}
}
