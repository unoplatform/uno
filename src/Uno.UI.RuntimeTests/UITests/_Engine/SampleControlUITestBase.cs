#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Uno.UI.RuntimeTests;
using Uno.UITest;

namespace SamplesApp.UITests;

[RunsOnUIThread]
public class SampleControlUITestBase
{
	protected SkiaApp App => SkiaApp.Current;

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public PointerDeviceType DefaultPointerType => InputInjectorHelper.DefaultPointerType;

	public PointerDeviceType CurrentPointerType => InputInjectorHelper.Current.CurrentPointerType;

	protected async Task RunAsync(string metadataName)
	{
		await App.RunAsync(metadataName);
	}
}
