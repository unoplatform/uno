using System;

namespace Windows.UI.Xaml.Automation
{
	public class ElementNotEnabledException : Exception
	{
		public ElementNotEnabledException() : base()
		{
		}

		public ElementNotEnabledException(string message) : base(message)
		{
		}

		public ElementNotEnabledException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
