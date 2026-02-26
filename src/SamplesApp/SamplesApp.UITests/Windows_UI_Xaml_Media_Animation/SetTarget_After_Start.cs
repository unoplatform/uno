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
		[ActivePlatforms(Platform.Browser)] // Flaky on iOS/Android native https://github.com/unoplatform/uno/issues/22688
		public void When_Target_Is_Set_After_Start()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Media_Animation.SetTargetProperty");
			var playBtn = _app.Marked("playButton");

			var animatedRect = _app.Query("AnimatedRect");
			var animatedRectRect = animatedRect.Single().Rect.ToRectangle();

			var container = _app.Query("Container");
			var containerRect = container.Single().Rect.ToRectangle();

			const int Tolerance = 7;
			Assert.AreEqual(animatedRectRect.X, containerRect.X, Tolerance);
			Assert.AreEqual(animatedRectRect.Y, containerRect.Y, Tolerance);

			playBtn.FastTap();

			_app.WaitForDependencyPropertyValue(_app.Marked("AnimationState"), "Text", "Completed!");

			animatedRectRect = _app.Query("AnimatedRect").Single().Rect.ToRectangle();

			// The rect should move horizontally.
			Assert.AreEqual(animatedRectRect.Right, containerRect.Right, Tolerance);
			Assert.AreEqual(animatedRectRect.Y, containerRect.Y, Tolerance);

			// Change the direction
			_app.Marked("IsDirectionHorizontalToggle").SetDependencyPropertyValue("IsOn", "True");

			_app.WaitForDependencyPropertyValue(_app.Marked("AnimationState"), "Text", "Completed!");

			animatedRectRect = _app.Query("AnimatedRect").Single().Rect.ToRectangle();

			// The rect should move vertically.
			Assert.AreEqual(animatedRectRect.Right, containerRect.Right, Tolerance);
			Assert.AreEqual(animatedRectRect.Bottom, containerRect.Bottom, Tolerance);

			// Toggle the rect
			var animatedRect2 = _app.Query("AnimatedRect2");
			var animatedRect2Rect = animatedRect2.Single().Rect.ToRectangle();

			var container2 = _app.Query("Container2");
			var container2Rect = container2.Single().Rect.ToRectangle();

			Assert.AreEqual(animatedRect2Rect.X, container2Rect.X, Tolerance);
			Assert.AreEqual(animatedRect2Rect.Y, container2Rect.Y, Tolerance);

			_app.Marked("AnimatedRectSwitch").SetDependencyPropertyValue("IsOn", "True");

			_app.WaitForDependencyPropertyValue(_app.Marked("AnimationState"), "Text", "Completed!");

			animatedRect2Rect = _app.Query("AnimatedRect2").Single().Rect.ToRectangle();

			Assert.AreEqual(animatedRect2Rect.X, container2Rect.X, Tolerance);
			Assert.AreEqual(animatedRect2Rect.Bottom, container2Rect.Bottom, Tolerance);
		}
	}
}
