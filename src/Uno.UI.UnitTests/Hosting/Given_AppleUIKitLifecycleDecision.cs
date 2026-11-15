#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.AppleUIKit;

namespace Uno.UI.Tests.Hosting;

/// <summary>
/// Tests for <see cref="AppleUIKitLifecycleDecision"/>, the pure part of picking an Apple UIKit app's lifecycle
/// (reading the app's Info.plist needs an app bundle).
/// </summary>
[UITestClass]
public class Given_AppleUIKitLifecycleDecision
{
	[TestMethod]
	[DataRow(null, "26.0")]
	[DataRow(true, "27.1")]
	[DataRow(false, "27.1")]
	[DataRow(false, "")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Scene_Manifest_Then_Manifest_Scene(bool? useSceneLifecycle, string sdkVersion)
		=> AssertDecision(hasSceneManifest: true, useSceneLifecycle, sdkVersion, AppleUIKitLifecycleMode.ManifestScene, AppleUIKitLifecycleDecidedBy.SceneManifest);

	[TestMethod]
	[DataRow("26.0")]
	[DataRow("")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_UseSceneLifecycle_True_Then_Adopted_Scene(string sdkVersion)
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: true, sdkVersion, AppleUIKitLifecycleMode.AdoptedScene, AppleUIKitLifecycleDecidedBy.UseSceneLifecycleKey);

	[TestMethod]
	[DataRow("27.1")]
	[DataRow("")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_UseSceneLifecycle_False_Then_App(string sdkVersion)
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: false, sdkVersion, AppleUIKitLifecycleMode.App, AppleUIKitLifecycleDecidedBy.UseSceneLifecycleKey);

	[TestMethod]
	[DataRow("18.0")]
	[DataRow("26.5")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Sdk_Before_27_Then_App(string sdkVersion)
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: null, sdkVersion, AppleUIKitLifecycleMode.App, AppleUIKitLifecycleDecidedBy.SdkVersion);

	[TestMethod]
	[DataRow("27")]
	[DataRow("27.1")]
	[DataRow("28.0.1")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Sdk_27_Or_Later_Then_Adopted_Scene(string sdkVersion)
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: null, sdkVersion, AppleUIKitLifecycleMode.AdoptedScene, AppleUIKitLifecycleDecidedBy.SdkVersion);

	[TestMethod]
	[DataRow("")]
	[DataRow("abc")]
	[DataRow("-27")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Sdk_Version_Unreadable_Then_Adopted_Scene(string sdkVersion)
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: null, sdkVersion, AppleUIKitLifecycleMode.AdoptedScene, AppleUIKitLifecycleDecidedBy.UnknownSdkVersion);

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25510")]
	public void When_Sdk_Version_Missing_Then_Adopted_Scene()
		=> AssertDecision(hasSceneManifest: false, useSceneLifecycle: null, sdkVersion: null, AppleUIKitLifecycleMode.AdoptedScene, AppleUIKitLifecycleDecidedBy.UnknownSdkVersion);

	private static void AssertDecision(
		bool hasSceneManifest,
		bool? useSceneLifecycle,
		string? sdkVersion,
		AppleUIKitLifecycleMode expectedMode,
		AppleUIKitLifecycleDecidedBy expectedDecidedBy)
	{
		var (mode, decidedBy) = AppleUIKitLifecycleDecision.Decide(hasSceneManifest, useSceneLifecycle, sdkVersion);

		Assert.AreEqual(expectedMode, mode);
		Assert.AreEqual(expectedDecidedBy, decidedBy);
	}
}
