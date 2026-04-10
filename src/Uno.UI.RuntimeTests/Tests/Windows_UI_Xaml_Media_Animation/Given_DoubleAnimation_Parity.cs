using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	/// <summary>
	/// Tests ported from WinUI DoubleAnimationTests.cpp (FillBehaviorWUC, EasingFunctionsWUC,
	/// SpeedRatio, From/To/By precedence, Duration edge cases, AutoReverse+RepeatBehavior).
	/// </summary>
	[TestClass]
	[RunsOnUIThread]
	public class Given_DoubleAnimation_Parity
	{
		#region FillBehavior (ported from DoubleAnimationTests::FillBehaviorWUC)

		[TestMethod]
		public async Task When_FillBehavior_HoldEnd_Holds_Value()
		{
			// WinUI: FillBehavior::HoldEnd keeps the To value after the animation completes.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 0;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"HoldEnd should maintain To value (100) after completion, was {translate.X}");
		}

		[TestMethod]
		public async Task When_FillBehavior_Stop_Returns_To_Base()
		{
			// WinUI: FillBehavior::Stop reverts to the local value after the animation completes.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			// Set local value first — animation should revert to this.
			translate.X = 25;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.Stop,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(25.0, translate.X, 1.0,
				$"FillBehavior.Stop should revert to local value (25), was {translate.X}");
		}

		[TestMethod]
		public async Task When_FillBehavior_HoldEnd_Then_Stop_And_SetLocal()
		{
			// WinUI: After HoldEnd fills at 100, calling Stop clears the animated value,
			// then setting a local value should read that local value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 0;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var storyboard = animation.ToStoryboard();
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			// Animation is filling at 100
			Assert.AreEqual(100.0, translate.X, 1.0,
				$"Fill value should be 100, was {translate.X}");

			// Stop the storyboard — clears the animated value
			storyboard.Stop();
			await WindowHelper.WaitForIdle();

			// Set a new local value
			translate.X = 42;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(42.0, translate.X, 1.0,
				$"After Stop + set local, value should be 42, was {translate.X}");
		}

		[TestMethod]
		public async Task When_FillBehavior_Stop_With_AutoReverse()
		{
			// WinUI: AutoReverse + FillBehavior.Stop → after the full forward+reverse cycle,
			// the animated value is cleared and the property returns to the local value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 15;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				AutoReverse = true,
				FillBehavior = FillBehavior.Stop,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(15.0, translate.X, 1.0,
				$"AutoReverse + FillBehavior.Stop should return to local value (15), was {translate.X}");
		}

		#endregion

		#region Easing functions (ported from DoubleAnimationTests::EasingFunctionsWUC)

		[TestMethod]
		public async Task When_CircleEase_Applied()
		{
			// WinUI: CircleEase applied to a DoubleAnimation, verify animation completes to target.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"CircleEase animation should complete to To value (100), was {translate.X}");
		}

		[TestMethod]
		public async Task When_CubicEase_EaseIn_SlowStart()
		{
			// WinUI: CubicEase EaseIn starts slow — at 30% time, value should be < 10% of range.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var storyboard = animation.ToStoryboard();
			storyboard.Begin();

			// Sample at ~30% through (300ms of 1000ms)
			await Task.Delay(300);
			var earlyValue = translate.X;

			// Wait for completion
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			// CubicEase EaseIn at 30% time: t^3 = 0.3^3 = 0.027, so value ~2.7% of range.
			// Use generous tolerance: value should be < 15% of range (< 15).
			Assert.IsTrue(earlyValue < 15.0,
				$"CubicEase EaseIn at ~30% time should be < 15 (slow start), was {earlyValue}");
			Assert.AreEqual(100.0, translate.X, 1.0,
				$"CubicEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_ExponentialEase_Applied()
		{
			// WinUI: ExponentialEase with custom Exponent, verify completion.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				EasingFunction = new ExponentialEase
				{
					Exponent = 4,
					EasingMode = EasingMode.EaseOut,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"ExponentialEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_ElasticEase_Completes()
		{
			// WinUI: ElasticEase EaseOut with Oscillations=3, Springiness=3, verify final value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(800)),
				EasingFunction = new ElasticEase
				{
					EasingMode = EasingMode.EaseOut,
					Oscillations = 3,
					Springiness = 3,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"ElasticEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_BounceEase_Completes()
		{
			// WinUI: BounceEase EaseOut, Bounces=3, Bounciness=2, verify final value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(800)),
				EasingFunction = new BounceEase
				{
					EasingMode = EasingMode.EaseOut,
					Bounces = 3,
					Bounciness = 2,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"BounceEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_BackEase_Completes()
		{
			// WinUI: BackEase EaseOut with Amplitude=1, verify final value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				EasingFunction = new BackEase
				{
					EasingMode = EasingMode.EaseOut,
					Amplitude = 1,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"BackEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_PowerEase_Applied()
		{
			// WinUI: PowerEase with Power=4, verify final value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				EasingFunction = new PowerEase
				{
					EasingMode = EasingMode.EaseOut,
					Power = 4,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"PowerEase animation should complete to 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_QuinticEase_Applied()
		{
			// WinUI: QuinticEase, verify final value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				EasingFunction = new QuinticEase
				{
					EasingMode = EasingMode.EaseOut,
				},
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"QuinticEase animation should complete to 100, was {translate.X}");
		}

		#endregion

		#region KeyFrame easing (ported from DoubleAnimationTests::EasingFunctionsWUC)

		[TestMethod]
		public async Task When_EasingKeyFrame_ExponentialEase()
		{
			// WinUI: EasingDoubleKeyFrame at 200ms with ExponentialEase Exponent=1.25 EaseIn.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var keyFrameAnimation = new DoubleAnimationUsingKeyFrames
			{
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(keyFrameAnimation, translate);
			Storyboard.SetTargetProperty(keyFrameAnimation, nameof(translate.X));

			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
				Value = 100,
				EasingFunction = new ExponentialEase
				{
					Exponent = 1.25,
					EasingMode = EasingMode.EaseIn,
				},
			});

			var storyboard = new Storyboard();
			storyboard.Children.Add(keyFrameAnimation);
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			// After completion, value should be held at the last keyframe value (100).
			Assert.AreEqual(100.0, translate.X, 1.0,
				$"EasingKeyFrame with ExponentialEase should end at 100, was {translate.X}");
		}

		[TestMethod]
		public async Task When_EasingKeyFrame_CubicEase()
		{
			// WinUI: Second keyframe at 400ms with CubicEase (default EaseOut).
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var keyFrameAnimation = new DoubleAnimationUsingKeyFrames
			{
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(keyFrameAnimation, translate);
			Storyboard.SetTargetProperty(keyFrameAnimation, nameof(translate.X));

			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
				Value = 50,
				EasingFunction = new CubicEase(),
			});

			var storyboard = new Storyboard();
			storyboard.Children.Add(keyFrameAnimation);
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(50.0, translate.X, 1.0,
				$"EasingKeyFrame with CubicEase should end at 50, was {translate.X}");
		}

		[TestMethod]
		public async Task When_EasingKeyFrame_SineEaseInOut()
		{
			// WinUI: Third keyframe at 600ms with SineEase EaseInOut.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var keyFrameAnimation = new DoubleAnimationUsingKeyFrames
			{
				Duration = new Duration(TimeSpan.FromMilliseconds(700)),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(keyFrameAnimation, translate);
			Storyboard.SetTargetProperty(keyFrameAnimation, nameof(translate.X));

			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
				Value = 150,
				EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
			});

			var storyboard = new Storyboard();
			storyboard.Children.Add(keyFrameAnimation);
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(150.0, translate.X, 1.0,
				$"EasingKeyFrame with SineEase EaseInOut should end at 150, was {translate.X}");
		}

		[TestMethod]
		public async Task When_Multiple_EasingKeyFrames()
		{
			// WinUI: 3 easing keyframes in one animation — ExponentialEase, CubicEase, ElasticEase.
			// Matches the EasingFunctionsWUC test structure with 3 keyframes.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var keyFrameAnimation = new DoubleAnimationUsingKeyFrames
			{
				Duration = new Duration(TimeSpan.FromMilliseconds(900)),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(keyFrameAnimation, translate);
			Storyboard.SetTargetProperty(keyFrameAnimation, nameof(translate.X));

			// KF1: ExponentialEase Exponent=1.25 EaseIn → 100
			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
				Value = 100,
				EasingFunction = new ExponentialEase
				{
					Exponent = 1.25,
					EasingMode = EasingMode.EaseIn,
				},
			});

			// KF2: CubicEase (default EaseOut) → 50
			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
				Value = 50,
				EasingFunction = new CubicEase(),
			});

			// KF3: ElasticEase EaseInOut, Oscillations=2, Springiness=0.7 → 150
			keyFrameAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
			{
				KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900)),
				Value = 150,
				EasingFunction = new ElasticEase
				{
					EasingMode = EasingMode.EaseInOut,
					Oscillations = 2,
					Springiness = 0.7,
				},
			});

			var storyboard = new Storyboard();
			storyboard.Children.Add(keyFrameAnimation);
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

			Assert.AreEqual(150.0, translate.X, 1.0,
				$"Multiple easing keyframes should complete to final value (150), was {translate.X}");
		}

		#endregion

		#region SpeedRatio

		[TestMethod]
		public async Task When_SpeedRatio_2x()
		{
			// WinUI: 2s duration at SpeedRatio=2 should complete in ~1s wall-clock time.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(2000)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var storyboard = animation.ToStoryboard();
			storyboard.SpeedRatio = 2.0;

			var sw = Stopwatch.StartNew();
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
			sw.Stop();

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"SpeedRatio 2x animation should complete to 100, was {translate.X}");
			// At 2x speed, a 2000ms animation should complete in ~1000ms.
			// Use generous tolerance: should be done well before 1800ms.
			Assert.IsTrue(sw.ElapsedMilliseconds < 1800,
				$"SpeedRatio 2x should complete faster than 1800ms, took {sw.ElapsedMilliseconds}ms");
		}

		[TestMethod]
		public async Task When_SpeedRatio_Half()
		{
			// WinUI: 500ms duration at SpeedRatio=0.5 should complete in ~1s wall-clock time.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var storyboard = animation.ToStoryboard();
			storyboard.SpeedRatio = 0.5;

			var sw = Stopwatch.StartNew();
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
			sw.Stop();

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"SpeedRatio 0.5x animation should complete to 100, was {translate.X}");
			// At 0.5x speed, a 500ms animation should complete in ~1000ms.
			// Should take at least 800ms.
			Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
				$"SpeedRatio 0.5x should take at least 800ms, took {sw.ElapsedMilliseconds}ms");
		}

		#endregion

		#region From/To/By precedence

		[TestMethod]
		public async Task When_Both_To_And_By_Set_To_Wins()
		{
			// WinUI: When both To and By are set, To takes priority and By is ignored.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 0;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 80,
				By = 200, // Should be ignored because To is set
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(80.0, translate.X, 1.0,
				$"To should take priority over By — expected 80, was {translate.X}");
		}

		[TestMethod]
		public async Task When_From_And_By_No_To()
		{
			// WinUI: From=20, By=30 → animates from 20 to (20+30)=50.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 20,
				By = 30,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(50.0, translate.X, 1.0,
				$"From=20 + By=30 should animate to 50, was {translate.X}");
		}

		[TestMethod]
		public async Task When_By_Only_Animates_From_Current()
		{
			// WinUI: By=70 with current value=30 → animates from 30 to (30+70)=100.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 30;
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				By = 70,
				Duration = new Duration(TimeSpan.FromMilliseconds(300)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"By=70 from current=30 should animate to 100, was {translate.X}");
		}

		#endregion

		#region Duration edge cases

		[TestMethod]
		public async Task When_ZeroDuration_Jumps_To_Value()
		{
			// WinUI: Duration=0 causes an instant jump to the To value.
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.X = 0;

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.Zero),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(100.0, translate.X, 1.0,
				$"Zero-duration animation should jump to To value (100), was {translate.X}");
		}

		[TestMethod]
		public async Task When_Default_Duration_Is_One_Second()
		{
			// WinUI: DoubleAnimation without explicit Duration defaults to 1 second (NULL_DURATION_DEFAULT).
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			// No Duration property set — should default to 1 second
			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var sw = Stopwatch.StartNew();
			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			sw.Stop();

			// Should have taken approximately 1 second
			Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
				$"Default 1s animation should take at least 800ms, took {sw.ElapsedMilliseconds}ms");
			Assert.AreEqual(100.0, translate.X, 1.0,
				$"Default-duration animation should complete to 100, was {translate.X}");
		}

		#endregion

		#region AutoReverse + RepeatBehavior combinations

		[TestMethod]
		public async Task When_AutoReverse_With_RepeatCount2()
		{
			// WinUI: RepeatBehavior.Count=2 with AutoReverse means 2 complete cycles.
			// Each cycle = forward + reverse. Even count ends on a reverse pass,
			// so with HoldEnd the fill value is the From value (0).
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(200)),
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(2),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var sw = Stopwatch.StartNew();
			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
			sw.Stop();

			// 2 complete forward+reverse cycles = 2 * (200ms + 200ms) = 800ms total
			Assert.IsTrue(sw.ElapsedMilliseconds >= 600,
				$"2 AutoReverse cycles of 200ms each should take ~800ms, took {sw.ElapsedMilliseconds}ms");

			// Even count with AutoReverse: ends on reverse pass, fill at From (0)
			Assert.AreEqual(0.0, translate.X, 2.0,
				$"AutoReverse with RepeatCount=2 (even) should end at From value (0), was {translate.X}");
		}

		[TestMethod]
		public async Task When_AutoReverse_With_RepeatCount3_Odd()
		{
			// WinUI: RepeatBehavior.Count=3 with AutoReverse.
			// RepeatBehavior.Count counts full iterations where each iteration = forward+reverse
			// when AutoReverse=true. So 3 iterations = 3 * (forward+reverse) and always ends at From (0).
			var translate = new TranslateTransform();
			var border = new Border
			{
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(200)),
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(3),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.X));

			var sw = Stopwatch.StartNew();
			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
			sw.Stop();

			// 3 iterations with AutoReverse: 3 * (200ms forward + 200ms reverse) = 1200ms
			Assert.IsTrue(sw.ElapsedMilliseconds >= 900,
				$"3 AutoReverse iterations of 200ms each should take at least 900ms, took {sw.ElapsedMilliseconds}ms");

			// Each iteration includes forward+reverse, so the animation always ends at From (0)
			Assert.AreEqual(0.0, translate.X, 2.0,
				$"AutoReverse with RepeatCount=3 should end at From value (0), was {translate.X}");
		}

		#endregion
	}
}
