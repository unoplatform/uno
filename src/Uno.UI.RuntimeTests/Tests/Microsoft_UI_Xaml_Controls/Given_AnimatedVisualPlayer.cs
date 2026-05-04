#nullable enable

#if __SKIA__

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass, RunsOnUIThread]
public class Given_AnimatedVisualPlayer
{
	// Embedded Lottie that ships with Uno.UI for ProgressRing — we reuse it to avoid having to add
	// a dedicated test asset.
	private static readonly Uri ProgressRingAsset =
		new("embedded://Uno.UI/Uno.UI.UI.Xaml.Controls.ProgressRing.ProgressRingIntdeterminate.json");

	[TestMethod]
	public async Task When_Stop_Removes_Rendering_Subscription()
	{
		var baseline = GetRenderingSubscriberCount();

		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = true,
			Source = new LottieVisualSource { UriSource = ProgressRingAsset }
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		Assert.IsTrue(GetRenderingSubscriberCount() > baseline, "CompositionTarget.Rendering should have an additional subscriber while playing.");

		player.Stop();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Player should report not playing after Stop.");
		Assert.AreEqual(baseline, GetRenderingSubscriberCount(), "CompositionTarget.Rendering subscription must be removed after Stop.");
	}

	[TestMethod]
	public async Task When_Hidden_Pauses_And_Removes_Rendering_Subscription()
	{
		var baseline = GetRenderingSubscriberCount();

		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = true,
			Source = new LottieVisualSource { UriSource = ProgressRingAsset }
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		Assert.IsTrue(GetRenderingSubscriberCount() > baseline, "Should have a subscriber while playing and visible.");

		player.Visibility = Visibility.Collapsed;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Player should report not playing while collapsed.");
		Assert.AreEqual(baseline, GetRenderingSubscriberCount(), "Subscription should be removed when player is hidden.");
	}

	[TestMethod]
	public async Task When_Unhidden_Resumes_Playback()
	{
		var baseline = GetRenderingSubscriberCount();

		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = true,
			Source = new LottieVisualSource { UriSource = ProgressRingAsset }
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		player.Visibility = Visibility.Collapsed;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(player.IsPlaying, "Pre-condition: player should be paused while collapsed.");

		player.Visibility = Visibility.Visible;
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should resume after becoming visible.");

		Assert.IsTrue(GetRenderingSubscriberCount() > baseline, "Subscription should be re-added when player becomes visible.");
	}

	[TestMethod]
	public async Task When_Stopped_While_Hidden_Stays_Stopped_When_Unhidden()
	{
		var baseline = GetRenderingSubscriberCount();

		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = true,
			Source = new LottieVisualSource { UriSource = ProgressRingAsset }
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		player.Visibility = Visibility.Collapsed;
		await TestServices.WindowHelper.WaitForIdle();

		// User explicitly stops while hidden — when we become visible again, we must not auto-resume.
		player.Stop();
		await TestServices.WindowHelper.WaitForIdle();

		player.Visibility = Visibility.Visible;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Explicit Stop must not be reversed by visibility transitions.");
		Assert.AreEqual(baseline, GetRenderingSubscriberCount(), "Subscription must remain removed after explicit Stop.");
	}

	private static int GetRenderingSubscriberCount()
	{
		// CompositionTarget._rendering is a private static event. Reflect into it to count subscribers
		// — the subscription is what drives the per-frame loop on Skia.
		var field = typeof(CompositionTarget).GetField("_rendering", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(field, "CompositionTarget._rendering field is expected to exist on Skia.");
		var value = field!.GetValue(null) as Delegate;
		return value?.GetInvocationList().Length ?? 0;
	}
}

#endif
