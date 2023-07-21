using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
// using Uno.Logging;

namespace Uno.Foundation.Interop
{
	[Obfuscation(Feature = "renaming", Exclude = true)]
	public sealed partial class JSObject
	{
		/// <summary>
		/// Used by javascript to dispatch a method call to the managed object at <paramref name="handlePtr"/>.
		/// </summary>
		[JSExport]
		[Obfuscation(Feature = "renaming", Exclude = true)]
		public static void Dispatch(IntPtr handlePtr, string method, string parameters)
		{
			var handle = GCHandle.FromIntPtr(handlePtr);

			if (!handle.IsAllocated)
			{
				//handle.Log().Debug($"Cannot invoke '{method}' as target has been collected!");
				return;
			}

			if (!(handle.Target is JSObjectHandle jsObjectHandle))
			{
				//handle.Log().Debug($"Cannot invoke '{method}' as target is not a valid JSObjectHandle! ({handle.Target?.GetType()})");
				return;
			}

			if (!jsObjectHandle.TryGetManaged(out var target))
			{
				//jsObjectHandle.Log().Debug($"Cannot invoke '{method}' as target has been collected!");
				return;
			}

			jsObjectHandle.Metadata.InvokeManaged(target, method, parameters);
		}
	}
}
