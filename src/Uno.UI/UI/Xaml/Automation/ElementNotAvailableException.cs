using System;

namespace Microsoft.UI.Xaml.Automation
{
	public partial class ElementNotAvailableException : Exception
	{
		private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

		public ElementNotAvailableException()
		{
			HResult = UIA_E_ELEMENTNOTAVAILABLE;
		}

		public ElementNotAvailableException(string message) : base(message)
		{
			HResult = UIA_E_ELEMENTNOTAVAILABLE;
		}

		public ElementNotAvailableException(string message, Exception innerException) : base(message, innerException)
		{
			HResult = UIA_E_ELEMENTNOTAVAILABLE;
		}
	}
}
