using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Private.Infrastructure;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Microsoft.Extensions.Logging;
using Uno.Logging;
#endif

namespace Uno.UI.Samples.Content.UITests
{
	[SampleControlInfo("Animations", "DoubleAnimationTestsControl")]

	public sealed partial class DoubleAnimationTestsControl : UserControl
	{
#if HAS_UNO
#pragma warning disable CS0109
		private new readonly Logger _log = Uno.Foundation.Logging.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#pragma warning restore CS0109
#else
		private static readonly ILogger _log = Uno.Extensions.LogExtensionPoint.Log(typeof(ControlWithTouchEvent));
#endif

		const int FrameRate = 500;

		public DoubleAnimationTestsControl()
		{
			this.InitializeComponent();
			this.DataContext = this;

			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnLoaded;

			InitList();
		}

		private void InitList()
		{
			Lst = new List<SourceItem>
			{
				new SourceItem
				{
					Name = "Reset",
					Description = "Reset",
					Command = new Common.DelegateCommand(Reset)
				},
				new SourceItem
				{
					Name = "Test 01: Animate Height From: 0 To: 100",
					Description = "Animate Height From: 0 To: 100",
					Command = new Common.DelegateCommand(Test01)
				},
				new SourceItem
				{
					Name = "Test 02: Animate Height By 100",
					Description = "Animate Height By 100",
					Command = new Common.DelegateCommand(Test02)
				},
				new SourceItem
				{
					Name = "Test 03: Animate Opacity From: 0 To: 1",
					Description = "Animate Opacity From: 0 To: 1",
					Command = new Common.DelegateCommand(Test03)
				},
				new SourceItem
				{
					Name = "Test 04: Begin Pause Resume",
					Description = "Expected: Opacity 0->.5, DELAY, .5->1",
					Command = new Common.DelegateCommand(Dispatch(Test04))
				},
				new SourceItem
				{
					Name = "Test 04.1: Skip to fill",
					Description = "Expected: increase height by 100 immediately",
					Command = new Common.DelegateCommand(Test041)
				},
				new SourceItem
				{
					Name = "Test 04.2: Seek",
					Description = "Expected 50, DELAY, 50->100",
					Command = new Common.DelegateCommand(Dispatch(Test042))
				},
				new SourceItem
				{
					Name = "Test 05: Duration Automatic",
					Description = "Expected: 0->100 in 1 second",
					Command = new Common.DelegateCommand(Test05)
				},
				new SourceItem
				{
					Name = "Test 06: Duration Forever",
					Description = "Expected: 0 height doesn't animate",
					Command = new Common.DelegateCommand(Test06)
				},
				new SourceItem
				{
					Name = "Test 07: Fill behavior stop",
					Description = "Expected 0->50 immediately to STARTVALUE",
					Command = new Common.DelegateCommand(Test07)
				},
				new SourceItem
				{
					Name = "Test 08: Begin time 1 sec",
					Description = "TExpected: Delay, 0-100",
					Command = new Common.DelegateCommand(Test08)
				},
				new SourceItem
				{
					Name = "Test 09: Repeat 3 times, end on 100",
					Description = "Repeat 3 times, end on 100",
					Command = new Common.DelegateCommand(Test09)
				},
				new SourceItem
				{
					Name = "Test 10: Repeat For Duration*1.5",
					Description = "Expected repeat 1 and a half times",
					Command = new Common.DelegateCommand(Test10)
				},
				new SourceItem
				{
					Name = "Test 11: Repeat Forever",
					Description = "Repeat Forever",
					Command = new Common.DelegateCommand(Test11)
				},
				new SourceItem
				{
					Name = "Test 12: Speed Ratio 10x",
					Description = " 0->100 very fast",
					Command = new Common.DelegateCommand(Test12)
				},
				new SourceItem
				{
					Name = "Test 13: ScaleY 0->3",
					Description = "Expected Border grows from 0->300 no other controls are effected",
					Command = new Common.DelegateCommand(Test13)
				},
				new SourceItem
				{
					Name = "Test 14: TranslateY 0->50",
					Description = "Expected Border moves down, no other controls are effected",
					Command = new Common.DelegateCommand(Test14)
				},
				new SourceItem
				{
					Name = "Test 15: SkewY 0->45",
					Description = "Expected Border skew, no other controls are effected",
					Command = new Common.DelegateCommand(Test15)
				},
				new SourceItem
				{
					Name = "Test 15.1: SkewX 0->45",
					Description = "Expected Border skew, no other controls are effected",
					Command = new Common.DelegateCommand(Test151)
				},
				new SourceItem
				{
					Name = "Test 16: Rotation 0->360",
					Description = "Expected Border rotates 360 degrees, no other controls are effected",
					Command = new Common.DelegateCommand(Test16)
				},
				new SourceItem
				{
					Name = "Test 17: CenterY 0->100 While Rotation 0->360",
					Description = "CenterY 0->100 While Rotation 0->360",
					Command = new Common.DelegateCommand(Test17)
				},
				new SourceItem
				{
					Name = "Test 18: CenterY 0->100, ScaleY 0->3 and Rotation 0->360",
					Description = " CenterY 0->100, ScaleY 0->3 and Rotation 0->360",
					Command = new Common.DelegateCommand(Test18)
				}
			};

			Box.ItemsSource = Lst.ToArray();

			void handleSelection(object sender, object args)
			{
				var item = (SourceItem)Box.SelectedItem;
				Txt.Text = item.Description;
			}

			Box.Loaded += (s, e) => Box.SelectionChanged += handleSelection;
			Box.Unloaded += (s, e) => Box.SelectionChanged -= handleSelection;

			Btn.Command = new Common.DelegateCommand(() =>
			{
				var item = (SourceItem)Box.SelectedItem;
				item.Command.Execute(null);
			});
		}

		public List<SourceItem> Lst { get; set; }

		private Action Dispatch(Func<CancellationToken, Task> action) => () =>
		{
			_ = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, async () =>
			{
				try
				{
					await action(CancellationToken.None);
				}
				catch (Exception e)
				{
					_log.Error("Failed", e);
				}
			});
		};

		private void Reset()
		{
			TestBorder.Width = 100;
			TestBorder.Height = 20;
			TestBorder.Opacity = 1;
			TestBorder.RenderTransform = null;
		}

		private Storyboard GetNewStoryBoard(string property = "Height")
		{
			return GetNewStoryBoard(TestBorder, property);
		}

		private Storyboard GetNewStoryBoardRenderTransform(string property)
		{
			var transform = new CompositeTransform();
			TestBorder.RenderTransform = transform;

			var story = GetNewStoryBoard(transform, property);
			return story;
		}


		private Storyboard GetNewStoryBoard(DependencyObject target, string property = "Height")
		{
			var storyBoard = new Storyboard();

			var animation = new DoubleAnimation();

			animation.From = 0;
			animation.To = 100;
			animation.Duration = new Duration(TimeSpan.FromSeconds(3));
			animation.EnableDependentAnimation = true;

			Storyboard.SetTarget(animation, target);
			Storyboard.SetTargetProperty(animation, property);

			storyBoard.Children.Add(animation);

			return storyBoard;
		}

		private void Test01()
		{
			var story = GetNewStoryBoard();
			story.Begin();
		}

		private void Test02()
		{
			var story = GetNewStoryBoard();
			var animation = (DoubleAnimation)story.Children.First();

			animation.From = null;
			animation.To = null;
			animation.By = 100;

			story.Begin();
		}

		private void Test03()
		{

			var story = GetNewStoryBoard("Opacity");
			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 1;

			story.Begin();
		}

		private async Task Test04(CancellationToken cancellationToken)
		{
			var story = GetNewStoryBoard("Height"); //Known issue - Android: This test won't work on Height, CPU blocking

			var animation = (DoubleAnimation)story.Children.First();
			var halfway = (int)animation.Duration.TimeSpan.TotalMilliseconds / 2;
			animation.From = 0;
			animation.To = 100;

			story.Begin();

			await Task.Delay(halfway, cancellationToken);
			story.Pause();

			await Task.Delay(halfway, cancellationToken);
			story.Resume();
		}

		private void Test041()
		{
			var story = GetNewStoryBoard();
			var animation = (DoubleAnimation)story.Children.First();

			animation.From = null;
			animation.To = null;
			animation.By = 100;

			story.SkipToFill();
		}

		private async Task Test042(CancellationToken cancellationToken)
		{
			var story = GetNewStoryBoard();
			story.Begin();
			story.Pause();
			var animation = (DoubleAnimation)story.Children.First();

			var halfway = (int)animation.Duration.TimeSpan.TotalMilliseconds / 2;

			story.Seek(TimeSpan.FromMilliseconds(halfway));

			await Task.Delay(halfway * 2, cancellationToken);

			story.Resume();
		}

		private void Test05()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.Duration = Duration.Automatic;

			story.Begin();
		}

		private void Test06()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.Duration = Duration.Forever; //Known issue - Android: This test will cause all other tests to keep height=0

			story.Begin();
		}

		private void Test07()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.FillBehavior = FillBehavior.Stop;
			animation.To = 50;

			story.Begin();
		}

		private void Test08()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.BeginTime = TimeSpan.FromSeconds(1);

			story.Begin();
		}

		private void Test09()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.RepeatBehavior = new RepeatBehavior(3);

			story.Begin();
		}

		private void Test10()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();

			var stopTime = animation.Duration.TimeSpan.TotalMilliseconds * 1.5;

			animation.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(stopTime));

			story.Begin();
		}

		private void Test11()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			animation.RepeatBehavior = RepeatBehavior.Forever;

			story.Begin();
		}

		private void Test12()
		{
			var story = GetNewStoryBoard();

			var animation = (DoubleAnimation)story.Children.First();
			//animation.SpeedRatio = 10;

			story.Begin();
		}


		private void Test13()
		{
			var story = GetNewStoryBoardRenderTransform("ScaleY");

			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 3;

			story.Begin();

		}

		private void Test14()
		{
			var story = GetNewStoryBoardRenderTransform("TranslateY");

			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 45;

			story.Begin();
		}

		private void Test15()
		{
			var story = GetNewStoryBoardRenderTransform("SkewY");

			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 50;

			story.Begin();
		}

		private void Test151()
		{
			var story = GetNewStoryBoardRenderTransform("SkewX");

			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 150;

			story.Begin();
		}

		private void Test16()
		{
			var story = GetNewStoryBoardRenderTransform("Rotation");

			var animation = (DoubleAnimation)story.Children.First();

			animation.From = 0;
			animation.To = 360;

			story.Begin();
		}

		private void Test17()
		{
			var transform = new CompositeTransform();
			TestBorder.RenderTransform = transform;

			var story = GetNewStoryBoard(transform, "CenterX");
			var animationCenter = (DoubleAnimation)story.Children.First();

			animationCenter.From = 0;
			animationCenter.To = 100;

			var animation = new DoubleAnimation();

			animation.From = 0;
			animation.To = 360;
			animation.Duration = new Duration(TimeSpan.FromSeconds(3));
			animation.EnableDependentAnimation = true;

			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, "Rotation");

			story.Children.Add(animation);

			story.Begin();
		}

		private void Test18()
		{
			var transform = new CompositeTransform();
			TestBorder.RenderTransform = transform;

			var story = GetNewStoryBoard(transform, "CenterX");
			var animationCenter = (DoubleAnimation)story.Children.First();

			animationCenter.From = 0;
			animationCenter.To = 100;
			animationCenter.Duration = new Duration(TimeSpan.FromSeconds(3));

			var animationScale = new DoubleAnimation();

			animationScale.From = 0;
			animationScale.To = 3;
			animationScale.Duration = new Duration(TimeSpan.FromSeconds(3));
			animationScale.EnableDependentAnimation = true;

			Storyboard.SetTarget(animationScale, transform);
			Storyboard.SetTargetProperty(animationScale, "ScaleY");

			story.Children.Add(animationScale);

			var animationRotate = new DoubleAnimation();

			animationRotate.From = 0;
			animationRotate.To = 360;
			animationRotate.Duration = new Duration(TimeSpan.FromSeconds(3));
			animationRotate.EnableDependentAnimation = true;

			Storyboard.SetTarget(animationRotate, transform);
			Storyboard.SetTargetProperty(animationRotate, "Rotation");

			story.Children.Add(animationRotate);
			story.Begin();
		}
	}

	public class SourceItem
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public ICommand Command { get; set; }
	}
}
