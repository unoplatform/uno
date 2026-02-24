using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Uno.UI.RemoteControl.Host.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class ConfigurationExtensionsTests
{
	[TestMethod]
	[Description("Empty string means 'no add-ins' — must not collapse to null")]
	public void GetAddinsValue_WhenEmptyString_ReturnsEmptyNotNull()
	{
		var config = BuildConfig(("addins", ""));

		config.GetAddinsValue("addins").Should().Be("");
	}

	[TestMethod]
	[Description("When the key is absent, returns null (triggers MSBuild fallback)")]
	public void GetAddinsValue_WhenNotPresent_ReturnsNull()
	{
		var config = BuildConfig();

		config.GetAddinsValue("addins").Should().BeNull();
	}

	[TestMethod]
	[Description("When a value is present, it is trimmed")]
	public void GetAddinsValue_WhenHasValue_ReturnsTrimmed()
	{
		var config = BuildConfig(("addins", " a.dll;b.dll "));

		config.GetAddinsValue("addins").Should().Be("a.dll;b.dll");
	}

	private static IConfiguration BuildConfig(params (string key, string value)[] pairs)
	{
		var dict = new Dictionary<string, string?>();
		foreach (var (key, value) in pairs)
		{
			dict[key] = value;
		}

		return new ConfigurationBuilder()
			.AddInMemoryCollection(dict)
			.Build();
	}
}
