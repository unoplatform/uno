// TODO:MZ!!!


using System;
using System.Globalization;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace Uno.UI.Helpers.WinUI;

internal static class CppWinRTHelpers
{
	/// <remarks>
	/// Note: Although this is usually called as 'SetDefaultStyleKey(this)' (per WinUI C++ code), we actually only use the compile-time
	///  TDerived type and ignore the runtime derivedControl parameter, preserving the expected behaviour that DefaultStyleKey is 'fixed'
	/// under inheritance unless explicitly changed by an inheriting type.
	/// </remarks>
	internal static void SetDefaultStyleKey<TDerived>(this TDerived derivedControl) where TDerived : Control
	{
		derivedControl.SetDefaultStyleKeyInternal(typeof(TDerived));

		if (derivedControl is Control control)
		{
			Uri uri = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);
			control.DefaultStyleResourceUri = uri;
		}
	}

	/// <summary>
	/// Default style key for controls of the Microsoft.UI.Xaml.Controls.Tabular binary (TableView and its parts),
	/// whose default styles live in that binary's own generic.xaml.
	/// </summary>
	/// <remarks>
	/// Port of SetDefaultStyleKeyWorker, controls\dev\dll-tabular\TabularControlsResources.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475.
	/// Like <see cref="SetDefaultStyleKey{TDerived}(TDerived)"/>, the key is the compile-time TDerived type.
	/// </remarks>
	internal static void SetTabularDefaultStyleKey<TDerived>(this TDerived derivedControl) where TDerived : Control
	{
		derivedControl.SetDefaultStyleKeyInternal(typeof(TDerived));

		// TABULAR_BINARY_EMITS_THEME_RESOURCES is defined for the Tabular binary (dll-tabular\Microsoft.UI.Xaml.Common.props).
		if (derivedControl is Control control)
		{
			const bool isPerf2026Enabled = false; // TODO: Decide based on opt-in flag, task.ms/60958581
			Uri uri = new Uri(isPerf2026Enabled
				? XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.TabularRootNamespace + "/Themes/generic_perf2026.xaml"
				: XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.TabularGenericURL);
			control.DefaultStyleResourceUri = uri;
		}
	}

	public static IDisposable RegisterXamlRootChanged(XamlRoot xamlRoot, TypedEventHandler<XamlRoot, XamlRootChangedEventArgs> handler)
	{
		xamlRoot.Changed += handler;
		return Disposable.Create(() => xamlRoot.Changed -= handler);
	}

	public static IDisposable RegisterPropertyChanged(DependencyObject dependencyObject, DependencyProperty dependencyProperty, DependencyPropertyChangedCallback callback)
	{
		var token = dependencyObject.RegisterPropertyChangedCallback(dependencyProperty, callback);
		return Disposable.Create(() => dependencyObject.UnregisterPropertyChangedCallback(dependencyProperty, token));
	}

	public static bool SetFocus(DependencyObject obj, FocusState focusState)
	{
		if (obj != null)
		{
			// Use TryFocusAsync if it's available.
			if (false) //TODO Uno specific: WinUI checks for TryFocusAsync method presence, which is not implemented yet in Uno (issue #4256)
			{
				//var result = FocusManager.TryFocusAsync(obj, focusState);
				//if (result.Status == AsyncStatus.Completed)
				//{
				//	return result.GetResults().Succeeded;
				//}
				//// Operation was async, let's assume it worked.
				//return true;
			}

			if (obj is Control control)
			{
				return control.Focus(focusState);
			}
			else if (obj is Hyperlink hyperlink)
			{
				return hyperlink.Focus(focusState);
			}
			else if (obj is WebView webview)
			{
				return webview.Focus(focusState);
			}
		}

		return false;
	}

#nullable enable
	/// <summary>
	/// Equivalent of C++/WinRT <c>weak_ref&lt;T&gt;::get()</c>: the target, or null when the reference
	/// is empty or the target has been collected.
	/// </summary>
	internal static T? Get<T>(this WeakReference<T>? weakRef) where T : class
		=> weakRef is not null && weakRef.TryGetTarget(out var target) ? target : null;

	private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
	private const int HRESULT_FROM_WIN32_RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
	private const int JSCRIPT_E_CANTEXECUTE = unchecked((int)0x89020001);

	/// <summary>
	/// Equivalent of raising a C++/WinRT <c>winrt::event&lt;T&gt;</c> (<c>event::operator()</c> + <c>impl::invoke</c>):
	/// every handler of the snapshot is called even when an earlier one throws, a thrown exception is
	/// reported but not rethrown, and a handler whose target is disconnected is removed.
	/// </summary>
	/// <remarks>
	/// Only for members that are a plain <c>winrt::event</c>. The MUX <c>event_source</c> does not swallow
	/// exceptions, so events backed by it stay plain C# events.
	/// </remarks>
	internal static void InvokeWinRTEvent<TDelegate>(ref TDelegate? field, Action<TDelegate> invoke) where TDelegate : Delegate
	{
		var targets = field?.GetInvocationList();
		if (targets is null)
		{
			return;
		}

		foreach (var target in targets)
		{
			var handler = (TDelegate)target;
			try
			{
				invoke(handler);
			}
			catch (Exception ex)
			{
				// TODO Uno: C++/WinRT reports through RoTransformError; the closest Uno equivalent is the log.
				if (typeof(CppWinRTHelpers).Log().IsEnabled(LogLevel.Error))
				{
					typeof(CppWinRTHelpers).Log().Error("An event handler threw; the exception was swallowed as C++/WinRT does.", ex);
				}

				if (ex.HResult is RPC_E_DISCONNECTED or HRESULT_FROM_WIN32_RPC_S_SERVER_UNAVAILABLE or JSCRIPT_E_CANTEXECUTE)
				{
					field = (TDelegate?)Delegate.Remove(field, handler);
				}
			}
		}
	}

	/// <summary>
	/// Equivalent of C++/WinRT <c>winrt::to_hstring(float)</c>, which formats with MSVC <c>std::to_chars(value)</c>.
	/// </summary>
	internal static string ToHString(float value)
	{
		uint bits = BitConverter.SingleToUInt32Bits(value);
		if (float.IsFinite(value))
		{
			return FormatShortest(value.ToString("R", CultureInfo.InvariantCulture), (double)value);
		}

		return FormatNonFinite((bits & 0x8000_0000u) != 0, bits & 0x007F_FFFFu, 0x0040_0000u);
	}

	/// <summary>
	/// Equivalent of C++/WinRT <c>winrt::to_hstring(double)</c>, which formats with MSVC <c>std::to_chars(value)</c>.
	/// </summary>
	internal static string ToHString(double value)
	{
		ulong bits = BitConverter.DoubleToUInt64Bits(value);
		if (double.IsFinite(value))
		{
			return FormatShortest(value.ToString("R", CultureInfo.InvariantCulture), value);
		}

		return FormatNonFinite((bits & 0x8000_0000_0000_0000ul) != 0, bits & 0x000F_FFFF_FFFF_FFFFul, 0x0008_0000_0000_0000ul);
	}

	private static string FormatNonFinite(bool isNegative, ulong mantissa, ulong quietBit)
	{
		var sign = isNegative ? "-" : "";
		if (mantissa == 0)
		{
			return sign + "inf";
		}

		if (isNegative && mantissa == quietBit)
		{
			return sign + "nan(ind)";
		}

		return sign + ((mantissa & quietBit) != 0 ? "nan" : "nan(snan)");
	}

	// std::to_chars(value) without a format: the shortest round-trip digits, in fixed or scientific
	// notation, whichever is shorter (fixed on a tie). Integral fixed output prints the exact value.
	private static string FormatShortest(string roundTrip, double exactValue)
	{
		var isNegative = roundTrip.StartsWith('-');
		var unsigned = isNegative ? roundTrip.Substring(1) : roundTrip;
		var sign = isNegative ? "-" : "";

		var exponentIndex = unsigned.IndexOf('E');
		var mantissa = exponentIndex >= 0 ? unsigned.Substring(0, exponentIndex) : unsigned;
		var exponent = exponentIndex >= 0 ? int.Parse(unsigned.Substring(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : 0;

		var pointIndex = mantissa.IndexOf('.');
		var integerDigitCount = pointIndex >= 0 ? pointIndex : mantissa.Length;
		var allDigits = mantissa.Replace(".", "");

		var leadingZeros = 0;
		while (leadingZeros < allDigits.Length && allDigits[leadingZeros] == '0')
		{
			leadingZeros++;
		}

		var digits = allDigits.Substring(leadingZeros).TrimEnd('0');
		if (digits.Length == 0)
		{
			return sign + "0";
		}

		// Decimal exponent of the first significant digit (d.ddd x 10^scientificExponent).
		var scientificExponent = integerDigitCount - leadingZeros - 1 + exponent;
		var digitCount = digits.Length;

		var absExponent = Math.Abs(scientificExponent);
		var scientificLength = digitCount + (digitCount > 1 ? 1 : 0) + 2 + Math.Max(2, absExponent.ToString(CultureInfo.InvariantCulture).Length);

		int fixedLength;
		if (scientificExponent >= 0)
		{
			fixedLength = scientificExponent + 1 >= digitCount ? scientificExponent + 1 : digitCount + 1;
		}
		else
		{
			fixedLength = 2 + (-scientificExponent - 1) + digitCount;
		}

		if (fixedLength <= scientificLength)
		{
			if (scientificExponent + 1 >= digitCount)
			{
				// Integral: MSVC prints the exact value rather than the shortest digits padded with zeros.
				return sign + new BigInteger(Math.Abs(exactValue)).ToString(CultureInfo.InvariantCulture);
			}

			if (scientificExponent >= 0)
			{
				return string.Concat(sign, digits.AsSpan(0, scientificExponent + 1), ".", digits.AsSpan(scientificExponent + 1));
			}

			return sign + "0." + new string('0', -scientificExponent - 1) + digits;
		}

		var scientificMantissa = digitCount > 1 ? string.Concat(digits.AsSpan(0, 1), ".", digits.AsSpan(1)) : digits;
		return sign + scientificMantissa + "e" + (scientificExponent < 0 ? "-" : "+") + absExponent.ToString("00", CultureInfo.InvariantCulture);
	}
#nullable restore
}
