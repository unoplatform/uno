namespace Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using Uno.Foundation.Logging;

	class MicrosoftLoggerFactory : Foundation.Logging.IExternalLoggerFactory
	{
		public IExternalLogger CreateLogger(string categoryName) => new MicrosoftLogger(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(categoryName));
	}
}
