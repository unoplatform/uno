#if !WINAPPSDK // Disabled on UWP as tests use Uno-specific APIs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ObjectAnimationUsingKeyFrames
	{

		[TestMethod]
		public async Task When_Theme_Changed_Animated_Value()
		{
			var checkBox = new MyCheckBox() { Content = "CheckBox", IsEnabled = false };
			TestServices.WindowHelper.WindowContent = checkBox;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(checkBox.ContentPresenter);

			var lightThemeForeground = TestsColorHelper.ToColor("#5C000000");
			var darkThemeForeground = TestsColorHelper.ToColor("#5DFFFFFF");

			Assert.AreEqual(lightThemeForeground, (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);

			using (UseDarkTheme())
			{
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(darkThemeForeground, (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);
			}
		}

		[TestMethod]
		public async Task When_Animate()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(2), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 3);
			await Task.Yield();

			// v3 is repeated because the target property is not a DependencyProperty
			// and no deduplication happens in the binding engine.
			target.History.Should().BeEquivalentTo(v1, v2, v3, v3);
			sut.State.Should().Be(Timeline.TimelineState.Filling);
		}

		[TestMethod]
		public async Task When_Stop()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(100), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 2);
			await Task.Yield();
			((ITimeline)sut).Stop();
			await Task.Delay(150, ct);

			target.History.Should().BeEquivalentTo(v1, v2);
			sut.State.Should().Be(Timeline.TimelineState.Stopped);
		}

		[TestMethod]
		public async Task When_PauseResume()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(100), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 2);
			await Task.Yield();
			((ITimeline)sut).Pause();

			await Task.Delay(100, ct);
			target.History.Should().BeEquivalentTo(v1, v2);
			sut.State.Should().Be(Timeline.TimelineState.Paused);

			((ITimeline)sut).Resume();
			await target.GetValue(ct, 3);
			await Task.Yield();

			// v3 is repeated because the target property is not a DependencyProperty
			// and no deduplication happens in the binding engine.
			target.History.Should().BeEquivalentTo(v1, v2, v3, v3);
			sut.State.Should().Be(Timeline.TimelineState.Filling);
		}

		[TestMethod]
		public async Task When_RepeatCount()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(3),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(2), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 9);
			await Task.Yield();

			// v3 is repeated because the target property is not a DependencyProperty
			// and no deduplication happens in the binding engine.
			target.History.Should().BeEquivalentTo(v1, v2, v3, v1, v2, v3, v1, v2, v3, v3);
			sut.State.Should().Be(Timeline.TimelineState.Filling);
		}

		[TestMethod]
#if __MACOS__ // #9282 for macOS
		[Ignore]
#endif
#if __SKIA__
		[Ignore("Flaky on Skia targets, see https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_RepeatDuration()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(100 * 3)),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(50), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(100), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 9);
			await Task.Delay(100, ct); // Give opportunity to (wrongly) repeat again some frames

			target.History.Take(9)/* Helps laggish CI! */.Should().BeEquivalentTo(v1, v2, v3, v1, v2, v3, v1, v2, v3);
			sut.State.Should().Be(Timeline.TimelineState.Filling);
		}

		[TestMethod]
		public async Task When_RepeatForever()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = RepeatBehavior.Forever,
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(2), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await target.GetValue(ct, 9);
			await Task.Yield();

			try
			{
				target.History.Count.Should().BeGreaterThan(9);
				target.History.Take(9).Should().BeEquivalentTo(v1, v2, v3, v1, v2, v3, v1, v2, v3);
				sut.State.Should().Be(Timeline.TimelineState.Active);
			}
			finally
			{
				((ITimeline)sut).Stop();
			}
		}

		[TestMethod]
		public async Task When_BeginTime()
		{
			var ct = CancellationToken.None; // Not supported yet by test engine

			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.FromMilliseconds(100),
				RepeatBehavior = new RepeatBehavior(),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(2), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			((ITimeline)sut).Begin();
			await Task.Delay(5, ct);
			((ITimeline)sut).Stop();

			target.History.Should().BeEquivalentTo(/* empty */);
			sut.State.Should().Be(Timeline.TimelineState.Stopped);
		}

		[TestMethod]
		public void When_FirstFrameTimeIsZero_Then_ItsAppliedSyncOnStart()
		{
			object v1, v2, v3;
			var target = new AnimTarget();
			var sut = new ObjectAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.Zero,
				RepeatBehavior = new RepeatBehavior(),
				FillBehavior = FillBehavior.HoldEnd,
				KeyFrames =
				{
					new ObjectKeyFrame{KeyTime = TimeSpan.Zero, Value = v1 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(1), Value = v2 = new object()},
					new ObjectKeyFrame{KeyTime = TimeSpan.FromMilliseconds(2), Value = v3 = new object()},
				}
			};

			Storyboard.SetTarget(sut, target);
			Storyboard.SetTargetProperty(sut, nameof(target.Value));

			try
			{
				((ITimeline)sut).Begin();

				target.Value.Should().Be(v1);
			}
			finally
			{
				((ITimeline)sut).Stop();
			}
		}


		/// <summary>
		/// Ensure dark theme is applied for the course of a single test.
		/// </summary>
		private IDisposable UseDarkTheme() => ThemeHelper.UseDarkTheme();

		// Intentionally nested to test NativeCtorsGenerator handling of nested classes.
		public partial class MyCheckBox : CheckBox
		{
			public ContentPresenter ContentPresenter { get; set; }
			protected override void OnApplyTemplate()
			{
				base.OnApplyTemplate();
				ContentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter; // This is a ContentPresenter
			}
		}
	}

	public partial class AnimTarget : DependencyObject
	{
		private event EventHandler _valuedAdded;
		private object _value;

		public object Value
		{
			get => _value;
			set
			{
				_value = value;
				History.Add(value);
				_valuedAdded?.Invoke(this, EventArgs.Empty);
			}
		}

		public List<object> History { get; } = new List<object>();

		public async Task<object> GetValue(CancellationToken ct, int count, TimeSpan? timeout = null)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource(
				ct,
				new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(1)).Token);
			var tcs = new TaskCompletionSource<object>(TaskCreationOptions.AttachedToParent);

			using var _ = cts.Token.Register(() => tcs.TrySetCanceled());
			try
			{
				_valuedAdded += OnValueAdded;
				OnValueAdded(null, null);
				return await tcs.Task;
			}
			finally
			{
				_valuedAdded -= OnValueAdded;
			}

			void OnValueAdded(object snd, EventArgs eventArgs)
			{
				if (History.Count >= count)
				{
					tcs.TrySetResult(History[count - 1]);
				}
			}
		}
	}
}
#endif
