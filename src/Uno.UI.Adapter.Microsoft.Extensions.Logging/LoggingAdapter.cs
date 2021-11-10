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
			Uno.Foundation.Logging.LoggerFactory.ExternalLoggerFactory = new MicrosoftLoggerFactory();
		}
	}
}

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class ModuleInitializerAttribute : System.Attribute { }
}
