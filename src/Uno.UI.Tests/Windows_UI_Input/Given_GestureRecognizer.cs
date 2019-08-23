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
	}

	internal static class GestureRecognizerTestExtensions
	{
		private static long _pointerId = 0;

		public static PointerPoint GetPoint(
			double x,
			double y,
			uint? id = null,
			ulong? ts = null,
			PointerDeviceType? device = null,
			bool? isInContact = true,
			PointerPointProperties properties = null)
		{
			id = id ?? (uint)Interlocked.Increment(ref _pointerId);
			ts = ts ?? (ulong)id;
			var pointer = new PointerDevice(device ?? PointerDeviceType.Mouse);
			var location = new Windows.Foundation.Point(x, y);
			properties = properties ?? new PointerPointProperties
			{
				IsPrimary = true,
				IsInRange = true,
				IsLeftButtonPressed = true,
			};

			return new PointerPoint(id.Value, ts.Value, pointer, (uint)pointer.PointerDeviceType, location, location, isInContact.GetValueOrDefault(), properties);
		}

		public static TappedEventArgs Tap(double x, double y, uint tapCount = 1, PointerDeviceType? device = null)
			=> new TappedEventArgs(device ?? PointerDeviceType.Mouse, new Point(x, y), tapCount);

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
