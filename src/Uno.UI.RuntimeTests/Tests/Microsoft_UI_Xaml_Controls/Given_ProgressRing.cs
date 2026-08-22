using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass, RunsOnUIThread]
public class Given_ProgressRing
{
	[TestMethod]
#if !(WINAPPSDK || __SKIA__ || __WASM__)
	[Ignore("IAnimatedVisualSource is not implemented")]
#endif
	public async Task When_NoProgress_IsIndeterminate_Toggle()
	{
		var SUT = new ProgressRing { Width = 20, Height = 20 };
		try
		{
			(SUT.Minimum, SUT.Value, SUT.Maximum) = (0, 0, 100);
			SUT.IsActive = true;
			SUT.IsIndeterminate = false;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			var player = SUT.FindFirstDescendant<AnimatedVisualPlayer>(x => x.Name == "LottiePlayer");
			if (player is null)
			{
				// The test may be invalid due to template change.
				Assert.IsNotNull(player, "Failed to find template part: AnimatedVisualPlayer#LottiePlayer");
			}

			SUT.IsIndeterminate = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(player.IsPlaying, "LottiePlayer should be playing.");

			SUT.IsIndeterminate = false;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(player.IsPlaying, "LottiePlayer should have stopped playing.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}

	[TestMethod]
#if !(WINAPPSDK || __SKIA__ || __WASM__)
	[Ignore("IAnimatedVisualSource is not implemented")]
#endif
	public async Task When_HalfProgress_IsIndeterminate_Toggle()
	{
		var SUT = new ProgressRing { Width = 20, Height = 20 };
		try
		{
			(SUT.Minimum, SUT.Value, SUT.Maximum) = (0, 50, 100);
			SUT.IsActive = true;
			SUT.IsIndeterminate = false;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			var player = SUT.FindFirstDescendant<AnimatedVisualPlayer>(x => x.Name == "LottiePlayer");
			if (player is null)
			{
				// The test may be invalid due to template change.
				Assert.IsNotNull(player, "Failed to find template part: AnimatedVisualPlayer#LottiePlayer");
			}

			SUT.IsIndeterminate = true;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(player.IsPlaying, "LottiePlayer should be playing.");

			// note: unlike in When_NoProgress_IsIndeterminate_Toggle, the progress was (and still is) at 50%.
			// so upon entering "Determinate" again, it (should animate 0->50%)? before coming to a stop.
			SUT.IsIndeterminate = false;
			await TestServices.WindowHelper.WaitForIdle();
			//Assert.IsTrue(player.IsPlaying, "LottiePlayer should be animating briefly from 0% to 50%."); // not the case for windows, but this is animated on uno
			await TestServices.WindowHelper.WaitFor(() => player.IsPlaying == false, timeoutMS: 2000, "LottiePlayer should be eventually stop playing.");
		}
		finally
		{
			SUT.IsActive = false;
		}
	}
}
