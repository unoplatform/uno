using System;

namespace Microsoft.UI.Xaml.Markup;

public partial class XamlParseException : Exception
{
	public XamlParseException() : base()
	{
	}

	public XamlParseException(string message) : base(message)
	{
	}

	public XamlParseException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
