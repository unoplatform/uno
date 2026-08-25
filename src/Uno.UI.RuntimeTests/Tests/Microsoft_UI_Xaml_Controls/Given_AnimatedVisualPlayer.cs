#nullable enable

#if __SKIA__

using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Storage;

#if HAS_UNO_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_AnimatedVisualPlayer
{
	[TestCleanup]
	public void Cleanup()
	{
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_Stop_Reports_Not_Playing()
	{
		var player = CreatePlayer(autoPlay: true);

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");

		player.Stop();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Player should report not playing after Stop.");
	}

	[TestMethod]
	public async Task When_Stop_Restores_FromProgress()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(800));

		await UITestHelper.Load(player);

		var playTask = player.PlayAsync(0.35, 1.0, false).AsTask();
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "Player should start playing.");
		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) > 0.45, timeoutMS: 2000, "Player should advance beyond the start progress.");

		player.Stop();
		await playTask;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.35, GetPlayerProgress(player), 0.05, "Stop should restore the most recent play's from-progress.");
	}

	[TestMethod]
	public async Task When_Preempted_Play_Completes_Previous_Task()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(800));

		await UITestHelper.Load(player);

		var firstPlay = player.PlayAsync(0.2, 1.0, false).AsTask();
		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) > 0.3, timeoutMS: 2000, "First play should start advancing.");

		var secondPlay = player.PlayAsync(0.6, 1.0, false).AsTask();

		var completed = await Task.WhenAny(firstPlay, Task.Delay(TimeSpan.FromSeconds(2)));
		Assert.AreSame(firstPlay, completed, "Preempting a play should complete the previous PlayAsync task.");

		await secondPlay;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "Player should stop after the replacement play completes.");
		Assert.AreEqual(1.0, GetPlayerProgress(player), 0.05, "Replacement play should finish at its target progress.");
	}

	[TestMethod]
	public async Task When_To_Zero_Keyframe_Animation_Completes_At_One()
	{
		var player = CreatePlayer();

		await UITestHelper.Load(player);
		await player.PlayAsync(0.35, 0, false);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1.0, GetPlayerProgress(player), 0.05, "Playing from 0.35 to 0 should map to [0.35..1.0].");
	}

	[TestMethod]
	public async Task When_Around_The_End_Animation_Completes_At_Target_Progress()
	{
		var player = CreatePlayer();

		await UITestHelper.Load(player);
		await player.PlayAsync(0.35, 0.3, false);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.3, GetPlayerProgress(player), 0.05, "Playing from 0.35 to 0.3 should wrap and finish at 0.3.");
	}

	[TestMethod]
	public async Task When_From_One_Keyframe_Animation_Completes_At_Target_Progress()
	{
		var player = CreatePlayer();

		await UITestHelper.Load(player);
		await player.PlayAsync(1, 0.35, false);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.35, GetPlayerProgress(player), 0.05, "Playing from 1 to 0.35 should map to [0..0.35].");
	}

	[TestMethod]
	public async Task When_Reverse_Negative_PlaybackRate_Animation_Completes_At_One()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(400));

		await UITestHelper.Load(player);

		player.PlaybackRate = -1;
		var playTask = player.PlayAsync(0, 1, false).AsTask();
		var reverseTask = DelayForHalfAnimationDurationThenReverse(player);

		await Task.WhenAll(playTask, reverseTask);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1.0, GetPlayerProgress(player), 0.05, "Changing PlaybackRate from negative to positive mid-play should finish at 1.");
	}

	[TestMethod]
	public async Task When_Negative_PlaybackRate_Moves_Progress_Backward()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(400));

		await UITestHelper.Load(player);

		player.PlaybackRate = -1;
		var playTask = player.PlayAsync(0, 1, false).AsTask();

		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) < 0.8, timeoutMS: 2000, "Negative playback should move the player's Progress value backward from 1.");

		player.Stop();
		await playTask;
	}

	[TestMethod]
	public async Task When_Reverse_Positive_PlaybackRate_Animation_Completes_At_Zero()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(400));

		await UITestHelper.Load(player);

		player.PlaybackRate = 1;
		var playTask = player.PlayAsync(0, 1, false).AsTask();
		var reverseTask = DelayForHalfAnimationDurationThenReverse(player);

		await Task.WhenAll(playTask, reverseTask);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.0, GetPlayerProgress(player), 0.05, "Changing PlaybackRate from positive to negative mid-play should finish at 0.");
	}

	[TestMethod]
	public async Task When_PlaybackRate_Zero_Keeps_Progress_Stable()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(800));

		await UITestHelper.Load(player);

		var playTask = player.PlayAsync(0, 1, true).AsTask();
		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) > 0.2, timeoutMS: 2000, "Looped play should advance before freezing.");

		player.PlaybackRate = 0;
		var frozenProgress = GetPlayerProgress(player);

		await Task.Delay(TimeSpan.FromMilliseconds(200));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(frozenProgress, GetPlayerProgress(player), 0.02, "A zero playback rate should freeze the current progress.");
		Assert.IsTrue(player.IsPlaying, "The player should remain in the playing state while playback rate is zero.");

		player.Stop();
		await playTask;
	}

	[TestMethod]
	public async Task When_Pause_And_Resume_Preserve_Progress()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(800));

		await UITestHelper.Load(player);

		var playTask = player.PlayAsync(0, 1, false).AsTask();
		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) > 0.2, timeoutMS: 2000, "Player should advance before pausing.");

		player.Pause();
		var pausedProgress = GetPlayerProgress(player);

		await Task.Delay(TimeSpan.FromMilliseconds(200));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(pausedProgress, GetPlayerProgress(player), 0.02, "Pause should keep progress stable.");

		player.Resume();
		await TestServices.WindowHelper.WaitFor(() => GetPlayerProgress(player) > pausedProgress + 0.1, timeoutMS: 2000, "Resume should allow the animation to continue.");

		player.Stop();
		await playTask;
	}

	[TestMethod]
	public async Task When_Looped_Play_Remains_Playing()
	{
		var player = CreatePlayer(duration: TimeSpan.FromMilliseconds(200));

		await UITestHelper.Load(player);

		var playTask = player.PlayAsync(0, 1, true).AsTask();
		await Task.Delay(TimeSpan.FromMilliseconds(500));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsPlaying, "Looped playback should remain active after the first iteration duration elapses.");

		player.Stop();
		await playTask;
	}

	[TestMethod]
	public async Task When_Source_Fails_Shows_Fallback_Content_And_Diagnostics()
	{
		var diagnostics = new object();
		var source = new TestAnimatedVisualSource
		{
			Diagnostics = diagnostics,
			ReturnNullVisual = true
		};
		var player = new AnimatedVisualPlayer
		{
			AutoPlay = false,
			Source = source,
			FallbackContent = new DataTemplate(null, (_, _) => new Border
			{
				Name = "FallbackBorder",
				Width = 12,
				Height = 12,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
			})
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsAnimatedVisualLoaded, "Player should stay unloaded when the source fails.");
		Assert.AreSame(diagnostics, player.Diagnostics, "Player should surface the source diagnostics.");
		Assert.IsNotNull(player.FindFirstDescendant<Border>(x => x.Name == "FallbackBorder"), "Fallback content should be present in the player's visual tree.");
	}

	[TestMethod]
	public void When_Invalid_Fallback_Root_Throws()
	{
		var fallbackTemplate = (DataTemplate)XamlReader.Load("""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<x:String>AnimatedVisualPlayer</x:String>
			</DataTemplate>
			""");

		InvalidCastException? thrown = null;
		try
		{
			_ = new AnimatedVisualPlayer
			{
				AutoPlay = false,
				Source = new TestAnimatedVisualSource { ReturnNullVisual = true },
				FallbackContent = fallbackTemplate
			};
		}
		catch (InvalidCastException e)
		{
			thrown = e;
		}

		Assert.IsNotNull(thrown, "A fallback template with a non-UIElement root should throw.");
	}

	[TestMethod]
	public async Task When_Unstarted_Play_Survives_Dynamic_Source_Upgrade()
	{
		var source = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true,
			ReturnEmptyVisual = true,
			NextDuration = TimeSpan.FromMilliseconds(500)
		};
		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = false,
			Source = source
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitForIdle();

		var playTask = player.PlayAsync(0.25, 1.0, false).AsTask();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(player.IsPlaying, "An empty visual should not start playback.");
		Assert.IsFalse(playTask.IsCompleted, "PlayAsync should stay pending until real content is available.");

		source.ReturnEmptyVisual = false;
		source.NextDuration = TimeSpan.FromMilliseconds(900);
		source.RaiseAnimatedVisualInvalidated();

		await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 2000, "The upgraded visual should load.");
		await TestServices.WindowHelper.WaitFor(() => player.IsPlaying, timeoutMS: 2000, "The preserved play should start when the source produces real content.");

		Assert.IsFalse(playTask.IsCompleted, "The preserved play should remain active until it is explicitly stopped or completes.");

		player.Stop();
		await playTask;
	}

	[TestMethod]
	public async Task When_Lottie_SetSourceAsync_Completes_Source_Is_Ready()
	{
		var source = new LottieVisualSource();
		var host = new Border
		{
			Width = 48,
			Height = 48
		};

		await UITestHelper.Load(host);

		await source.SetSourceAsync(FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset);

		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var visual = ((IAnimatedVisualSource3)source).TryCreateAnimatedVisual(compositor, out var diagnostics, createAnimations: false);
		try
		{
			Assert.IsNotNull(visual, "SetSourceAsync should not complete until the source can create a visual.");
			Assert.IsNull(diagnostics, "A successfully loaded Lottie source should not publish diagnostics.");
			Assert.AreNotEqual(Vector2.Zero, visual.Size, "The loaded visual should have a non-empty size.");
			Assert.IsTrue(visual.Duration > TimeSpan.Zero, "The loaded visual should report a duration.");
		}
		finally
		{
			visual?.Dispose();
		}
	}

	[TestMethod]
	public async Task When_Real_Lottie_Source_Swaps_Uri_Player_Reloads()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = source
		};
		var host = CreateHost(player);

		await UITestHelper.Load(host);

		await source.SetSourceAsync(FeatureConfiguration.ProgressRing.ProgressRingAsset);
		await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "The first Lottie source should load.");
		await TestServices.WindowHelper.WaitForIdle();

		var firstDuration = player.Duration;
		player.SetProgress(0.25);
		await TestServices.WindowHelper.WaitForIdle();
		var firstFrame = await UITestHelper.ScreenShot(host);
		await firstFrame.Populate();

		await source.SetSourceAsync(FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset);
		await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded && player.Duration != firstDuration, timeoutMS: 5000, "Swapping the URI should reload the real Lottie source.");
		await TestServices.WindowHelper.WaitForIdle();

		player.SetProgress(0.25);
		await TestServices.WindowHelper.WaitForIdle();
		var secondFrame = await UITestHelper.ScreenShot(host);
		await secondFrame.Populate();

		Assert.AreNotEqual(firstDuration, player.Duration, "Swapping to a different real Lottie asset should refresh the player metadata.");
		Assert.IsTrue(CountDifferentPixels(firstFrame, secondFrame, (int)host.ActualWidth, (int)host.ActualHeight) > 0, "The reloaded asset should render a different frame.");
	}

	[TestMethod]
	public async Task When_Real_Lottie_Source_Fails_Player_Shows_Fallback_Content()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = source,
			FallbackContent = new DataTemplate(null, (_, _) => new Border
			{
				Name = "LottieFallbackBorder",
				Width = 12,
				Height = 12,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
			})
		};
		var host = CreateHost(player);

		await UITestHelper.Load(host);

		await source.SetSourceAsync(CreateUniqueAppDataUri("missing-lottie.json"));
		await TestServices.WindowHelper.WaitFor(() => player.Diagnostics is Exception, timeoutMS: 5000, "Load failures should publish diagnostics and complete.");

		Assert.IsFalse(player.IsAnimatedVisualLoaded, "The player should stay unloaded after a real Lottie load failure.");
		Assert.IsNotNull(player.FindFirstDescendant<Border>(x => x.Name == "LottieFallbackBorder"), "The player should display fallback content after a real Lottie load failure.");
	}

	[TestMethod]
	public async Task When_Real_Lottie_Source_Retries_Same_Uri_After_Failure_It_Loads()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = source
		};
		var retryUri = CreateUniqueAppDataUri("retry-lottie.json");
		var retryPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, Path.GetFileName(retryUri.AbsolutePath));
		var host = CreateHost(player);

		try
		{
			await UITestHelper.Load(host);

			await source.SetSourceAsync(retryUri);
			await TestServices.WindowHelper.WaitFor(() => player.Diagnostics is Exception, timeoutMS: 5000, "The initial load should fail.");

			await WriteEmbeddedAssetToFileAsync(
				FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset,
				retryPath);

			await source.SetSourceAsync(retryUri);
			await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "Retrying the same URI should load the animation once the file appears.");

			Assert.IsNull(player.Diagnostics, "A successful retry should clear the previous diagnostics.");
		}
		finally
		{
			if (File.Exists(retryPath))
			{
				File.Delete(retryPath);
			}
		}
	}

	[TestMethod]
	public async Task When_Lottie_AppData_Uri_Escapes_Its_Declared_Root_Load_Is_Blocked()
	{
		var source = new LottieVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = source
		};
		var fileName = "traversal-lottie.json";
		var roamingPath = Path.Combine(ApplicationData.Current.RoamingFolder.Path, fileName);
		var traversalUri = new Uri($"ms-appdata:///local/%2E%2E/roaming/{fileName}");
		var host = CreateHost(player);

		try
		{
			await WriteEmbeddedAssetToFileAsync(FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset, roamingPath);
			await UITestHelper.Load(host);

			await source.SetSourceAsync(traversalUri);
			await TestServices.WindowHelper.WaitFor(() => player.Diagnostics is UnauthorizedAccessException, timeoutMS: 5000, "Encoded traversal outside the declared appdata root should be rejected.");

			Assert.IsFalse(player.IsAnimatedVisualLoaded, "Traversal attempts should not load animations.");
		}
		finally
		{
			if (File.Exists(roamingPath))
			{
				File.Delete(roamingPath);
			}
		}
	}

	[TestMethod]
	public async Task When_Dynamic_Source_Is_Invalidated_Content_Is_Reloaded()
	{
		var source = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true,
			NextDuration = TimeSpan.FromMilliseconds(200)
		};
		var player = new AnimatedVisualPlayer
		{
			AutoPlay = false,
			Source = source
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, source.CreateCount, "Initial content should be created once.");
		Assert.AreEqual(TimeSpan.FromMilliseconds(200), player.Duration, "Initial duration should come from the first visual.");

		source.NextDuration = TimeSpan.FromMilliseconds(500);
		source.Diagnostics = new object();
		source.RaiseAnimatedVisualInvalidated();

		await TestServices.WindowHelper.WaitFor(() => source.CreateCount == 2, timeoutMS: 2000, "Player should recreate the animated visual after invalidation.");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(TimeSpan.FromMilliseconds(500), player.Duration, "Invalidation should refresh the player from the new visual.");
	}

	[TestMethod]
	public async Task When_Dynamic_Source_Is_Invalidated_While_Unloaded_It_Reloads_On_Load()
	{
		var source = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true,
			NextDuration = TimeSpan.FromMilliseconds(200)
		};
		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = false,
			Source = source
		};

		try
		{
			await UITestHelper.Load(player);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreEqual(1, source.CreateCount, "Initial load should create one animated visual.");

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			source.NextDuration = TimeSpan.FromMilliseconds(600);
			source.RaiseAnimatedVisualInvalidated();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, source.CreateCount, "Invalidation while unloaded should defer content recreation.");

			TestServices.WindowHelper.WindowContent = player;
			await TestServices.WindowHelper.WaitForLoaded(player);
			await TestServices.WindowHelper.WaitFor(() => source.CreateCount == 2, timeoutMS: 2000, "The deferred invalidation should recreate content on the next load.");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(TimeSpan.FromMilliseconds(600), player.Duration, "Reload should use the invalidated content.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Empty_AnimatedVisual_Is_Replaced_It_Is_Disposed()
	{
		var emptySource = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true,
			ReturnEmptyVisual = true
		};
		var replacementSource = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true
		};
		var player = new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = false,
			Source = emptySource
		};

		await UITestHelper.Load(player, x => x.IsLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		var emptyVisual = emptySource.LastVisual;
		Assert.IsNotNull(emptyVisual, "The source should return a non-null empty visual.");
		Assert.IsFalse(player.IsAnimatedVisualLoaded, "Empty visuals should keep the player unloaded.");

		player.Source = replacementSource;
		await TestServices.WindowHelper.WaitFor(() => replacementSource.CreateCount == 1, timeoutMS: 2000, "Replacing the source should recreate content.");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, emptyVisual!.DisposeCallCount, "Replacing the source should dispose the previous empty visual.");
		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Replacement content should load successfully.");
	}

	[TestMethod]
	public async Task When_AnimationOptimization_Is_Resources_Player_Creates_And_Destroys_Animations()
	{
		var source = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true
		};
		var player = new AnimatedVisualPlayer
		{
			AutoPlay = false,
			AnimationOptimization = PlayerAnimationOptimization.Resources,
			Source = source
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(false, source.LastCreateAnimationsFlag, "Source3 should be asked to defer animation creation when optimization is Resources.");
		Assert.IsNotNull(source.LastVisual);
		Assert.AreEqual(0, source.LastVisual!.CreateAnimationsCallCount, "Source3 should not pre-create animations for Resources optimization.");

		await player.PlayAsync(0, 1, false);
		await WaitForCompositionCommitAsync(player);

		Assert.AreEqual(1, source.LastVisual.CreateAnimationsCallCount, "Playback should create animations on demand.");
		Assert.AreEqual(1, source.LastVisual.DestroyAnimationsCallCount, "Resources optimization should destroy animations after playback completes.");
	}

	[TestMethod]
	public async Task When_DestroyAnimations_Is_Stale_CreateAnimations_Wins()
	{
		var source = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true
		};
		var player = new AnimatedVisualPlayer
		{
			AutoPlay = false,
			AnimationOptimization = PlayerAnimationOptimization.Resources,
			Source = source
		};

		await UITestHelper.Load(player);
		await WaitForCompositionCommitAsync(player);

		Assert.IsNotNull(source.LastVisual);

		player.SetProgress(0.5);
		player.AnimationOptimization = PlayerAnimationOptimization.Latency;

		await WaitForCompositionCommitAsync(player);

		Assert.AreEqual(1, source.LastVisual!.CreateAnimationsCallCount, "The original CreateAnimations call should remain in effect.");
		Assert.AreEqual(0, source.LastVisual.DestroyAnimationsCallCount, "The stale DestroyAnimations continuation must not tear down newly-retained animations.");
	}

	[TestMethod]
	public async Task When_DestroyAnimations_Is_Pending_Source_Replacement_Is_Not_Targeted()
	{
		var firstSource = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true
		};
		var secondSource = new TestAnimatedVisualSource
		{
			UseAnimatedVisualSource3 = true
		};
		var player = new AnimatedVisualPlayer
		{
			AutoPlay = false,
			AnimationOptimization = PlayerAnimationOptimization.Resources,
			Source = firstSource
		};

		await UITestHelper.Load(player);
		await TestServices.WindowHelper.WaitForIdle();

		player.SetProgress(0.5);
		player.Source = secondSource;

		await TestServices.WindowHelper.WaitFor(() => secondSource.CreateCount == 1, timeoutMS: 2000, "Source replacement should create the replacement visual.");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(secondSource.LastVisual);
		Assert.AreEqual(0, secondSource.LastVisual!.DestroyAnimationsCallCount, "Delayed destruction must not target the replacement visual.");
	}

	[TestMethod]
	public async Task When_Production_Lottie_Source_Loads_And_Renders()
	{
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = new LottieVisualSource
			{
				UriSource = FeatureConfiguration.ProgressRing.DeterminateProgressRingAsset
			}
		};
		var host = CreateHost(player);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitFor(() => player.IsAnimatedVisualLoaded, timeoutMS: 5000, "The production Lottie source should load asynchronously.");
		await TestServices.WindowHelper.WaitForIdle();

		player.SetProgress(0.15);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.Duration > TimeSpan.Zero, "The production Lottie source should report a non-zero duration.");

		var firstFrame = await UITestHelper.ScreenShot(host);
		await firstFrame.Populate();

		player.SetProgress(0.75);
		await TestServices.WindowHelper.WaitForIdle();

		var secondFrame = await UITestHelper.ScreenShot(host);
		await secondFrame.Populate();

		Assert.IsTrue(ContainsNonWhitePixel(firstFrame, 0, 0, (int)host.ActualWidth, (int)host.ActualHeight), "The production Lottie source should render visible pixels at the early frame.");
		Assert.IsTrue(ContainsNonWhitePixel(secondFrame, 0, 0, (int)host.ActualWidth, (int)host.ActualHeight), "The production Lottie source should render visible pixels at the later frame.");
		Assert.IsTrue(CountDifferentPixels(firstFrame, secondFrame, (int)host.ActualWidth, (int)host.ActualHeight) > 0, "Two known progress frames should produce different output.");
	}

	private static AnimatedVisualPlayer CreatePlayer(bool autoPlay = false, TimeSpan? duration = null)
	{
		return new AnimatedVisualPlayer
		{
			Width = 50,
			Height = 50,
			AutoPlay = autoPlay,
			Source = new TestAnimatedVisualSource
			{
				NextDuration = duration ?? TimeSpan.FromMilliseconds(300)
			}
		};
	}

	private static Border CreateHost(UIElement child)
	{
		return new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = child
		};
	}

	private static double GetPlayerProgress(AnimatedVisualPlayer player)
	{
		Assert.IsInstanceOfType<CompositionPropertySet>(player.ProgressObject);
		var propertySet = (CompositionPropertySet)player.ProgressObject;
		Assert.AreEqual(CompositionGetValueStatus.Succeeded, propertySet.TryGetScalar("Progress", out var value));
		return value;
	}

	private static async Task DelayForHalfAnimationDurationThenReverse(AnimatedVisualPlayer player)
	{
		var delayTimeSpan = TimeSpan.FromTicks((long)(0.5 * player.Duration.Ticks));
		await Task.Delay(delayTimeSpan);

		player.Pause();
		player.PlaybackRate *= -1;
		player.Resume();
	}

	private static async Task WaitForCompositionCommitAsync(AnimatedVisualPlayer player)
	{
		await player.Visual.Compositor.RequestCommitAsync();
		await TestServices.WindowHelper.WaitForIdle();
	}

	private static Uri CreateUniqueAppDataUri(string fileName)
		=> new($"ms-appdata:///local/{Guid.NewGuid():N}-{fileName}");

	private static async Task WriteEmbeddedAssetToFileAsync(Uri embeddedUri, string destinationPath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

		var resourceName = embeddedUri.AbsolutePath.TrimStart('/');
		using var source = typeof(FeatureConfiguration).Assembly.GetManifestResourceStream(resourceName);
		Assert.IsNotNull(source, $"Unable to find embedded resource '{resourceName}'.");

		await using var destination = File.Create(destinationPath);
		await source.CopyToAsync(destination);
		await destination.FlushAsync();
	}

	private static int CountDifferentPixels(RawBitmap first, RawBitmap second, int width, int height, int tolerance = 12)
	{
		var differentPixels = 0;
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				var left = first.GetPixel(x, y);
				var right = second.GetPixel(x, y);
				if (Math.Abs(left.R - right.R) > tolerance
					|| Math.Abs(left.G - right.G) > tolerance
					|| Math.Abs(left.B - right.B) > tolerance
					|| Math.Abs(left.A - right.A) > tolerance)
				{
					differentPixels++;
				}
			}
		}

		return differentPixels;
	}

	private static bool ContainsNonWhitePixel(RawBitmap screenshot, int startX, int startY, int width, int height)
	{
		for (int x = startX; x < startX + width; x++)
		{
			for (int y = startY; y < startY + height; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250 || pixel.A < 250)
				{
					return true;
				}
			}
		}

		return false;
	}

	private sealed class TestAnimatedVisualSource : IAnimatedVisualSource, IAnimatedVisualSource3, IDynamicAnimatedVisualSource
	{
		public bool UseAnimatedVisualSource3 { get; set; }
		public bool ReturnNullVisual { get; set; }
		public bool ReturnEmptyVisual { get; set; }
		public object? Diagnostics { get; set; }
		public TimeSpan NextDuration { get; set; } = TimeSpan.FromMilliseconds(300);
		public int CreateCount { get; private set; }
		public bool? LastCreateAnimationsFlag { get; private set; }
		public TestAnimatedVisual? LastVisual { get; private set; }

		public event TypedEventHandler<IDynamicAnimatedVisualSource, object>? AnimatedVisualInvalidated;

		public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
		{
			if (UseAnimatedVisualSource3)
			{
				return TryCreateAnimatedVisual(compositor, out diagnostics, createAnimations: true);
			}

			return CreateVisual(compositor, createAnimations: true, out diagnostics)!;
		}

		public IAnimatedVisual2 TryCreateAnimatedVisual(Compositor compositor, out object diagnostics, bool createAnimations)
			=> CreateVisual(compositor, createAnimations, out diagnostics)!;

		public void RaiseAnimatedVisualInvalidated()
			=> AnimatedVisualInvalidated?.Invoke(this, default!);

		private TestAnimatedVisual? CreateVisual(Compositor compositor, bool createAnimations, out object diagnostics)
		{
			CreateCount++;
			LastCreateAnimationsFlag = createAnimations;
			diagnostics = Diagnostics!;

			if (ReturnNullVisual)
			{
				LastVisual = null;
				return null;
			}

			var visual = LastVisual = new TestAnimatedVisual(compositor, NextDuration, ReturnEmptyVisual);
			if (createAnimations)
			{
				visual.CreateAnimations();
			}

			return visual;
		}
	}

	private sealed class TestAnimatedVisual : IAnimatedVisual2
	{
		public TestAnimatedVisual(Compositor compositor, TimeSpan duration, bool isEmpty)
		{
			RootVisual = compositor.CreateContainerVisual();
			Size = isEmpty ? Vector2.Zero : new Vector2(100, 100);
			Duration = duration;
		}

		public Visual RootVisual { get; }

		public Vector2 Size { get; }

		public TimeSpan Duration { get; }

		public int CreateAnimationsCallCount { get; private set; }

		public int DestroyAnimationsCallCount { get; private set; }

		public int DisposeCallCount { get; private set; }

		public void CreateAnimations()
			=> CreateAnimationsCallCount++;

		public void DestroyAnimations()
			=> DestroyAnimationsCallCount++;

		public void Dispose()
			=> DisposeCallCount++;
	}
}

#endif
