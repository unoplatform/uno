#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationArgumentCodec
{
	[TestMethod]
	public void When_Arguments_Contain_Reserved_Characters()
	{
		var arguments = new Dictionary<string, string>
		{
			["&;\"='%<>"] = string.Empty,
			["&\"'<>"] = ";=%"
		};

		var encoded = AppNotificationArgumentCodec.Encode(arguments);
		Assert.AreEqual("&amp;%3B&quot;%3D&apos;%25&lt;&gt;;&amp;&quot;&apos;&lt;&gt;=%3B%3D%25", encoded);

		var decoded = AppNotificationArgumentCodec.Decode("&%3B\"%3D'%25<>;&\"'<>=%3B%3D%25");

		Assert.AreEqual(string.Empty, decoded["&;\"='%<>"]);
		Assert.AreEqual(";=%", decoded["&\"'<>"]);
	}

	[TestMethod]
	public void When_Decoding_Bare_And_Unknown_Escapes()
	{
		var decoded = AppNotificationArgumentCodec.Decode("key%3B;lower%3b;unknown%2F=value%25");

		Assert.AreEqual(string.Empty, decoded["key;"]);
		Assert.AreEqual(string.Empty, decoded["lower%3b"]);
		Assert.AreEqual("value%", decoded["unknown%2F"]);
	}

	[TestMethod]
	public void When_Duplicate_Keys_Are_Encoded_Entries_Are_Preserved()
	{
		var arguments = new[]
		{
			new KeyValuePair<string, string>("key", "first"),
			new KeyValuePair<string, string>("key", "second"),
		};

		var encoded = AppNotificationArgumentCodec.Encode(arguments);

		Assert.AreEqual("key=first;key=second", encoded);
	}
}
