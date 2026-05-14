using System;
using System.Threading;
using Android.Runtime;
using AndroidX.Core.View.Accessibility;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Sets <c>AccessibilityNodeInfoCompat.checked</c> via JNI so the call works against any
/// AndroidX.Core binding loaded at runtime. The C# binding for <c>setChecked</c> changed
/// signature between AndroidX.Core 1.16 (<c>setChecked(boolean)</c>) and 1.17
/// (<c>setChecked(int)</c> with state constants), which means a build compiled against one
/// version throws <see cref="Java.Lang.NoSuchMethodError"/> at runtime when the consuming
/// app pulls in the other version (see unoplatform/uno#22999). Probing both Java signatures
/// here bypasses the managed property entirely.
/// </summary>
internal static class AccessibilityNodeInfoCompatJni
{
	private const string ClassName = "androidx/core/view/accessibility/AccessibilityNodeInfoCompat";

	// Mirror AccessibilityNodeInfoCompat.CHECKED_STATE_* (AndroidX.Core 1.17+).
	private const int CheckedStateFalse = 0;
	private const int CheckedStateTrue = 1;

	private static readonly object _initLock = new();
	private static bool _initialized;
	private static IntPtr _setCheckedIntId;
	private static IntPtr _setCheckedBoolId;

	public static void SetChecked(AccessibilityNodeInfoCompat node, bool isChecked)
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
		// else: neither signature exists on the loaded binding — skip silently rather than
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
			_setCheckedIntId = TryGetMethodId(classRef, "setChecked", "(I)V");
			_setCheckedBoolId = TryGetMethodId(classRef, "setChecked", "(Z)V");

			Volatile.Write(ref _initialized, true);
		}
	}

	private static IntPtr TryGetMethodId(IntPtr classRef, string name, string signature)
	{
		try
		{
			return JNIEnv.GetMethodID(classRef, name, signature);
		}
		catch (Java.Lang.Throwable)
		{
			return IntPtr.Zero;
		}
	}
}
