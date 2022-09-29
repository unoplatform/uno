namespace Uno.Foundation.Logging
{
	internal interface IExternalLoggerFactory
	{
		IExternalLogger CreateLogger(string categoryName);
	}
}
