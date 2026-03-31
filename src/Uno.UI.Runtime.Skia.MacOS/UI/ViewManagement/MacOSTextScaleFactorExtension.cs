using System;
using Uno.Foundation.Extensibility;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.MacOS;

// macOS does not have a system-wide text scaling API.
// Returns 1.0 as documented known limitation.
internal class MacOSTextScaleFactorExtension : ITextScaleFactorExtension
{
	private static readonly MacOSTextScaleFactorExtension _instance = new();

#pragma warning disable 67 // Event is never used
	public event EventHandler? TextScaleFactorChanged;
#pragma warning restore 67

	public static void Register()
	{
		ApiExtensibility.Register(typeof(ITextScaleFactorExtension), _ => _instance);
	}

	public double GetTextScaleFactor() => 1.0;
}
