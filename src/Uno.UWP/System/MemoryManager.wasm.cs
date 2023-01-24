using System;
using System.Globalization;
using Uno.Foundation;

namespace Windows.System
{
	public partial class MemoryManager
	{
		private const string JsType = "Windows.System.MemoryManager";

		static MemoryManager()
		{
			IsAvailable = true;
		}

		public static ulong AppMemoryUsage
		{
			get
			{
				if (ulong.TryParse(WebAssemblyRuntime.InvokeJS(
					$"{JsType}.getAppMemoryUsage()"),
					NumberStyles.Any,
					CultureInfo.InvariantCulture, out var value))
				{
					return value;
				}

				throw new Exception($"getAppMemoryUsage returned an unsupported value");
			}
		}

		public static ulong AppMemoryUsageLimit
		{
			get
			{
				if (Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_EMSCRIPTEN_MAXIMUM_MEMORY") == "4GB")
				{
					return 4ul * 1024 * 1024 * 1024;
				}

				return 2ul * 1024 * 1024 * 1024;
			}
		}
	}
}
