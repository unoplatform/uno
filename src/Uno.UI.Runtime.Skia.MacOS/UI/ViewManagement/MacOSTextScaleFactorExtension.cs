using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSTextScaleFactorExtension : ITextScaleFactorExtension
{
	private static readonly MacOSTextScaleFactorExtension _instance = new();

	public event EventHandler? TextScaleFactorChanged;

	public static unsafe void Register()
	{
		ApiExtensibility.Register(typeof(ITextScaleFactorExtension), _ => _instance);
		NativeUno.uno_set_text_scale_factor_change_callback(&Update);
	}

	public double GetTextScaleFactor()
	{
		var value = NativeUno.uno_get_text_scale_factor();
		return value > 0 ? value : 1.0;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void Update()
	{
		if (typeof(MacOSTextScaleFactorExtension).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSTextScaleFactorExtension).Log().Trace($"MacOSTextScaleFactorExtension.TextScaleFactorChanged {_instance.GetTextScaleFactor()}");
		}

		_instance.TextScaleFactorChanged?.Invoke(_instance, EventArgs.Empty);
	}
}
