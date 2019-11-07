using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Point = Windows.Foundation.Point;
using static Uno.UI.Tests.Windows_UI_Input.GestureRecognizerTestExtensions;

namespace Uno.UI.Tests.Windows_UI_Input
{
	[TestClass]
	public class Given_GestureRecognizer
	{
		[TestMethod]
		public void Tapped()
		{
			var sut = new GestureRecognizer {GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			taps.Should().BeEmpty();

			sut.CanBeDoubleTap(GetPoint(28, 28)).Should().BeFalse();
			sut.ProcessUpEvent(27, 27);
			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void Tapped_Duration()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			taps.Should().BeEmpty();

			sut.CanBeDoubleTap(GetPoint(28, 28)).Should().BeFalse();
			sut.ProcessUpEvent(27, 27, ts: long.MaxValue); // No matter the duration
			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void Tapped_Delta_X()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessUpEvent(25 + GestureRecognizer.TapMaxXDelta + 1, 25); // Moved too far

			taps.Should().BeEmpty();
		}

		[TestMethod]
		public void Tapped_Delta_X_Back_Over()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + GestureRecognizer.TapMaxXDelta + 1, 25); // Moved too far
			sut.ProcessUpEvent(25, 25); // Release over

			taps.Should().BeEmpty();
		}

		[TestMethod]
		public void Tapped_Delta_Y()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessUpEvent(25, 25 + GestureRecognizer.TapMaxXDelta + 1); // Moved too far

			taps.Should().BeEmpty();
		}

		[TestMethod]
		public void Tapped_Delta_Y_Back_Over()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25, 25 + GestureRecognizer.TapMaxXDelta + 1); // Moved too far
			sut.ProcessUpEvent(25, 25); // Release over

			taps.Should().BeEmpty();
		}

		[TestMethod]
		public void DoubleTapped()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap | GestureSettings.DoubleTap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(26, 26);
			taps.Should().BeEmpty();

			sut.ProcessUpEvent(27, 27);
			taps.Should().BeEquivalentTo(Tap(25, 25));

			sut.ProcessDownEvent(28, 28);
			taps.Should().BeEquivalentTo(Tap(25, 25), Tap(28, 28, 2));
		}

		[TestMethod]
		public void DoubleTapped_Without_Tapped()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.DoubleTap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			taps.Should().BeEmpty();

			sut.ProcessMoveEvent(26, 26);


			sut.CanBeDoubleTap(GetPoint(28, 28)).Should().BeFalse();
			sut.ProcessUpEvent(GetPoint(27, 27, ts: long.MaxValue)); // No matter the duration
			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void DoubleTapped_Duration()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap | GestureSettings.DoubleTap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			// Tapped
			sut.ProcessDownEvent(25, 25, ts: 0);
			sut.ProcessUpEvent(26, 26, ts: 1);

			taps.Should().BeEquivalentTo(Tap(25, 25));

			// Double tapped
			var tooSlow = GetPoint(25, 25, ts: 1 + GestureRecognizer.MultiTapMaxDelayTicks + 1);
			sut.CanBeDoubleTap(tooSlow).Should().BeFalse();
			sut.ProcessDownEvent(tooSlow);

			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void DoubleTapped_Delta_X()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap | GestureSettings.DoubleTap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessUpEvent(25, 25);
			sut.ProcessDownEvent(25 + GestureRecognizer.TapMaxXDelta + 1, 25); // Second tap too far

			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void DoubleTapped_Delta_Y()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.Tap | GestureSettings.DoubleTap };
			var taps = new List<TappedEventArgs>();
			sut.Tapped += (snd, e) => taps.Add(e);

			sut.ProcessDownEvent(25, 25);
			sut.ProcessUpEvent(25, 25);
			sut.ProcessDownEvent(25, 25 + GestureRecognizer.TapMaxYDelta + 1); // Second tap too far

			taps.Should().BeEquivalentTo(Tap(25, 25));
		}

		[TestMethod]
		public void Manipulation_Begin()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);
			var step = GestureRecognizer.Manipulation.StartTouch.TranslateX;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + 1, 25); // Ignored
			sut.ProcessMoveEvent(25 + step, 25);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + step, 25).WithDelta(step, 0).WithCumulative(step, 0)
			);
		}

		[TestMethod]
		public void Manipulation_Begin_MultiPointer()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);
			sut.ProcessDownEvent(25, 25, id: 2);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started()
			);
		}

		[TestMethod]
		public void Manipulation_Begin_WithAnotherDevice()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);
			sut.ProcessDownEvent(25, 25, id: 2, device: PointerDeviceType.Pen);

			result.ShouldBe(
				v => v.Starting()
			);
		}

		[TestMethod]
		public void Manipulation_Delta()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);
			var step = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + 1, 25);
			sut.ProcessMoveEvent(25 + step, 25);
			sut.ProcessMoveEvent(25 + step * 2, 25);
			sut.ProcessMoveEvent(25 + step * 2 + 1, 25);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + step, 25).WithDelta(step, 0).WithCumulative(step, 0),
				v => v.Delta().At(25 + step * 2, 25).WithDelta(step, 0).WithCumulative(step * 2, 0)
			);
		}

		[TestMethod]
		public void Manipulation_End()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);
			var step = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + step, 25);
			sut.ProcessUpEvent(25 + step + 1, 25);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + step, 25).WithDelta(step, 0).WithCumulative(step, 0),
				v => v.End().At(25 + step + 1, 25).WithCumulative(step + 1, 0)
			);
		}

		[TestMethod]
		public void Manipulation_End_2()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);
			var step = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + step, 25);
			sut.ProcessMoveEvent(25 + step * 2, 25);
			sut.ProcessMoveEvent(25 + step * 2 + 1, 25);
			sut.ProcessUpEvent(25 + step * 2 + 2, 25);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + step, 25).WithDelta(step, 0).WithCumulative(step, 0),
				v => v.Delta().At(25 + step * 2, 25).WithDelta(step, 0).WithCumulative(step * 2, 0),
				v => v.End().At(25 + step * 2 + 2, 25).WithCumulative(step * 2 + 2, 0)
			);
		}

		[TestMethod]
		public void Manipulation_TranslateXOnly()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateX };
			var result = new ManipulationRecorder(sut);
			var stepX = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;
			var stepY = GestureRecognizer.Manipulation.StartTouch.TranslateY + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25, 25 + stepY); // Invalid move that should NOT cause the started
			sut.ProcessMoveEvent(25 + stepX, 25 + stepY); // Valid move that should cause a started ... but without Y
			sut.ProcessMoveEvent(25 + stepX, 25 + stepY * 2); // Invalid move that should also be muted
			sut.ProcessMoveEvent(25 + stepX * 2, 25 + stepY * 2); // Invalid move that should also be muted
			sut.ProcessUpEvent(25 + stepX * 2 + 1, 25 + stepY * 2 + 1);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + stepX, 25 + stepY).WithDelta(stepX, 0).WithCumulative(stepX, 0),
				v => v.Delta().At(25 + stepX * 2, 25 + stepY * 2).WithDelta(stepX, 0).WithCumulative(stepX * 2, 0),
				v => v.End().At(25 + stepX * 2 + 1, 25 + stepY * 2 + 1).WithCumulative(stepX * 2 + 1, 0)
			);
		}

		[TestMethod]
		public void Manipulation_TranslateYOnly()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateY };
			var result = new ManipulationRecorder(sut);
			var stepX = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;
			var stepY = GestureRecognizer.Manipulation.StartTouch.TranslateY + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 + stepX, 25); // Invalid move that should NOT cause the started
			sut.ProcessMoveEvent(25 + stepX, 25 + stepY); // Valid move that should cause a started ... but without Y
			sut.ProcessMoveEvent(25 + stepX * 2, 25 + stepY); // Invalid move that should also be muted
			sut.ProcessMoveEvent(25 + stepX * 2, 25 + stepY * 2); // Invalid move that should also be muted
			sut.ProcessUpEvent(25 + stepX * 2 + 1, 25 + stepY * 2 + 1);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 + stepX, 25 + stepY).WithDelta(0, stepY).WithCumulative(0, stepY),
				v => v.Delta().At(25 + stepX * 2, 25 + stepY * 2).WithDelta(0, stepY).WithCumulative(0, stepY * 2),
				v => v.End().At(25 + stepX * 2 + 1, 25 + stepY * 2 + 1).WithCumulative(0, stepX * 2 + 1)
			);
		}

		[TestMethod]
		public void Manipulation_TranslateXOnly_Negative()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateX };
			var result = new ManipulationRecorder(sut);
			var stepX = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;
			var stepY = GestureRecognizer.Manipulation.StartTouch.TranslateY + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25, 25 + stepY); // Invalid move that should NOT cause the started
			sut.ProcessMoveEvent(25 - stepX, 25 - stepY); // Valid move that should cause a started ... but without Y
			sut.ProcessMoveEvent(25 - stepX, 25 - stepY * 2); // Invalid move that should also be muted
			sut.ProcessMoveEvent(25 - stepX * 2, 25 - stepY * 2); // Invalid move that should also be muted
			sut.ProcessUpEvent(25 - stepX * 2 - 1, 25 - stepY * 2 - 1);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 - stepX, 25 - stepY).WithDelta(-stepX, 0).WithCumulative(-stepX, 0),
				v => v.Delta().At(25 - stepX * 2, 25 - stepY * 2).WithDelta(-stepX, 0).WithCumulative(-stepX * 2, 0),
				v => v.End().At(25 - stepX * 2 - 1, 25 - stepY * 2 - 1).WithCumulative(-stepX * 2 - 1, 0)
			);
		}

		[TestMethod]
		public void Manipulation_TranslateYOnly_Negative()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateY };
			var result = new ManipulationRecorder(sut);
			var stepX = GestureRecognizer.Manipulation.StartTouch.TranslateX + 1;
			var stepY = GestureRecognizer.Manipulation.StartTouch.TranslateY + 1;

			sut.ProcessDownEvent(25, 25);
			sut.ProcessMoveEvent(25 - stepX, 25); // Invalid move that should NOT cause the started
			sut.ProcessMoveEvent(25 - stepX, 25 - stepY); // Valid move that should cause a started ... but without Y
			sut.ProcessMoveEvent(25 - stepX * 2, 25 - stepY); // Invalid move that should also be muted
			sut.ProcessMoveEvent(25 - stepX * 2, 25 - stepY * 2); // Invalid move that should also be muted
			sut.ProcessUpEvent(25 - stepX * 2 - 1, 25 - stepY * 2 - 1);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(25 - stepX, 25 - stepY).WithDelta(0, -stepY).WithCumulative(0, -stepY),
				v => v.Delta().At(25 - stepX * 2, 25 - stepY * 2).WithDelta(0, -stepY).WithCumulative(0, -stepY * 2),
				v => v.End().At(25 - stepX * 2 - 1, 25 - stepY * 2 - 1).WithCumulative(0, -stepX * 2 - 1)
			);
		}

		[TestMethod]
		public void Manipulation_Translate_MultiTouch()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(0, 0, id: 1);

			sut.ProcessDownEvent(50, 50, id: 2);
			sut.ProcessMoveEvent(200, 200, id: 2);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(25, 25).WithEmptyCumulative(),
				v => v.Delta().At(100, 100).WithDelta(tX: 75, tY: 75).WithCumulative(tX: 75, tY: 75)
			);
		}

		[TestMethod]
		public void Manipulation_Translate_MultiTouch_Negative()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateY };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(0, 0, id: 1);

			sut.ProcessDownEvent(200, 200, id: 2);
			sut.ProcessMoveEvent(50, 50, id: 2);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().At(100, 100).WithEmptyCumulative(),
				v => v.Delta().At(25, 25).WithDelta(tX: -75, tY: -75).WithCumulative(tX: -75, tY: -75)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_Trigonometric_InFirstQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, -25, id: 1);

			sut.ProcessDownEvent(50, -25, id: 2); // Angle = 0
			sut.ProcessMoveEvent(50, -50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(25, -50, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 315),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 270)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_Trigonometric_InSecondQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(-25, -25, id: 1);

			sut.ProcessDownEvent(-25, -50, id: 2); // Angle = 0
			sut.ProcessMoveEvent(-50, -50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(-50, -25, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 315),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 270)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_Trigonometric_InThirdQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(-25, 25, id: 1);

			sut.ProcessDownEvent(-50, 25, id: 2); // Angle = 0
			sut.ProcessMoveEvent(-50, 50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(-25, 50, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 315),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 270)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_Trigonometric_InForthQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);

			sut.ProcessDownEvent(25, 50, id: 2); // Angle = 0
			sut.ProcessMoveEvent(50, 50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(50, 25, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 315),
				v => v.Delta().WithDelta(angle: 315).WithCumulative(angle: 270)
			);
		}


		[TestMethod]
		public void Manipulation_RotateOnly_AntiTrigonometric_InFirstQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, -25, id: 1);

			sut.ProcessDownEvent(25, -50, id: 2); // Angle = 0
			sut.ProcessMoveEvent(50, -50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(50, -25, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 45),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 90)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_AntiTrigonometric_InSecondQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(-25, -25, id: 1);

			sut.ProcessDownEvent(-50, -25, id: 2); // Angle = 0
			sut.ProcessMoveEvent(-50, -50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(-25, -50, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 45),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 90)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_AntiTrigonometric_InThirdQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(-25, 25, id: 1);

			sut.ProcessDownEvent(-25, 50, id: 2); // Angle = 0
			sut.ProcessMoveEvent(-50, 50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(-50, 25, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 45),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 90)
			);
		}

		[TestMethod]
		public void Manipulation_RotateOnly_AntiTrigonometric_InForthQuadrant()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationRotate };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);

			sut.ProcessDownEvent(50, 25, id: 2); // Angle = 0
			sut.ProcessMoveEvent(50, 50, id: 2); // Angle = -Pi/4
			sut.ProcessMoveEvent(25, 50, id: 2); // Angle = -Pi/2

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(angle: 0),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 45),
				v => v.Delta().WithDelta(angle: 45).WithCumulative(angle: 90)
			);
		}

		[TestMethod]
		public void Manipulation_ScaleOnly()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationScale };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);

			sut.ProcessDownEvent(50, 25, id: 2);
			sut.ProcessMoveEvent(100, 25, id: 2);
			sut.ProcessMoveEvent(150, 25, id: 2);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(scale: 1),
				v => v.Delta().WithDelta(scale: 3, exp: 50).WithCumulative(scale: 3, exp: 50),
				v => v.Delta().WithDelta(scale: 5F/3F, exp: 50).WithCumulative(scale: 5, exp: 100)
			);
		}

		[TestMethod]
		public void Manipulation_ScaleOnly_Negative()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettings.ManipulationScale };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(25, 25, id: 1);

			sut.ProcessDownEvent(150, 25, id: 2);
			sut.ProcessMoveEvent(100, 25, id: 2);
			sut.ProcessMoveEvent(75, 25, id: 2);

			result.ShouldBe(
				v => v.Starting(),
				v => v.Started().WithCumulative(scale: 1),
				v => v.Delta().WithDelta(scale: .6F, exp: -50).WithCumulative(scale: .6F, exp: -50),
				v => v.Delta().WithDelta(scale: 2F/3F, exp: -25).WithCumulative(scale: .4F, exp: -75)
			);
		}

		[TestMethod]
		public void Manipulation_Mixed()
		{
			var sut = new GestureRecognizer { GestureSettings = GestureSettingsHelper.Manipulations };
			var result = new ManipulationRecorder(sut);

			sut.ProcessDownEvent(50, 50, id: 1); // 1.
			sut.ProcessDownEvent(50, 200, id: 2); // 2.

			sut.ProcessMoveEvent(200, 200, id: 2); // 3.
			sut.ProcessMoveEvent(50, 200, id: 1); // 4.

			sut.ProcessMoveEvent(200, 50, id: 2); // 5.
			sut.ProcessMoveEvent(200, 200, id: 1); // 6.

			sut.ProcessMoveEvent(50, 50, id: 2); // 7.
			sut.ProcessMoveEvent(200, 50, id: 1); // 8.

			sut.ProcessMoveEvent(50, 200, id: 2); // 9.
			sut.ProcessMoveEvent(50, 50, id: 1); // 10.

			// At same point (0 proof ?)
			sut.ProcessMoveEvent(50, 50, id: 2); // 11.

			var s = (float)Math.Sqrt(2);
			var e = (float)((Math.Sqrt(2) - 1) * 150);

			result.ShouldBe(
				v => v.Starting(), // 1.

				// 2.
				v => v.Started()
					.At(50, 125)
					.WithCumulative(),

				// 3.
				v => v.Delta()
					.At(125, 125)
					.WithDelta(tX: 75, tY: 0, angle: 315, scale: s, exp: e)
					.WithCumulative(tX: 75, tY: 0, angle: 315, scale: s, exp: e),

				// 4.
				v => v.Delta()
					.At(125, 200)
					.WithDelta(tX: 0, tY: 75, angle: 315, scale: 1/s, exp: -e)
					.WithCumulative(tX: 75, tY: 75, angle: 270, scale: 1, exp: 0),

				// 5.
				v => v.Delta()
					.At(125, 125)
					.WithDelta(tX: 0, tY: -75, angle: 315, scale: s, exp: e)
					.WithCumulative(tX: 75, tY: 0, angle: 225, scale: s, exp: e),

				// 6.
				v => v.Delta()
					.At(200, 125)
					.WithDelta(tX: 75, tY: 0, angle: 315, scale: 1 / s, exp: -e)
					.WithCumulative(tX: 150, tY: 0, angle: 180, scale: 1, exp: 0),

				// 7.
				v => v.Delta()
					.At(125, 125)
					.WithDelta(tX: -75, tY: 0, angle: 315, scale: s, exp: e)
					.WithCumulative(tX: 75, tY: 0, angle: 135, scale: s, exp: e),

				// 8.
				v => v.Delta()
					.At(125, 50)
					.WithDelta(tX: 0, tY: -75, angle: 315, scale: 1 / s, exp: -e)
					.WithCumulative(tX: 75, tY: -75, angle: 90, scale: 1, exp: 0),

				// 9.
				v => v.Delta()
					.At(125, 125)
					.WithDelta(tX: 0, tY: 75, angle: 315, scale: s, exp: e)
					.WithCumulative(tX: 75, tY: 0, angle: 45, scale: s, exp: e),

				// 10.
				v => v.Delta()
					.At(50, 125)
					.WithDelta(tX: -75, tY: 0, angle: 315, scale: 1 / s, exp: -e)
					.WithCumulative(tX: 0, tY: 0, angle: 0, scale: 1, exp: 0),

				// 11.
				v => v.Delta()
					.At(50, 50)
					.WithDelta(tX: 0, tY: -75, angle: 90 /* ??? */, scale: 0, exp: -150F)
					.WithCumulative(tX: 0, tY: -75, angle: 90 /* ??? */, scale: 0, exp: -150F)
			);
		}
	}

	internal class ManipulationRecorder
	{
		private readonly GestureRecognizer _recognizer;

		private List<(GestureRecognizer snd, object args)> _result = new List<(GestureRecognizer, object)>();

		public ManipulationRecorder(GestureRecognizer recognizer)
		{
			_recognizer = recognizer;
			recognizer.ManipulationStarting += (snd, e) => _result.Add((snd, e));
			recognizer.ManipulationStarted += (snd, e) => _result.Add((snd, e));
			recognizer.ManipulationUpdated += (snd, e) => _result.Add((snd, e));
			recognizer.ManipulationCompleted += (snd, e) => _result.Add((snd, e));
		}

		public void ShouldBe(params Func<EmptyValidator, Validator>[] expected)
		{
			if (expected.Length != _result.Count)
			{
				throw new AssertionFailedException(
					$"Not the same number of events. Expected: {expected.Length} actual: {_result.Count}"
					+ $"\r\nExpected: \r\n\t{string.Join("\r\n\t", expected.Select((e, i) => $"[{i+1:D3}] + {e(Validator.Empty)}"))}"
					+ $"\r\nActual: \r\n\t{string.Join("\r\n\t", _result.Select((r, i) => $"[{i+1:D3}] + {r.args.GetType().Name}"))}");
			}

			for (var i = 0; i < expected.Length; i++)
			{
				var config = expected[i];
				var actual = _result[i];

				if (actual.snd != _recognizer)
				{
					throw new AssertionFailedException($"args {i + 1} of {expected.Length}: sender is not the sut.");
				}

				config(Validator.Empty).Assert(actual.args, $"args {i + 1} of {expected.Length}");
			}
		}

		public abstract class Validator
		{
			public static EmptyValidator Empty { get; } = new EmptyValidator();

			internal abstract void Assert(object args, string identifier);
		}

		public abstract class Validator<TArgs> : Validator
		{
			private string _pendingAssertIdentifier;
			private List<(string name, object expected, object actual)> _pendingAssertResult;

			internal override void Assert(object args, string identifier)
			{
				try
				{
					_pendingAssertIdentifier = identifier;
					if (!(args is TArgs t))
					{
						Failed($"is not of the expected type: {typeof(TArgs).Name}");
						return;
					}

					_pendingAssertResult = new List<(string, object, object)>();

					Assert(t);

					if (_pendingAssertResult.Any())
					{
						Failed("has some unexpected values: \r\n" + string.Join("\r\n", _pendingAssertResult.Select(r => $"\t{r.name} (expected: {r.expected} / actual: {r.actual})")));
					}
				}
				catch (Exception e) when (!(e is AssertionFailedException))
				{
					throw new AssertionFailedException($"{identifier} validation failed: {e.Message}");
				}
				finally
				{
					_pendingAssertResult = null;
				}
			}

			protected abstract void Assert(TArgs args);

			protected void Failed(string reason)
				=> throw new AssertionFailedException($"{_pendingAssertIdentifier} {reason}");

			protected void AreEquals(string name, Point? expected, Point actual)
			{
				if (expected.HasValue && expected.Value != actual)
					_pendingAssertResult.Add((name, expected.Value, actual));
			}

			protected void AreEquals(string name, bool? expected, bool actual)
			{
				if (expected.HasValue && expected.Value != actual)
					_pendingAssertResult.Add((name, expected.Value, actual));
			}

			protected void AreEquals(string name, ManipulationDelta? expected, ManipulationDelta actual)
			{
				if (!expected.HasValue)
				{
					return;
				}

				var e = expected.Value;

				if (e.Translation.X != actual.Translation.X)
					_pendingAssertResult.Add((name + ".Translation.X", expected.Value.Translation.X, actual.Translation.X));

				if (e.Translation.Y != actual.Translation.Y)
					_pendingAssertResult.Add((name + ".Translation.Y", expected.Value.Translation.Y, actual.Translation.Y));

				if (Math.Abs(e.Rotation - actual.Rotation) > .00001)
					_pendingAssertResult.Add((name + ".Rotation", expected.Value.Rotation, actual.Rotation));

				if (Math.Abs(e.Scale - actual.Scale) > .00001)
					_pendingAssertResult.Add((name + ".Scale", expected.Value.Scale, actual.Scale));

				if (Math.Abs(e.Expansion - actual.Expansion) > .00001)
					_pendingAssertResult.Add((name + ".Expansion", expected.Value.Expansion, actual.Expansion));
			}

			/// <inheritdoc />
			public override string ToString()
				=> typeof(TArgs).Name;
		}

		public class EmptyValidator : Validator
		{
			public StartingValidator Starting() => new StartingValidator();
			public StartedValidator Started() => new StartedValidator();
			public DeltaValidator Delta() => new DeltaValidator();
			public EndValidator End() => new EndValidator();

			internal override void Assert(object args, string identifier)
				=> throw new AssertionFailedException($"Expected of {identifier} is not configured");
		}

		public class StartingValidator : Validator<ManipulationStartingEventArgs>
		{
			protected override void Assert(ManipulationStartingEventArgs args)
			{
			}
		}

		public class StartedValidator : Validator<ManipulationStartedEventArgs>
		{
			private Point? _position;
			private ManipulationDelta? _cumulative;

			public StartedValidator()
			{
			}

			public StartedValidator At(double x, double y)
			{
				_position = new Point(x, y);
				return this;
			}

			public StartedValidator WithEmptyCumulative()
			{
				_cumulative = ManipulationDelta.Empty;
				return this;
			}

			public StartedValidator WithCumulative(double tX = 0, double tY = 0, float angle = 0, float scale = 1, float exp = 0)
			{
				_cumulative = new ManipulationDelta
				{
					Translation = new Point(tX, tY),
					Rotation = angle,
					Scale = scale,
					Expansion = exp,
				};
				return this;
			}

			protected override void Assert(ManipulationStartedEventArgs args)
			{
				AreEquals(nameof(args.Position), _position, args.Position);
				AreEquals(nameof(args.Cumulative), _cumulative, args.Cumulative);
			}
		}

		public class DeltaValidator : Validator<ManipulationUpdatedEventArgs>
		{
			private Point? _position;
			private ManipulationDelta? _delta;
			private ManipulationDelta? _cumulative;
			private bool? _isInertial;

			public DeltaValidator At(double x, double y)
			{
				_position = new Point(x, y);
				return this;
			}

			public DeltaValidator WithDelta(double tX = 0, double tY = 0, float angle = 0, float scale = 1, float exp = 0)
			{
				_delta = new ManipulationDelta
				{
					Translation = new Point(tX, tY),
					Rotation = angle,
					Scale = scale,
					Expansion = exp,
				};
				return this;
			}

			public DeltaValidator WithCumulative(double tX = 0, double tY = 0, float angle = 0, float scale = 1, float exp = 0)
			{
				_cumulative = new ManipulationDelta
				{
					Translation = new Point(tX, tY),
					Rotation = angle,
					Scale = scale,
					Expansion = exp,
				};
				return this;
			}

			public DeltaValidator IsNotInertial() => IsInertial(false);
			public DeltaValidator IsInertial(bool isInertial = true)
			{
				_isInertial = isInertial;
				return this;
			}

			protected override void Assert(ManipulationUpdatedEventArgs args)
			{
				AreEquals(nameof(args.Position), _position, args.Position);
				AreEquals(nameof(args.Delta), _delta, args.Delta);
				AreEquals(nameof(args.Cumulative), _cumulative, args.Cumulative);
				AreEquals(nameof(args.IsInertial), _isInertial, args.IsInertial);
			}
		}

		public class EndValidator : Validator<ManipulationCompletedEventArgs>
		{
			private Point? _position;
			private ManipulationDelta? _cumulative;
			private bool? _isInertial;

			public EndValidator At(double x, double y)
			{
				_position = new Point(x, y);
				return this;
			}

			public EndValidator WithCumulative(double tX = 0, double tY = 0, float angle = 0, float scale = 1, float exp = 0)
			{
				_cumulative = new ManipulationDelta
				{
					Translation = new Point(tX, tY),
					Rotation = angle,
					Scale = scale,
					Expansion = exp,
				};
				return this;
			}

			public EndValidator IsNotInertial() => IsInertial(false);
			public EndValidator IsInertial(bool isInertial = true)
			{
				_isInertial = isInertial;
				return this;
			}

			protected override void Assert(ManipulationCompletedEventArgs args)
			{
				AreEquals(nameof(args.Position), _position, args.Position);
				AreEquals(nameof(args.Cumulative), _cumulative, args.Cumulative);
				AreEquals(nameof(args.IsInertial), _isInertial, args.IsInertial);
			}
		}
	}

	internal static class GestureRecognizerTestExtensions
	{
		private static long _frameId = 0;

		public static PointerPoint GetPoint(
			double x,
			double y,
			uint? id = null,
			ulong? ts = null,
			PointerDeviceType? device = null,
			bool? isInContact = true,
			PointerPointProperties properties = null)
		{
			var frameId = (uint)Interlocked.Increment(ref _frameId);
			id = id ?? 1;
			ts = ts ?? frameId;
			var pointer = new PointerDevice(device ?? PointerDeviceType.Touch);
			var location = new Windows.Foundation.Point(x, y);
			properties = properties ?? new PointerPointProperties
			{
				IsPrimary = true,
				IsInRange = true,
				IsLeftButtonPressed = true,
			};

			return new PointerPoint(frameId, ts.Value, pointer, id.Value, location, location, isInContact.GetValueOrDefault(), properties);
		}

		public static TappedEventArgs Tap(double x, double y, uint tapCount = 1, PointerDeviceType? device = null)
			=> new TappedEventArgs(device ?? PointerDeviceType.Touch, new Point(x, y), tapCount);

		public static void ProcessDownEvent(
			this GestureRecognizer sut,
			double x,
			double y,
			uint? id = null,
			ulong? ts = null,
			PointerDeviceType ? device = null,
			bool? isInContact = true,
			PointerPointProperties properties = null)
			=> sut.ProcessDownEvent(GetPoint(x, y, id, ts, device, isInContact, properties));

		public static void ProcessMoveEvent(
			this GestureRecognizer sut,
			double x,
			double y,
			uint? id = null,
			ulong? ts = null,
			PointerDeviceType? device = null,
			bool? isInContact = true,
			PointerPointProperties properties = null)
			=> sut.ProcessMoveEvent(GetPoint(x, y, id, ts, device, isInContact, properties));

		public static void ProcessMoveEvent(this GestureRecognizer sut, PointerPoint point)
			=> sut.ProcessMoveEvents(new[] { point });

		public static void ProcessUpEvent(
			this GestureRecognizer sut,
			double x,
			double y,
			uint? id = null,
			ulong? ts = null,
			PointerDeviceType? device = null,
			bool? isInContact = true,
			PointerPointProperties properties = null)
			=> sut.ProcessUpEvent(GetPoint(x, y, id, ts, device, isInContact, properties));
	}
}
