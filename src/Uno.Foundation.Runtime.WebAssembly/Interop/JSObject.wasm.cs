using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Foundation.Interop
{
	[Obfuscation(Feature = "renaming", Exclude = true)]
	public sealed class JSObject
	{
		private static readonly Func<string, IntPtr> _strToIntPtr =
			Marshal.SizeOf<IntPtr>() == 4
				? (s => (IntPtr)int.Parse(s))
				: (Func<string, IntPtr>)(s => (IntPtr)long.Parse(s));

		/// <summary>
		/// Used by javascript to dispatch a method call to the managed object at <paramref name="handlePtr"/>.
		/// </summary>
		[Obfuscation(Feature = "renaming", Exclude = true)]
		public static void Dispatch(string handlePtr, string method, string parameters)
		{
			var intPtr = _strToIntPtr(handlePtr);
			var handle = GCHandle.FromIntPtr(intPtr);

			if (!handle.IsAllocated)
			{
				handle.Log().Debug($"Cannot invoke '{method}' as target has been collected!");
				return;
			}

			if (!(handle.Target is JSObjectHandle jsObjectHandle))
			{
				handle.Log().Debug($"Cannot invoke '{method}' as target is not a valid JSObjectHandle! ({handle.Target?.GetType()})");
				return;
			}

			if (!jsObjectHandle.TryGetManaged(out var target))
			{
				jsObjectHandle.Log().Debug($"Cannot invoke '{method}' as target has been collected!");
				return;
			}

			jsObjectHandle.Metadata.InvokeManaged(target, method, parameters);
		}
	}
}
