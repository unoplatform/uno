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
	private static readonly Uri ProgressRingAsset =
		new("embedded://Uno.UI/Uno.UI.UI.Xaml.Controls.ProgressRing.ProgressRingIntdeterminate.json");

	[TestMethod]
	public async Task When_Playing_Does_Not_Subscribe_To_CompositionTarget_Rendering()
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

		// The Skia-side source must drive itself from its own paint callback rather than from the
		// global CompositionTarget.Rendering event — otherwise the Skia render loop is forced to run
		// at full FPS even when nothing else needs rendering.
		Assert.AreEqual(baseline, GetRenderingSubscriberCount(), "Player must not add a CompositionTarget.Rendering subscriber while playing.");
	}

	[TestMethod]
	public async Task When_Stop_Reports_Not_Playing()
	{
		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = true,
			Source = new LottieVisualSource { UriSource = ProgressRingAsset }
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		player.Stop();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Player should report not playing after Stop.");
	}

	private static int GetRenderingSubscriberCount()
	{
		var field = typeof(CompositionTarget).GetField("_rendering", BindingFlags.Static | BindingFlags.NonPublic);
		Assert.IsNotNull(field, "CompositionTarget._rendering field is expected to exist on Skia.");
		var value = field!.GetValue(null) as Delegate;
		return value?.GetInvocationList().Length ?? 0;
	}
}

#endif
