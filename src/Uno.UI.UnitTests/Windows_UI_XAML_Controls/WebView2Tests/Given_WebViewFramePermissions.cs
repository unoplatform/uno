#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_WebViewFramePermissions
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Default_Policy()
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes(FeatureConfiguration.WebView2.PermissionsPolicy);

		Assert.AreEqual("autoplay *; encrypted-media *; clipboard-write *", allow);
		Assert.IsTrue(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Feature_Alone_Then_Every_Origin()
	{
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(["camera"]);

		Assert.AreEqual("camera *", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Feature_With_Allowlist_Then_Written_As_Is()
	{
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(["camera https://a.example 'self'", "autoplay 'none'"]);

		Assert.AreEqual("camera https://a.example 'self'; autoplay 'none'", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	[DataRow("camera\thttps://a.example")]
	[DataRow("camera\nhttps://a.example")]
	[DataRow("camera\fhttps://a.example")]
	[DataRow("camera\rhttps://a.example")]
	[DataRow("  camera   https://a.example  ")]
	public void When_Separated_By_Ascii_Whitespace_Then_Single_Spaces(string entry)
	{
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes([entry]);

		Assert.AreEqual("camera https://a.example", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Other_Whitespace_Then_Not_Split()
	{
		// The browser splits only on ASCII whitespace, so a no-break space stays part of the feature name.
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(["camera\u00A0https://a.example"]);

		Assert.AreEqual("camera\u00A0https://a.example *", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Entry_Holds_Several_Directives_Then_Split_On_Semicolons()
	{
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(["camera; microphone https://a.example;;geolocation"]);

		Assert.AreEqual("camera *; microphone https://a.example; geolocation *", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Null_Or_Empty_Entries_Then_Skipped()
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes([null, "", " ", ";", " ; \t", "camera"]);

		Assert.AreEqual("camera *", allow);
		Assert.IsFalse(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_No_Directive_Then_No_Attribute()
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes([null, " "]);

		Assert.IsNull(allow);
		Assert.IsFalse(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Feature_Repeated_Then_Later_Entry_Replaces_Earlier()
	{
		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(["autoplay", "camera", "autoplay 'none'"]);

		Assert.AreEqual("autoplay 'none'; camera *", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Default_Feature_Added_Again_Then_Added_Entry_Wins()
	{
		var policy = new List<string?>(FeatureConfiguration.WebView2.PermissionsPolicy) { "autoplay 'none'" };

		var (allow, _) = WebViewFramePermissions.GetFrameAttributes(policy);

		Assert.AreEqual("autoplay 'none'; encrypted-media *; clipboard-write *", allow);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	[DataRow("fullscreen")]
	[DataRow("fullscreen *")]
	[DataRow("fullscreen https://a.example *")]
	public void When_Fullscreen_For_Every_Origin_Then_Only_AllowFullscreen(string entry)
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes(["autoplay", entry]);

		Assert.AreEqual("autoplay *", allow);
		Assert.IsTrue(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	[DataRow("fullscreen https://a.example")]
	[DataRow("fullscreen 'self'")]
	[DataRow("fullscreen 'none'")]
	public void When_Fullscreen_Restricted_Then_No_AllowFullscreen(string entry)
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes([entry]);

		Assert.AreEqual(entry, allow);
		Assert.IsFalse(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Fullscreen_Blocked_After_Allowed_Then_No_AllowFullscreen()
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes(["fullscreen", "fullscreen 'none'"]);

		Assert.AreEqual("fullscreen 'none'", allow);
		Assert.IsFalse(allowFullscreen);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25739")]
	public void When_Fullscreen_Allowed_After_Blocked_Then_AllowFullscreen()
	{
		var (allow, allowFullscreen) = WebViewFramePermissions.GetFrameAttributes(["fullscreen 'none'", "fullscreen"]);

		Assert.IsNull(allow);
		Assert.IsTrue(allowFullscreen);
	}
}
