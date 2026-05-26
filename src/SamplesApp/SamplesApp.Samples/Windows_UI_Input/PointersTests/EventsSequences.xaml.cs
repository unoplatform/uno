using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using V = System.Collections.Generic.Dictionary<string, object>;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", Name = "Sequence")]
	public sealed partial class EventsSequences : Page
	{
		private readonly List<(object evt, RoutedEventArgs args)> _tapResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _clickResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _translatedTapResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _translatedClickResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _hyperlinkResult = new List<(object, RoutedEventArgs)>();
		private readonly List<(object evt, RoutedEventArgs args)> _listViewResult = new List<(object, RoutedEventArgs)>();

		private static readonly object ClickEvent = "ClickEvent";

		[Flags]
		private enum EventsKind
		{
			Pointers = 1,
			Manipulation = 2,
			Gestures = 4,
			Click = 8
		}

		public EventsSequences()
		{
			this.InitializeComponent();
			TestListViewTarget.ItemsSource = "0123456789".ToCharArray();
			SetupEvents(TestTapTarget, _tapResult, EventsKind.Pointers | EventsKind.Manipulation | EventsKind.Gestures);
			SetupEvents(TestClickTarget, _clickResult, EventsKind.Pointers | EventsKind.Manipulation | EventsKind.Gestures | EventsKind.Click);
			SetupEvents(TestTranslatedTapTarget, _translatedTapResult, EventsKind.Pointers | EventsKind.Manipulation | EventsKind.Gestures);
			SetupEvents(TestTranslatedClickTarget, _translatedClickResult, EventsKind.Pointers | EventsKind.Manipulation | EventsKind.Gestures | EventsKind.Click);
			SetupEvents(TestHyperlinkTarget, _hyperlinkResult, EventsKind.Pointers | EventsKind.Gestures);
			SetupEvents(TestHyperlinkInner, _hyperlinkResult, EventsKind.Click);
			SetupEvents(TestListViewTarget, _listViewResult, EventsKind.Pointers | EventsKind.Gestures | EventsKind.Click);

			_pointerType.ItemsSource = Enum.GetNames<PointerDeviceType>();

			// Values for automated tests
#if __ANDROID__ || __APPLE_UIKIT__
			_pointerType.SelectedValue = PointerDeviceType.Touch.ToString();
#else
			_pointerType.SelectedValue = PointerDeviceType.Mouse.ToString();
#endif
		}

#if __APPLE_UIKIT__ // On iOS pen is handled exactly as if it was a finger ...
		private bool PenSupportsHover = false;
#else
		private bool PenSupportsHover = true;
#endif

		private PointerDeviceType PointerType => Enum.Parse<PointerDeviceType>(_pointerType.SelectedValue.ToString());

		private void ResetTapTest(object sender, RoutedEventArgs e) => Clear(_tapResult, TestTapResult);
		private void ValidateTapTest(object sender, RoutedEventArgs e)
		{
			var args = new EventSequenceValidator(_tapResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
						&& args.One(PointerPressedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(TappedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result =
						args.One(PointerEnteredEvent)
						&& args.One(PointerPressedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(TappedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			Log(args.Error);
			TestTapResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetClickTest(object sender, RoutedEventArgs e) => Clear(_clickResult, TestClickResult);
		private void ValidateClickTest(object sender, RoutedEventArgs e)
		{
			// Pointer pressed and released are handled by the ButtonBase

			var args = new EventSequenceValidator(_clickResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						&& args.One(TappedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						&& args.One(TappedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			Log(args.Error);
			TestClickResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetTranslatedTapTest(object sender, RoutedEventArgs e) => Clear(_translatedTapResult, TestTranslatedTapResult);
		private void ValidateTranslatedTapTest(object sender, RoutedEventArgs e)
		{
			var args = new EventSequenceValidator(_translatedTapResult);
			var isInertialManip = _translatedTapResult.Any(arg => ReferenceEquals(arg.evt, ManipulationInertiaStartingEvent));
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse when isInertialManip:
				case PointerDeviceType.Pen when isInertialManip && PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerPressedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.One(ManipulationInertiaStartingEvent)
						&& args.MaybeSome(ManipulationDeltaEvent)
						&& args.One(PointerReleasedEvent)
						&& args.MaybeSome(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerPressedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.One(ManipulationCompletedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen when isInertialManip:
				case PointerDeviceType.Touch when isInertialManip:
					result = args.One(PointerEnteredEvent)
						&& args.One(PointerPressedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.One(ManipulationInertiaStartingEvent)
						&& args.Some(ManipulationDeltaEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(PointerExitedEvent)
						&& args.Some(ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result = args.One(PointerEnteredEvent)
						&& args.One(PointerPressedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.One(ManipulationCompletedEvent)
						&& args.One(PointerReleasedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			Log(args.Error);
			TestTranslatedTapResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetTranslatedClickTest(object sender, RoutedEventArgs e) => Clear(_translatedClickResult, TestTranslatedClickResult);
		private void ValidateTranslatedClickTest(object sender, RoutedEventArgs e)
		{
			// Pointer pressed and released are handled by the ButtonBase

			var args = new EventSequenceValidator(_translatedClickResult);
			var isInertialManip = _translatedClickResult.Any(arg => ReferenceEquals(arg.evt, ManipulationInertiaStartingEvent));
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse when isInertialManip:
				case PointerDeviceType.Pen when isInertialManip && PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationInertiaStartingEvent)
						&& args.MaybeSome(ManipulationDeltaEvent)
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result = args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen when isInertialManip:
				case PointerDeviceType.Touch when isInertialManip:
					result = args.One(PointerEnteredEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationInertiaStartingEvent)
						&& args.MaybeSome(ManipulationDeltaEvent)
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						// && args.One(TappedEvent) // No tap as we moved too far
						&& args.One(PointerExitedEvent)
						&& args.Some(ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result = args.One(PointerEnteredEvent)
						&& args.One(ManipulationStartingEvent)
						&& args.Some(PointerMovedEvent)
						&& args.One(ManipulationStartedEvent)
						&& args.Some(PointerMovedEvent, ManipulationDeltaEvent)
						&& args.One(ManipulationCompletedEvent)
						&& args.Click()
						&& args.One(PointerCaptureLostEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;
			}

			Log(args.Error);
			TestTranslatedClickResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetHyperlinkTest(object sender, RoutedEventArgs e) => Clear(_hyperlinkResult, TestHyperlinkResult);
		private void ValidateHyperlinkTest(object sender, RoutedEventArgs e)
		{
			// We subscribed at booth, the TextBlock (Pointers and Gestures) and the Hyperlink (Click)
			// Pointer pressed is handled by the TextBlock, but NOT the released
			// We MUST not receive a Tapped (when clicking on an hyperlink) neither a CaptureLost

			var args = new EventSequenceValidator(_hyperlinkResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generates it and we want to validate it
#if WINAPPSDK
						&& args.One(PointerReleasedEvent)
						&& args.Click()
#else
						&& args.Click()
						&& args.One(PointerReleasedEvent)
#endif
						&& args.MaybeSome(PointerMovedEvent)
						&& args.One(PointerExitedEvent)
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
#if __APPLE_UIKIT__
					// KNOWN ISSUE:
					//	On iOS as the Entered/Exited are generated on Pressed/Released, which are Handled by the Hyperlink,
					//	we do not receive the expected Entered/Exited on parent control.
					//	As a side effect we will also not receive the Tap as it is an interpretation of those missing Pointer events.
					result =
						args.Click()
						&& args.End();
#elif __ANDROID__
					// KNOWN ISSUE: https://github.com/unoplatform/uno/issues/15734
					// On Android the pointer clicked event is not raised properly
					result =
						args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerExitedEvent);
#else
					result =
						args.One(PointerEnteredEvent)
						&& args.MaybeSome(PointerMovedEvent)
#if WINAPPSDK
						&& args.One(PointerReleasedEvent)
						&& args.Click()
#elif __WASM__ // KNOWN ISSUE: We don't get a released if not previously pressed, but pressed are muted by the Hyperlink which is a UIElement on wasm
						&& args.Click()
#else
						&& args.Click()
						&& args.One(PointerExitedEvent)
						&& args.One(PointerReleasedEvent)
#endif
						&& args.One(PointerExitedEvent)
						&& args.End();
#endif
					break;
			}

			Log(args.Error);
			TestHyperlinkResult.Text = result ? "SUCCESS" : "FAILED";
		}

		private void ResetListViewTest(object sender, RoutedEventArgs e) => Clear(_listViewResult, TestListViewResult);
		private void ValidateListViewTest(object sender, RoutedEventArgs e)
		{
			// We subscribed at booth, the TextBlock (Pointers and Gestures) and the Hyperlink (Click)
			// Pointer pressed and released are handled by the TextBlock

			var args = new EventSequenceValidator(_listViewResult);
			var result = false;
			switch (PointerType)
			{
				case PointerDeviceType.Mouse:
				case PointerDeviceType.Pen when PenSupportsHover:
					result =
						args.One(PointerEnteredEvent)
						&& args.Some(PointerMovedEvent) // Could be "Maybe" but WASM UI test generate it and we want to validate it
						&& args.Click()
						&& args.One(TappedEvent)
						&& args.MaybeSome(PointerMovedEvent)
						&& args.MaybeOne(PointerExitedEvent) // This should be "One" (Not maybe) ... but the ListView is a complex control
						&& args.End();
					break;

				case PointerDeviceType.Pen:
				case PointerDeviceType.Touch:
					result =
						args.MaybeOne(PointerEnteredEvent) // This should be "One" (Not maybe) ... but the ListView is a complex control
						&& args.MaybeSome(PointerMovedEvent)
						&& args.Click()
						&& args.MaybeOne(TappedEvent) // This should be "One" (Not maybe) ... but the ListView is a complex control
						&& args.MaybeOne(PointerExitedEvent) // This should be "One" (Not maybe) ... but the ListView is a complex control
						&& args.End();
					break;
			}

			Log(args.Error);
			TestListViewResult.Text = result ? "SUCCESS" : "FAILED";
		}

		#region Common helpers
		private void Clear(IList events, TextBlock result)
		{
			events.Clear();
			result.Text = "** no result **";
			Output.Text = "";
		}
		private void SetupEvents(DependencyObject target, IList<(object, RoutedEventArgs)> events, EventsKind kind, string name = null, bool captureOnPress = false)
		{
			name = name ?? (target as FrameworkElement)?.Name ?? $"{target.GetType().Name}:{target.GetHashCode():X6}";

			if (kind.HasFlag(EventsKind.Pointers) && target is UIElement pointerTarget)
			{
				pointerTarget.PointerEntered += (snd, e) => OnPointerEvent(PointerEnteredEvent, "Entered", e);
				pointerTarget.PointerPressed += (snd, e) =>
				{
					OnPointerEvent(PointerPressedEvent, "Pressed", e);
					if (captureOnPress)
					{
						var captured = pointerTarget.CapturePointer(e.Pointer);
						Log($"[{name}] Captured: {captured}");
					}
				};
				pointerTarget.PointerMoved += (snd, e) => OnPointerEvent(PointerMovedEvent, "Moved", e);
				pointerTarget.PointerReleased += (snd, e) => OnPointerEvent(PointerReleasedEvent, "Released", e);
				pointerTarget.PointerCanceled += (snd, e) => OnPointerEvent(PointerCanceledEvent, "Canceled", e);
				pointerTarget.PointerExited += (snd, e) => OnPointerEvent(PointerExitedEvent, "Exited", e);
				pointerTarget.PointerCaptureLost += (snd, e) => OnPointerEvent(PointerCaptureLostEvent, "CaptureLost", e);
			}

			if (kind.HasFlag(EventsKind.Manipulation) && target is UIElement manipulationTarget)
			{
				manipulationTarget.ManipulationStarting += (snd, e) => OnEvt(ManipulationStartingEvent, "ManipStarting", e, () => e.Mode);
				manipulationTarget.ManipulationStarted += (snd, e) => OnEvt(ManipulationStartedEvent, "ManipStarted", e, () => e.Position, () => e.PointerDeviceType, () => e.Cumulative);
				manipulationTarget.ManipulationDelta += (snd, e) => OnEvt(ManipulationDeltaEvent, "ManipDelta", e, () => e.Position, () => e.PointerDeviceType, () => e.Delta, () => e.Cumulative);
				manipulationTarget.ManipulationInertiaStarting += (snd, e) => OnEvt(ManipulationInertiaStartingEvent, "ManipInertia", e, () => e.PointerDeviceType, () => e.Delta, () => e.Cumulative, () => e.Velocities);
				manipulationTarget.ManipulationCompleted += (snd, e) => OnEvt(ManipulationCompletedEvent, "ManipCompleted", e, () => e.Position, () => e.PointerDeviceType, () => e.Cumulative);
			}

			if (kind.HasFlag(EventsKind.Gestures) && target is UIElement gestureTarget)
			{
				// Those events are built using the GestureRecognizer
				gestureTarget.Tapped += (snd, e) => OnEvent(TappedEvent, "Tapped", e);
				gestureTarget.DoubleTapped += (snd, e) => OnEvent(DoubleTappedEvent, "DoubleTapped", e);
			}

			if (kind.HasFlag(EventsKind.Click))
			{
				if (target is ButtonBase button)
					button.Click += (snd, e) => OnEvent(ClickEvent, "Click", e);

				if (target is Hyperlink hyperlink)
					hyperlink.Click += (snd, e) => OnEvent(ClickEvent, "Click", e);

				if (target is ListViewBase listView)
					listView.ItemClick += (snd, e) => OnEvent(ClickEvent, "Click", e);
			}

			void OnEvent(object evt, string evtName, RoutedEventArgs e, string extra = null)
			{
				events.Add((evt, e));
				Log($"[{name}] {evtName} {extra}");
			}

			void OnPointerEvent(RoutedEvent evt, string evtName, PointerRoutedEventArgs e)
			{
				events.Add((evt, e));

				var point = e.GetCurrentPoint(this);
				Log($"[{name}] {evtName}: id={e.Pointer.PointerId} "
					+ $"| frame={point.FrameId}"
					+ $"| type={e.Pointer.PointerDeviceType} "
					+ $"| position={point.Position} "
#if !WINAPPSDK
					+ $"| rawPosition={point.RawPosition} "
#endif
					+ $"| inContact={point.IsInContact} "
					+ $"| inRange={point.Properties.IsInRange} "
					+ $"| primary={point.Properties.IsPrimary}"
					+ $"| intermediates={e.GetIntermediatePoints(this)?.Count.ToString() ?? "null"} ");
			}

			void OnEvt(object evt, string evtName, RoutedEventArgs e, params Expression<Func<object>>[] values)
			{
				events.Add((evt, e));
				Log($"[{name}] {evtName}: {string.Join("| ", values.Select(v => $"{((v.Body as UnaryExpression)?.Operand as MemberExpression)?.Member.Name ?? "??"}: {v.Compile()()}"))}");
			}
		}

		private void Log(string message)
		{
			System.Diagnostics.Debug.WriteLine(message);
			Output.Text += message + "\r\n";
		}

		private class EventSequenceValidator
		{
			private readonly IList<(object evt, RoutedEventArgs args)> _args;
			private int _index = 0;
			private readonly StringBuilder _error;

			public EventSequenceValidator(IList<(object evt, RoutedEventArgs args)> args)
			{
				_args = args;
				_error = new StringBuilder();
			}

			public string Error => _error.ToString();

			/// <summary>
			/// [1..1]
			/// </summary>
			public bool Click([CallerLineNumber] int line = -1)
				=> One($"at line {line}", ClickEvent);

			/// <summary>
			/// [1..1]
			/// </summary>
			private bool One(string debug, params object[] expected)
			{
				if (_index >= _args.Count)
				{
					_error.AppendLine($"Reach end of events ({_index}) while expecting {string.Join(" or ", expected.AsEnumerable())} {debug}. ");
					return false;
				}

				var actual = _args[_index++].evt;
				if (!expected.Contains(actual))
				{
					_error.AppendLine($"Event {_index} is {actual} while expecting {string.Join(" or ", expected.AsEnumerable())} {debug}. ");
					return false;
				}

				return true;
			}

			/// <summary>
			/// [1..1]
			/// </summary>
			public bool One(RoutedEvent evt, [CallerLineNumber] int line = -1)
				=> One($"at line {line}", evt);

			/// <summary>
			/// [1..1]
			/// </summary>
			public bool One(RoutedEvent evt1, RoutedEvent evt2, [CallerLineNumber] int line = -1)
				=> One($"at line {line}", evt1, evt2);

			/// <summary>
			/// [1..*]
			/// </summary>
			public bool Some(RoutedEvent evt, [CallerLineNumber] int line = -1)
				=> Some($"at line {line}", evt);

			/// <summary>
			/// [1..*]
			/// </summary>
			public bool Some(RoutedEvent evt1, RoutedEvent evt2, [CallerLineNumber] int line = -1)
				=> Some($"at line {line}", evt1, evt2);

			/// <summary>
			/// [1..*]
			/// </summary>
			public bool Some(string debug, params RoutedEvent[] evt)
				=> One(debug, evt) && MaybeSome(evt);

			/// <summary>
			/// [0..1]
			/// </summary>
			public bool MaybeOne(RoutedEvent evt)
			{
				if (_index < _args.Count && ReferenceEquals(_args[_index].evt, evt))
				{
					++_index;
				}
				return true;
			}

			/// <summary>
			/// [0..*]
			/// </summary>
			public bool MaybeSome(params RoutedEvent[] evt)
			{
				while (_index < _args.Count && evt.Contains(_args[_index].evt))
				{
					++_index;
				}
				return true;
			}

			public bool End([CallerLineNumber] int line = -1)
			{
				if (_index < _args.Count)
				{
					_error.AppendLine($"Expected to have reach the end of events at line {line} but {_args.Count - _index} are remaining. ");
					return false;
				}

				return true;
			}
		}
		#endregion
	}
}
