using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Animation
{
	[TestFixture]
	public partial class SetTarget_After_Start : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public async Task When_Target_Is_Set_After_Start()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Media_Animation.SetTargetProperty");
			var playBtn = _app.Marked("playButton");

			var animatedRect = _app.Query("AnimatedRect");
			var animatedRectRect = animatedRect.Single().Rect.ToRectangle();

			var container = _app.Query("Container");
			var containerRect = container.Single().Rect.ToRectangle();

			// 5 is tolerance.
			Assert.LessOrEqual(Math.Abs(animatedRectRect.X - containerRect.X), 5);
			Assert.LessOrEqual(Math.Abs(animatedRectRect.Y - containerRect.Y), 5);

			playBtn.FastTap();
			await Task.Delay(1000); // Wait for animation.

			animatedRectRect = _app.Query("AnimatedRect").Single().Rect.ToRectangle();

			// The rect should move horizontally.
			Assert.LessOrEqual(Math.Abs(animatedRectRect.Right - containerRect.Right), 5);
			Assert.LessOrEqual(Math.Abs(animatedRectRect.Y - containerRect.Y), 5);

			// Change the direction
			_app.Marked("IsDirectionHorizontalToggle").SetDependencyPropertyValue("IsOn", "True");

			await Task.Delay(1000);

			animatedRectRect = _app.Query("AnimatedRect").Single().Rect.ToRectangle();

			// The rect should move vertically.
			Assert.LessOrEqual(Math.Abs(animatedRectRect.Right - containerRect.Right), 5);
			Assert.LessOrEqual(Math.Abs(animatedRectRect.Bottom - containerRect.Bottom), 5);

			// Toggle the rect
			var animatedRect2 = _app.Query("AnimatedRect2");
			var animatedRect2Rect = animatedRect2.Single().Rect.ToRectangle();

			var container2 = _app.Query("Container2");
			var container2Rect = container2.Single().Rect.ToRectangle();

			Assert.LessOrEqual(Math.Abs(animatedRect2Rect.X - container2Rect.X), 5);
			Assert.LessOrEqual(Math.Abs(animatedRect2Rect.Y - container2Rect.Y), 5);

			_app.Marked("AnimatedRectSwitch").SetDependencyPropertyValue("IsOn", "True");

			await Task.Delay(1000);

			animatedRect2Rect = _app.Query("AnimatedRect2").Single().Rect.ToRectangle();

			Assert.LessOrEqual(Math.Abs(animatedRect2Rect.X - container2Rect.X), 5);
			Assert.LessOrEqual(Math.Abs(animatedRect2Rect.Bottom - container2Rect.Bottom), 5);
		}
	}
}
