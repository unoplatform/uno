using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	public class XamlLoadError : IMessage
	{
		public const string Name = nameof(XamlLoadError);

		public XamlLoadError(string filePath, string message, string stackTrace, string exceptionType)
		{
			FilePath = filePath;
			Message = message;
			ExceptionType = exceptionType;
			StackTrace = stackTrace;
		}

		public string Scope => HotReloadConstants.ScopeName;

		string IMessage.Name => Name;

		public string FilePath { get; }

		public string ExceptionType { get; }

		public string Message { get; }

		public string StackTrace { get; }
	}
}
