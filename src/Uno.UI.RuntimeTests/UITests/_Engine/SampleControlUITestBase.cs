#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Uno.Testing;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest;

namespace SamplesApp.UITests;

// Note: All tests that are inheriting from this base class will run on UI thread.
public class SampleControlUITestBase : IInjectPointers
{
	protected RuntimeTestsApp App => RuntimeTestsApp.Current;

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public PointerDeviceType DefaultPointerType => App.DefaultPointerType;

	public PointerDeviceType CurrentPointerType => App.CurrentPointerType;

	protected async Task RunAsync(string metadataName)
	{
		await App.RunAsync(metadataName);
	}

	void IInjectPointers.CleanupPointers()
		=> App.CleanupPointers();

	/// <inheritdoc />
	public IDisposable SetPointer(PointerDeviceType type)
		=> App.SetPointer(type);

	public ValueTask<RawBitmap> TakeScreenshotAsync(string name)
		=> App.TakeScreenshotAsync(name);
}
