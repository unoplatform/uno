using System;

namespace Microsoft.UI.Xaml;

public partial class LayoutCycleException : Exception
{
	public LayoutCycleException() : base()
	{
	}

	public LayoutCycleException(string message) : base(message)
	{
	}

	public LayoutCycleException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
