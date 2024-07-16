using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk.Tasks;

public abstract class SdkTask : Task, ISdkTask
{
	public bool SdkDebugging { get; set; }

	public sealed override bool Execute()
	{
		try
		{
			ExecuteInternal();
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);

			if (SdkDebugging)
			{
				Log.LogMessage(MessageImportance.High, ex.ToString());
			}
		}

		return !Log.HasLoggedErrors;
	}

	protected abstract void ExecuteInternal();

	private MessageImportance DefaultImportance => SdkDebugging ? MessageImportance.High : MessageImportance.Normal;

	public void LogMessage(string message, params object[] arguments)
	{
		if (string.IsNullOrEmpty(message))
		{
			message = string.Empty;
		}

		Log.LogMessage(DefaultImportance, message, arguments);
	}

	public void LogError(Exception exception)
	{
		Log.LogError(exception.Message);

		if (SdkDebugging)
		{
			Log.LogError(exception.ToString());
		}
	}

	public void LogError(string message, params object[] arguments)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		Log.LogError(message, arguments);
	}
}
