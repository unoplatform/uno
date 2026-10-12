#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_ElementNotAvailableException
{
	private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25239")]
	public void When_Constructed_HResult_Is_ElementNotAvailable()
	{
		var innerException = new InvalidOperationException();
		var exceptions = new ElementNotAvailableException[]
		{
			new(),
			new("Element is unavailable"),
			new("Element is unavailable", innerException),
		};

		foreach (var exception in exceptions)
		{
			Assert.AreEqual(UIA_E_ELEMENTNOTAVAILABLE, exception.HResult);
		}
	}
}
