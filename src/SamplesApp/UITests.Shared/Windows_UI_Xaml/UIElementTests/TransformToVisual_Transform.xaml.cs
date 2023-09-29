using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Uno.UI.Toolkit;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI.Samples.UITests.Helpers;
using Uno.UI.Toolkit;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo("UIElement", "TransformToVisual_Transform")]
	public sealed partial class TransformToVisual_Transform : UserControl
	{
		private readonly TestRunner _tests;

		public TransformToVisual_Transform()
		{
			this.InitializeComponent();

			_tests = new TestRunner(this, Outputs);

			Loaded += TransformToVisual_Transform_Loaded;
		}

		private async void TransformToVisual_Transform_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Delay(500); // On Android automated UI tests, make sure the status bar is collapsed and window resized before starting tests.

			_tests.Run(
				() => When_TransformToRoot(),
				() => When_TransformToRoot_With_TranslateTransform(),
				() => When_TransformToRoot_With_InheritedTranslateTransform_And_Margin(),
				() => When_TransformToParent_With_Margin(),
				() => When_TransformToParent_With_InheritedMargin(),
				() => When_TransformToParent_With_CompositeTransform(),
				() => When_TransformToParent_With_InheritedCompositeTransform_And_Margin(),
				() => When_TransformToAnotherBranch_With_InheritedCompositeTransform_And_Margin());
		}

		public void When_TransformToRoot()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current is not null ?
				Windows.UI.Xaml.Window.Current.Bounds :
				new Windows.Foundation.Rect(default, XamlRoot.Size); ;
			var visible = VisibleBoundsPadding.WindowPadding;
			var originAbs = new Point(windowBounds.Width - visible.Right - Border1.ActualWidth, windowBounds.Height - visible.Bottom - Border1.ActualHeight);

			var sut = Border1.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X, originAbs.Y, 50, 50), result);
		}

		public void When_TransformToRoot_With_TranslateTransform()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current is not null ?
				Windows.UI.Xaml.Window.Current.Bounds :
				new Windows.Foundation.Rect(default, XamlRoot.Size); ;
			var visible = VisibleBoundsPadding.WindowPadding;
			var originAbs = new Point(windowBounds.Width - visible.Right - Border2.ActualWidth, windowBounds.Height - visible.Bottom - Border2.ActualHeight);
			const int tX = -50;
			const int tY = -50;

			var sut = Border2.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X + tX, originAbs.Y + tY, 50, 50), result);
		}

		public void When_TransformToRoot_With_InheritedTranslateTransform_And_Margin()
		{
			var windowBounds = Windows.UI.Xaml.Window.Current is not null ?
				Windows.UI.Xaml.Window.Current.Bounds :
				new Windows.Foundation.Rect(default, XamlRoot.Size); ;
			var visible = VisibleBoundsPadding.WindowPadding;
			var originAbs = new Point(windowBounds.Width - visible.Right - Border2.ActualWidth, windowBounds.Height - visible.Bottom - Border2.ActualHeight);
			const int tX = -50;
			const int tY = -50;
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2Child.TransformToVisual(null);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(originAbs.X + tX + marginX, originAbs.Y + tY + marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_Margin()
		{
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2Child.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(marginX, marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_InheritedMargin()
		{
			const int marginX = 0;
			const int marginY = 30;

			var sut = Border2SubChild.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, 50, 50));

			Assert.AreEqual(new Rect(marginX, marginY, 50, 50), result);
		}

		public void When_TransformToParent_With_CompositeTransform()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			var sut = Border3.TransformToVisual(Border3Parent);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var expected = new Rect(tX - length / 2, tY /* Origin of rotation is top/left, so as angle < 90, origin Y is not impacted */, length, length);

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}

		public void When_TransformToParent_With_InheritedCompositeTransform_And_Margin()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			const double marginX = 0;
			const double marginY = 30;

			var sut = Border3Child.TransformToVisual(Border3Parent);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var marginXProjection = marginX * scale * Math.Cos(angle); // as angle == 45 degree and we are using squares, projection is the same on X and Y
			var marginYProjection = marginY * scale * Math.Cos(angle);
			var origin = new Point(
				tX - length / 2 + marginXProjection - marginYProjection,
				tY - marginXProjection + marginYProjection);
			var expected = new Rect(origin, new Size(length, length));

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}

		public void When_TransformToAnotherBranch_With_InheritedCompositeTransform_And_Margin()
		{
			const double w = 50;
			const double h = 50;

			const double tX = -100;
			const double tY = -100;
			const double scale = 2;
			const double angle = 45 * Math.PI / 180.0;

			const double marginX = 0;
			const double marginY = 30;

			var sut = Border3Child.TransformToVisual(Border2);
			var result = sut.TransformBounds(new Rect(0, 0, w, h));

			// Get expected compared to Border3Parent (like previous test: When_TransformToParent_With_InheritedCompositeTransform_And_Margin)
			var length = w * scale * Math.Cos(angle) + h * scale * Math.Cos(angle);
			var marginXProjection = marginX * scale * Math.Cos(angle); // as angle == 45 degree and we are using squares, projection is the same on X and Y
			var marginYProjection = marginY * scale * Math.Cos(angle);
			var origin = new Point(
				tX - length / 2 + marginXProjection - marginYProjection,
				tY - marginXProjection + marginYProjection);
			var expected = new Rect(origin, new Size(length, length));

			// Then apply the translation of B2
			expected = new Rect(expected.X + 50, expected.Y + 50, expected.Width, expected.Height);

			Assert.IsTrue(RectCloseComparer.UI.Equals(expected, result));
		}
	}

	internal class TestRunner
	{
		private readonly UserControl _target;
		private readonly TextBlock _status;
		private readonly Dictionary<string, Test> _tests;

		private bool _isRunning;

		public TestRunner(UserControl target, StackPanel testsOutput)
		{
			_target = target;

			_status = new TextBlock { Name = "TestsStatus" };
			testsOutput.Children.Add(_status);

			_tests = target
				.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.Where(method => method.Name.StartsWith("When_"))
				.Select(method => new Test(this, method))
				.ToDictionary(t => t.Name);

			foreach (var test in _tests.Values)
			{
				Button play;
				testsOutput.Children.Add(new StackPanel
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Orientation = Orientation.Horizontal,
					Children =
					{
						(play = new Button
						{
							Content = "▶",
						}),
						test.Output
					}
				});
				play.Click += async (snd, e) => await test.Run();
			}
		}

		public void Run(params Expression<Action>[] tests) => Run(tests.Select(GetTest).ToArray());
		public void Run(params Expression<Func<Task>>[] tests) => Run(tests.Select(GetTest).ToArray());

		private async void Run(params Test[] tests)
		{
			try
			{
				_isRunning = true;
				UpdateStatus();

				foreach (var test in tests)
				{
					await test.Run();
				}
			}
			finally
			{
				_isRunning = false;
				UpdateStatus();
			}
		}

		private Test GetTest(LambdaExpression test)
		{
			var testMethod = (test.Body as MethodCallExpression)?.Method;
			if (testMethod == null
				|| testMethod.Name.IsNullOrWhiteSpace()
				|| !_tests.TryGetValue(testMethod.Name, out var t))
			{
				throw new InvalidOperationException("Failed to get test");
			}

			return t;
		}

		private void UpdateStatus()
		{
			if (_isRunning || _tests.Values.Any(t => t.LastResult == TestResult.Pending))
			{
				_status.Text = "RUNNING";
			}
			else if (_tests.Values.Any(t => t.LastResult == TestResult.Failure))
			{
				_status.Text = "FAILED";
			}
			else
			{
				_status.Text = "SUCCESS";
			}
		}

		private class Test
		{
			private readonly TestRunner _owner;
			private readonly MethodInfo _testMethod;

			public Test(TestRunner owner, MethodInfo testMethod)
			{
				_owner = owner;
				_testMethod = testMethod;

				Name = testMethod.Name;
				Output = new TextBlock
				{
					Name = Name + "_Result",
					Text = "🟨 " + Name,
					TextWrapping = TextWrapping.Wrap,
					VerticalAlignment = VerticalAlignment.Center
				};
			}

			public string Name { get; }

			public TextBlock Output { get; }

			public TestResult LastResult { get; private set; }

			public async Task Run()
			{
				try
				{
					LastResult = TestResult.Pending;
					_owner.UpdateStatus();

					var result = _testMethod.Invoke(_owner._target, null);

					if (result is Task t)
					{
						await t;
					}

					LastResult = TestResult.Success;
					Output.Text = $"🟩 {_testMethod.Name}: SUCCESS";
				}
				catch (TargetInvocationException e)
				{
					LastResult = TestResult.Failure;
					Output.Text = $"🟥 {_testMethod.Name}: FAILED ({e.InnerException?.Message ?? e.Message})";
					Console.WriteLine($"{_testMethod.Name}: FAILED");
					Console.WriteLine(e.InnerException ?? e);
				}
				catch (Exception e)
				{
					LastResult = TestResult.Failure;
					Output.Text = $"🟥 {_testMethod.Name}: FAILED ({e.Message})";
					Console.WriteLine($"{_testMethod.Name}: FAILED");
					Console.WriteLine(e);
				}
				finally
				{
					_owner.UpdateStatus();
				}
			}
		}

		private enum TestResult
		{
			None = 0,
			Pending,
			Success,
			Failure,
		}
	}

	internal static class Assert
	{
		public static void IsTrue(bool actual)
		{
			if (!actual)
			{
				throw new AssertionFailedException(true, actual);
			}
		}

		public static void IsTrue(bool actual, string message)
		{
			if (!actual)
			{
				throw new AssertionFailedException(true, actual, message);
			}
		}

		public static void AreEqual(object expected, object actual)
		{
			if (!EqualityComparer<object>.Default.Equals(expected, actual))
			{
				throw new AssertionFailedException(expected, actual);
			}
		}

		public static void AreEqual(object expected, object actual, string message)
		{
			if (!EqualityComparer<object>.Default.Equals(expected, actual))
			{
				throw new AssertionFailedException(expected, actual, message);
			}
		}
	}

	internal class AssertionFailedException : Exception
	{
		public AssertionFailedException(object expected, object actual)
			: base($"Assertion failed\r\n\texpected: '{expected?.ToString() ?? "null"}'\r\n\tactual: '{actual?.ToString() ?? "null"}'")
		{
		}

		public AssertionFailedException(object expected, object actual, string message)
			: base($"Assertion failed {message}\r\n\texpected: '{expected?.ToString() ?? "null"}'\r\n\tactual: '{actual?.ToString() ?? "null"}'")
		{
		}
	}
}
