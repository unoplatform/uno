using System;
using System.Threading;
using Android.Runtime;
using Android.Views.Accessibility;
using AndroidX.Core.View.Accessibility;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Sets <c>AccessibilityNodeInfoCompat.checked</c> via JNI so the call works against any
/// AndroidX.Core binding loaded at runtime. The C# binding for <c>setChecked</c> changed
/// signature between AndroidX.Core 1.16 (<c>setChecked(boolean)</c>) and 1.17
/// (<c>setChecked(int)</c> with state constants), which means a build compiled against one
/// version throws <see cref="Java.Lang.NoSuchMethodError"/> at runtime when the consuming
/// app pulls in the other version (see unoplatform/uno#22999). Probing both Java signatures
/// here bypasses the managed property entirely.
///
/// Also supports <see cref="ToggleState.Indeterminate"/> ("mixed" state) via the
/// <c>setChecked(int)</c> path, falling back to unchecked on the boolean path because
/// <c>boolean</c> cannot represent three states.
/// </summary>
internal static class AccessibilityNodeInfoCompatJni
{
	private const string ClassName = "androidx/core/view/accessibility/AccessibilityNodeInfoCompat";

	// Mirror AccessibilityNodeInfoCompat.CHECKED_STATE_* (AndroidX.Core 1.17+).
	private const int CheckedStateFalse = 0;
	private const int CheckedStateTrue = 1;
	private const int CheckedStateMixed = 2; // Indeterminate / partially checked

	private static readonly object _initLock = new();
	private static bool _initialized;
	private static IntPtr _setCheckedIntId;
	private static IntPtr _setCheckedBoolId;

	/// <summary>
	/// Sets the checked state using a raw integer state value:
	/// <c>0</c> = unchecked, <c>1</c> = checked, <c>2</c> = indeterminate/mixed.
	/// Callers should map from <c>ToggleState</c> (Off=0, On=1, Indeterminate=2).
	/// On bindings that only expose the boolean overload, state=2 (Indeterminate)
	/// falls back to unchecked (false), preserving "not fully on" as the safe default.
	/// </summary>
	internal static bool SetChecked(AccessibilityNodeInfoCompat node, int checkState)
	{
		EnsureInitialized();

		if (_setCheckedIntId != IntPtr.Zero)
		{
			// AndroidX.Core 1.17+: setChecked(int) with full three-state support.
			JNIEnv.CallVoidMethod(node.Handle, _setCheckedIntId, new JValue(checkState));
			return true;
		}

		if (_setCheckedBoolId != IntPtr.Zero)
		{
			// AndroidX.Core 1.16 and earlier: setChecked(boolean).
			// Indeterminate (checkState == 2) degrades gracefully to false (unchecked).
			JNIEnv.CallVoidMethod(
				node.Handle,
				_setCheckedBoolId,
				new JValue(checkState == CheckedStateTrue));
		}

		if (checkState == CheckedStateMixed &&
			OperatingSystem.IsAndroidVersionAtLeast(36) &&
			node.Unwrap() is { } nativeNode)
		{
			nativeNode.CheckedState = CheckedState.Partial;
			return true;
		}

		return checkState != CheckedStateMixed;
	}

	internal static void SetChecked(AccessibilityNodeInfoCompat node, bool isChecked)
	{
		EnsureInitialized();

		if (_setCheckedIntId != IntPtr.Zero)
		{
			// AndroidX.Core 1.17+: setChecked(int state)
			JNIEnv.CallVoidMethod(
				node.Handle,
				_setCheckedIntId,
				new JValue(isChecked ? CheckedStateTrue : CheckedStateFalse));
		}
		else if (_setCheckedBoolId != IntPtr.Zero)
		{
			// AndroidX.Core 1.16 and earlier: setChecked(boolean)
			JNIEnv.CallVoidMethod(
				node.Handle,
				_setCheckedBoolId,
				new JValue(isChecked));
		}
		// If neither signature exists on the loaded binding, skip rather than
		// crash. This shouldn't happen in practice but keeps the helper safe by construction.
	}

	private static void EnsureInitialized()
	{
		if (Volatile.Read(ref _initialized))
		{
			return;
		}

		lock (_initLock)
		{
			if (_initialized)
			{
				return;
			}

			var classRef = JNIEnv.FindClass(ClassName);
			try
			{
				_setCheckedIntId = TryGetMethodId(classRef, "setChecked", "(I)V");
				_setCheckedBoolId = TryGetMethodId(classRef, "setChecked", "(Z)V");
			}
			finally
			{
				if (classRef != IntPtr.Zero)
				{
					JNIEnv.DeleteGlobalRef(classRef);
				}
			}

			if (_setCheckedIntId == IntPtr.Zero &&
				_setCheckedBoolId == IntPtr.Zero &&
				typeof(AccessibilityNodeInfoCompatJni).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(AccessibilityNodeInfoCompatJni).Log().Warn(
					"[A11y] AndroidX AccessibilityNodeInfoCompat exposes no supported setChecked signature.");
			}

			Volatile.Write(ref _initialized, true);
		}
	}

	private static IntPtr TryGetMethodId(IntPtr classRef, string name, string signature)
	{
		try
		{
			return JNIEnv.GetMethodID(classRef, name, signature);
		}
		catch (Java.Lang.NoSuchMethodError)
		{
			return IntPtr.Zero;
		}
	}
}
