using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
using Uno.Foundation.Logging;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.Win32;

// MUX Reference xcpcore.cpp, tag winui3/release/1.8.1 — text scale factor from registry
internal class Win32TextScaleFactorExtension : ITextScaleFactorExtension
{
	private const string SoftwareMicrosoftAccessibility = @"SOFTWARE\Microsoft\Accessibility";
	private const string TextScaleFactorValueName = "TextScaleFactor";

	public static Win32TextScaleFactorExtension Instance { get; } = new();

	public event EventHandler? TextScaleFactorChanged;

	public unsafe double GetTextScaleFactor()
	{
		using var hSubKey = new Win32Helper.NativeNulTerminatedUtf16String(SoftwareMicrosoftAccessibility);
		using var valueName = new Win32Helper.NativeNulTerminatedUtf16String(TextScaleFactorValueName);

		int value = 100;
		REG_VALUE_TYPE regValueType;
		uint valueSize = (uint)Marshal.SizeOf<int>();
		var errorCode = PInvoke.RegGetValue(HKEY.HKEY_CURRENT_USER, hSubKey, valueName, REG_ROUTINE_FLAGS.RRF_RT_DWORD, &regValueType, &value, &valueSize);
		if (errorCode is not WIN32_ERROR.ERROR_SUCCESS)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to read TextScaleFactor from registry: {Win32Helper.GetErrorMessage((uint)errorCode)}");
			}
			return 1.0;
		}

		return value / 100.0;
	}

	internal void RaiseTextScaleFactorChanged()
	{
		TextScaleFactorChanged?.Invoke(this, EventArgs.Empty);
	}
}
