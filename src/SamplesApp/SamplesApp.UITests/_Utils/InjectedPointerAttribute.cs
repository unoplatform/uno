using System;
using System.Linq;
using Windows.Devices.Input;

namespace Uno.UI.RuntimeTests;

/// <summary>
/// Clone of the Uno.UI.RuntimeTests.InjectedPointerAttribute to be used in SamplesApp.UITests.
/// This ias NO EFFECT, see remarks.
/// </summary>
/// <remarks>
/// This attribute is used only for inputs injected from the app itself (i.e. runtime tests).
/// Inputs sent externally by test engine like Selenium (wasm) and Xamarin UI tests (iOS and Android) are not using this attribute
/// and will only sent default pointer type on each platform (touch for iOS and Android, mouse for wasm).
/// Currently this attribute is present on those platforms only to share code between "UI tests" on those platforms
/// and "runtime tests (with input injection)" on Skia.
/// Injected inputs are not supported yet on wasm, iOS and Android.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InjectedPointerAttribute : Attribute
{
	public PointerDeviceType Type { get; }

	public InjectedPointerAttribute(PointerDeviceType type)
	{
		Type = type;
	}
}
