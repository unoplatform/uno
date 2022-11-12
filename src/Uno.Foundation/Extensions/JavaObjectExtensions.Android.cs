using System.Runtime.CompilerServices;
using System.Text;
using Android.Runtime;
using System;
using Uno.Foundation.Logging;
using System.ComponentModel;

namespace Uno.Extensions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JavaObjectExtensions
{
	/// <summary>
	/// Runs the specified action if the native Java instance of the <paramref name="instance"/> is still available.
	/// </summary>
	/// <typeparam name="T">An <see cref="IJavaObject"/> instance.</typeparam>
	/// <param name="instance">The .NET instance to check</param>
	/// <param name="action">The action to execute if both the .NET instance and Java instance are available.</param>
	public static void RunIfNativeInstanceAvailable<T>(
		this T instance,
		Action<T> action,
		[CallerMemberName]string member = null,
		[CallerLineNumber]int line = 0,
		[CallerFilePath]string filePath = null
	) where T : IJavaObject
	{
		if (instance.Handle != IntPtr.Zero)
		{
			action(instance);
		}
		else
		{
			if (instance.Log().IsEnabled(LogLevel.Warning))
			{
				instance.Log().Warn(
					string.Format("Native invocation discarded for {0} at {1}:{2} ({3}). The object may not have been disposed properly by its owner."
					, instance.GetType()
					, member
					, line
					, filePath)
				);
			}
		}
	}
}
