#nullable enable

using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class TextInputContentTests
{
	[TestMethod]
	[DataRow(InputScopeNameValue.Default, 0u)]
	[DataRow(InputScopeNameValue.Url, 5u)]
	[DataRow(InputScopeNameValue.EmailSmtpAddress, 6u)]
	[DataRow(InputScopeNameValue.PersonalFullName, 7u)]
	[DataRow(InputScopeNameValue.Digits, 2u)]
	[DataRow(InputScopeNameValue.Number, 3u)]
	[DataRow(InputScopeNameValue.TelephoneNumber, 4u)]
	[DataRow(InputScopeNameValue.DateYear, 10u)]
	[DataRow(InputScopeNameValue.TimeHour, 11u)]
	public void InputScopeMapsToPurpose(InputScopeNameValue scope, uint purpose)
	{
		Assert.AreEqual((0u, purpose), WaylandTextInputContent.Get(scope, false, false, false));
	}

	[TestMethod]
	public void ConfigurationChangesRefreshHints()
	{
		Assert.AreEqual((0u, 0u), WaylandTextInputContent.Get(InputScopeNameValue.Default, false, false, false));
		Assert.AreEqual((0x203u, 0u), WaylandTextInputContent.Get(InputScopeNameValue.Default, true, true, true));
		Assert.AreEqual((1u, 0u), WaylandTextInputContent.Get(InputScopeNameValue.Default, true, false, false));
		Assert.AreEqual((2u, 0u), WaylandTextInputContent.Get(InputScopeNameValue.Default, false, true, false));
	}

	[TestMethod]
	public void SecretsDisableSuggestionsAndRequestPrivacy()
	{
		Assert.AreEqual((0xc0u, 8u), WaylandTextInputContent.Get(InputScopeNameValue.Password, true, true, false));
		Assert.AreEqual((0xc0u, 9u), WaylandTextInputContent.Get(InputScopeNameValue.NumericPin, true, true, false));
	}
}
