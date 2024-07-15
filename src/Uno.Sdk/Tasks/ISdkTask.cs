using System;

namespace Uno.Sdk.Tasks;

internal interface ISdkTask
{
	void LogMessage(string message, params object[] arguments);

	void LogError(Exception exception);

	void LogError(string message, params object[] arguments);
}
