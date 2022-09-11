#nullable enable

namespace Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using System;
	using System.Runtime.CompilerServices;

	public class LoggingAdapter
	{
		// [ModuleInitializer]
		public static void Initialize()
		{
			Foundation.Logging.LoggerFactory.ExternalLoggerFactory = new MicrosoftLoggerFactory();
		}
	}
}

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	internal sealed class ModuleInitializerAttribute : System.Attribute { }
}
#endif
