#nullable enable

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// For tests of a language's own default clock, which the user's time setting can change.
/// </summary>
public static class DefaultClockHelper
{
	/// <summary>
	/// On Uno, stands in a device without a 12/24-hour setting until disposed, so the language's own clock applies.
	/// </summary>
	public static IDisposable UseDeviceWithoutClockSetting()
	{
#if HAS_UNO
		var device = global::Windows.Globalization.DeviceClock.TestDevice;
		global::Windows.Globalization.DeviceClock.TestDevice = ("zxx", null, null);
		return new Restore(() => global::Windows.Globalization.DeviceClock.TestDevice = device);
#else
		return new Restore(() => { });
#endif
	}

	/// <summary>
	/// Makes the test inconclusive when the Windows regional format is this culture with a customized time format,
	/// which reaches a culture of that name: through .NET's user overrides on Uno, through NLS on WinUI.
	/// </summary>
	public static void AssumeStockTimeFormat(string cultureName)
	{
		var user = CultureInfo.InstalledUICulture;
		if (!string.Equals(user.Name, cultureName, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		var stock = CultureInfo.GetCultureInfo(cultureName).DateTimeFormat;
		if (user.DateTimeFormat.ShortTimePattern != stock.ShortTimePattern || user.DateTimeFormat.LongTimePattern != stock.LongTimePattern)
		{
			Assert.Inconclusive($"The regional format customizes the {cultureName} time format.");
		}
	}

	private sealed class Restore(Action restore) : IDisposable
	{
		public void Dispose() => restore();
	}
}
