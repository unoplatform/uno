using System;
using Windows.Graphics.Display;
using Android.App;
using Android.Content;
using Android.OS;
using System.Diagnostics;
using Android.Content.Res;
using Android.Telephony;
using Android.Util;

using Uno.Extensions;
using Windows.System.Profile.Internal;
using Uno.Foundation.Logging;

namespace Windows.System.Profile;

public static partial class AnalyticsInfo
{
	private static UnoDeviceForm GetDeviceForm()
	{
		if (SafeFormCheck(IsTelevision))
		{
			return UnoDeviceForm.Television;
		}

		if (SafeFormCheck(IsCar))
		{
			return UnoDeviceForm.Car;
		}

		if (SafeFormCheck(IsWatch))
		{
			return UnoDeviceForm.Watch;
		}

		if (SafeFormCheck(IsVirtualReality))
		{
			return UnoDeviceForm.VirtualReality;
		}

		if (SafeFormCheck(IsDesktop))
		{
			return UnoDeviceForm.Desktop;
		}

		if (SafeFormCheck(IsTablet))
		{
			return UnoDeviceForm.Tablet;
		}

		if (SafeFormCheck(IsPhone))
		{
			return UnoDeviceForm.Mobile;
		}

		// If nothing is returned, we cannot determine the family.
		return UnoDeviceForm.Unknown;
	}

	private static bool SafeFormCheck(Func<bool> checkForm)
	{
		try
		{
			return checkForm();
		}
		catch (Exception ex)
		{
			if (typeof(AnalyticsInfo).Log().IsEnabled(LogLevel.Error))
			{
				typeof(AnalyticsInfo).Log().LogError("Checking form factor failed " + ex.Message);
			}
		}
		return false;
	}

	private static bool IsTelevision() =>
		Build.VERSION.SdkInt >= BuildVersionCodes.HoneycombMr2
		&& Application.Context.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
		&& uiModeManager.CurrentModeType == UiMode.TypeTelevision;

	private static bool IsCar() =>
		Application.Context.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
		&& uiModeManager.CurrentModeType == UiMode.TypeCar;

	private static bool IsWatch() =>
		Build.VERSION.SdkInt >= BuildVersionCodes.KitkatWatch
		&& Application.Context.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
		&& uiModeManager.CurrentModeType == UiMode.TypeWatch;

	private static bool IsVirtualReality() =>
		Application.Context.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
		&& uiModeManager.CurrentModeType == UiMode.TypeVrHeadset;

	private static bool IsDesktop() =>
		Application.Context.GetSystemService(Context.UiModeService) is UiModeManager uiModeManager
		&& uiModeManager.CurrentModeType == UiMode.TypeDesk;

	private static bool IsPhone() =>
		Application.Context.GetSystemService(Context.TelephonyService) is TelephonyManager telephonyManager &&
		telephonyManager.PhoneType != PhoneType.None;

	private static bool IsTablet()
	{
		// https://github.com/dotnet/maui/blob/1bbe79de61f241217f88207b1272179ff66e6733/src/Essentials/src/DeviceInfo/DeviceInfo.android.cs#L59-L75
		const int tabletCrossover = 600;

		if (Application.Context.Resources is not { } resources)
		{
			return false;
		}

		var configuration = resources.Configuration;
		if (configuration != null)
		{
			return configuration.SmallestScreenWidthDp >= tabletCrossover;
		}

		// start clutching at straws
		using var metrics = resources.DisplayMetrics;
		if (metrics != null)
		{
			var minSize = Math.Min(metrics.WidthPixels, metrics.HeightPixels);
			return minSize * metrics.Density >= tabletCrossover;
		}

		return false;
	}
}
