using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[SampleControlInfo("Pointers", "Sequence")]
	public sealed partial class EventsSequences : Page
	{
		private readonly List<(RoutedEvent evt, RoutedEventArgs args)> _tapResult = new List<(RoutedEvent, RoutedEventArgs)>();

		public EventsSequences()
		{
			this.InitializeComponent();

			SetupEvents(TestTapTarget, _tapResult);
		}

		private void ClearTapTest(object sender, RoutedEventArgs e)
		{
			_tapResult.Clear();
			Output.Text = "";
			TestTapResult.Text = "** no result **";
		}

		private void ValidateTapTest(object sender, RoutedEventArgs e)
		{
			var args = new EventSequenceValidator(_tapResult);
			var result =
				args.One(PointerEnteredEvent)
				&& args.One(PointerPressedEvent)
				&& args.MaybeSome(PointerMovedEvent)
				&& args.One(PointerReleasedEvent)
				&& args.One(TappedEvent)
				&& args.One(PointerExitedEvent)
				&& args.End();

			TestTapResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void SetupEvents(UIElement target, IList<(RoutedEvent, RoutedEventArgs)> events, string name = null, bool captureOnPress = false)
		{
			name = name ?? (target as FrameworkElement)?.Name ?? $"{target.GetType().Name}:{target.GetHashCode():X6}";

			target.PointerEntered += (snd, e) =>
			{
				OnPointerEvent(PointerEnteredEvent, "Entered", e);
			};
			target.PointerPressed += (snd, e) =>
			{
				OnPointerEvent(PointerPressedEvent, "Pressed", e);
				if (captureOnPress)
				{
					var captured = target.CapturePointer(e.Pointer);
					Log($"[{name}] Captured: {captured}");
				}
			};
			target.PointerMoved += (snd, e) =>
			{
				OnPointerEvent(PointerMovedEvent, "Moved", e);
			};
			target.PointerReleased += (snd, e) =>
			{
				OnPointerEvent(PointerReleasedEvent, "Released", e);
			};
			target.PointerCanceled += (snd, e) =>
			{
				OnPointerEvent(PointerCanceledEvent, "Canceled", e);
			};
			target.PointerExited += (snd, e) =>
			{
				OnPointerEvent(PointerExitedEvent, "Exited", e);
			};
			target.PointerCaptureLost += (snd, e) =>
			{
				OnPointerEvent(PointerCaptureLostEvent, "CaptureLost", e);
			};

			// Those events are built using the GestureRecognizer
			target.Tapped += (snd, e) =>
			{
				OnEvent(TappedEvent, "Tapped", e);
			};
			target.DoubleTapped += (snd, e) =>
			{
				OnEvent(DoubleTappedEvent, "DoubleTapped", e);
			};

			void OnEvent(RoutedEvent evt, string evtName, RoutedEventArgs e)
			{
				events.Add((evt, e));
				Log($"[{name}] {evtName}");
			}

			void OnPointerEvent(RoutedEvent evt, string evtName, PointerRoutedEventArgs e)
			{
				events.Add((evt, e));

				var point = e.GetCurrentPoint(this);
				Log($"[{name}] {evtName}: id={e.Pointer.PointerId} "
					+ $"| frame={point.FrameId}"
					+ $"| type={e.Pointer.PointerDeviceType} "
					+ $"| position={point.Position} "
					+ $"| rawPosition={point.RawPosition} "
					+ $"| inContact={point.IsInContact} "
					+ $"| inRange={point.Properties.IsInRange} "
					+ $"| primary={point.Properties.IsPrimary}"
					+ $"| intermediates={e.GetIntermediatePoints(this)?.Count.ToString() ?? "null"}");
			}
		}

		private void Log(string message)
		{
			System.Diagnostics.Debug.WriteLine(message);
			Output.Text += message + "\r\n";
		}

		private class EventSequenceValidator
		{
			private readonly IList<(RoutedEvent evt, RoutedEventArgs args)> _args;
			private int _index = 0;

			public EventSequenceValidator(IList<(RoutedEvent evt, RoutedEventArgs args)> args)
			{
				_args = args;
			}

			/// <summary>
			/// [1..1]
			/// </summary>
			public bool One(RoutedEvent evt)
				=> _index < _args.Count && _args[_index++].evt == evt;

			/// <summary>
			/// [1..*]
			/// </summary>
			public bool Some(RoutedEvent evt)
				=> One(evt) && MaybeSome(evt);

			/// <summary>
			/// [0..1]
			/// </summary>
			public bool MaybeOne(RoutedEvent evt)
			{
				if (_index < _args.Count &&  _args[_index].evt == evt)
				{
					++_index;
				}
				return true;
			}

			/// <summary>
			/// [0..*]
			/// </summary>
			public bool MaybeSome(RoutedEvent evt)
			{
				while (_index < _args.Count && _args[_index].evt == evt)
				{
					++_index;
				}
				return true;
			}

			public bool End()
				=> _index >= _args.Count;
		}
	}
}
