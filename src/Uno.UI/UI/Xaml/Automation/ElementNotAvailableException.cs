using System;

namespace Microsoft.UI.Xaml.Automation
{
	public class ElementNotAvailableException : Exception
	{
		public ElementNotAvailableException()
		{
		}

		public ElementNotAvailableException(string message) : base(message)
		{
		}

		public ElementNotAvailableException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
