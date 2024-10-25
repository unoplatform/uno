using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Shared.Windows_UI_Input.GestureRecognizer
{
	[SampleControlInfo("Gesture Recognizer", "Pointer Events test bench")]
	public sealed partial class PointersEvents : Page
	{
		private static readonly IDictionary<string, ManipulationModes> _manipulationModes = new Dictionary<string, ManipulationModes>
		{
			{"System", ManipulationModes.System},
			{"None", ManipulationModes.None},
			{"Translate", ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY | ManipulationModes.TranslateInertia},
			{"Translate X", ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateInertia},
			{"Translate Y", ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY | ManipulationModes.TranslateInertia},
			{"Rotate", ManipulationModes.Rotate | ManipulationModes.RotateInertia},
			{"Scale", ManipulationModes.Scale | ManipulationModes.ScaleInertia},
		};

		private readonly ObservableCollection<RoutedEventLogEntry> _eventLog = new ObservableCollection<RoutedEventLogEntry>();

		private readonly PointerEventHandler _logPointerEntered;
		private readonly PointerEventHandler _logPointerPressed;
		private readonly PointerEventHandler _logPointerMoved;
		private readonly PointerEventHandler _logPointerReleased;
		private readonly PointerEventHandler _logPointerExited;
		private readonly PointerEventHandler _logPointerCanceled;
		private readonly PointerEventHandler _logPointerCaptureLost;
		private readonly PointerEventHandler _logPointerWheel;
		private readonly ManipulationStartingEventHandler _logManipulationStarting;
		private readonly ManipulationStartedEventHandler _logManipulationStarted;
		private readonly ManipulationDeltaEventHandler _logManipulationDelta;
		private readonly ManipulationInertiaStartingEventHandler _logManipulationInertia;
		private readonly ManipulationCompletedEventHandler _logManipulationCompleted;
		private readonly TappedEventHandler _logTapped;
		private readonly DoubleTappedEventHandler _logDoubleTapped;
		private readonly RightTappedEventHandler _logRightTapped;

		private bool _isReady;

		public PointersEvents()
		{
			_logPointerPressed = new PointerEventHandler((snd, e) =>
			{
				CreateHandler(PointerPressedEvent, "Pressed", _ptPressedHandle)(snd, e);
				if (_ptPressedCapture.IsChecked ?? false)
				{
					((UIElement)snd).CapturePointer(e.Pointer);
				}
			});

			this.InitializeComponent();

			_logPointerEntered = new PointerEventHandler(CreateHandler(PointerEnteredEvent, "Entered", _ptEnteredHandle));
			_logPointerMoved = new PointerEventHandler(CreateHandler(PointerMovedEvent, "Moved", _ptMovedHandle));
			_logPointerReleased = new PointerEventHandler(CreateHandler(PointerReleasedEvent, "Released", _ptReleasedHandle));
			_logPointerExited = new PointerEventHandler(CreateHandler(PointerExitedEvent, "Exited", _ptExitedHandle));
			_logPointerCanceled = new PointerEventHandler(CreateHandler(PointerCanceledEvent, "Canceled", _ptCanceledHandle));
			_logPointerCaptureLost = new PointerEventHandler(CreateHandler(PointerCaptureLostEvent, "CaptureLost", _ptCaptureLostHandle));
			_logPointerWheel = new PointerEventHandler(CreateHandler(PointerWheelChangedEvent, "Wheel", _ptWheelHandle));
			_logManipulationStarting = new ManipulationStartingEventHandler(CreateHandler(ManipulationStartingEvent, "Manip starting", _manipStartingHandle));
			_logManipulationStarted = new ManipulationStartedEventHandler(CreateHandler(ManipulationStartedEvent, "Manip started", _manipStartedHandle));
			_logManipulationDelta = new ManipulationDeltaEventHandler(CreateHandler(ManipulationDeltaEvent, "Manip delta", _manipDeltaHandle));
			_logManipulationInertia = new ManipulationInertiaStartingEventHandler(CreateHandler(ManipulationInertiaStartingEvent, "Manip inertia", _manipInertiaHandle));
			_logManipulationCompleted = new ManipulationCompletedEventHandler(CreateHandler(ManipulationCompletedEvent, "Manip completed", _manipCompletedHandle));
			_logTapped = new TappedEventHandler(CreateHandler(TappedEvent, "Tapped", _gestureTappedHandle));
			_logDoubleTapped = new DoubleTappedEventHandler(CreateHandler(DoubleTappedEvent, "DoubleTapped", _gestureDoubleTappedHandle));
			_logRightTapped = new RightTappedEventHandler(CreateHandler(RightTappedEvent, "RightTapped", _gestureRightTappedHandle));

			_logs.ItemsSource = _eventLog;
			_pointerType.ItemsSource = Enum.GetNames(typeof(PointerDeviceType));
			_pointerType.SelectedValue = PointerDeviceType.Touch.ToString();
			_manipMode.ItemsSource = _manipulationModes.Keys;
			_manipMode.SelectedValue = _manipulationModes.First().Key;

			_isReady = true;
			OnConfigChanged(null, null);
		}

		private Action<object, RoutedEventArgs> CreateHandler(RoutedEvent evt, string eventName, CheckBox handleEvent = null)
			=> (object sender, RoutedEventArgs args) =>
			{
				_eventLog.Add(new RoutedEventLogEntry(evt, eventName, sender, args, Validate(sender, evt, args)));
				if (ReferenceEquals(sender, TouchTarget) && (handleEvent?.IsChecked ?? false))
				{
					args.GetType().GetProperty("Handled")?.SetValue(args, true);
				}
			};

		private void ClearLog(object sender, RoutedEventArgs e)
			=> _eventLog.Clear();

		private void OnPointerTypeChanged(object sender, SelectionChangedEventArgs e)
			=> OnConfigChanged(sender, e);

		private void OnManipModeChanged(object sender, SelectionChangedEventArgs e)
			=> OnConfigChanged(sender, e);

		private void OnConfigChanged(object sender, RoutedEventArgs e)
		{
			if (!_isReady)
			{
				return;
			}

			if (_horizontalScroll.IsOn)
			{
				_myScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
				_myScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
			}
			else
			{
				_myScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
				_myScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
			}
			if (_verticalScroll.IsOn)
			{
				_myScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
				_myScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
			}
			else
			{
				_myScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
				_myScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
			}

			if (_manipMode.SelectedValue is string modeKey
				&& _manipulationModes.TryGetValue(modeKey, out var mode))
			{
				TouchTarget.ManipulationMode = mode;
				TouchTargetParent.ManipulationMode = mode;
			}

			var handledToo = _handledEventsToo.IsOn;
			SetupEvents(TouchTargetParent, handledToo, _allEventsOnParent.IsOn);
			SetupEvents(TouchTarget, handledToo);
		}

		private void SetupEvents(FrameworkElement target, bool handledToo, bool allEvents = false)
		{
			target.RemoveHandler(PointerEnteredEvent, _logPointerEntered);
			target.RemoveHandler(PointerPressedEvent, _logPointerPressed);
			target.RemoveHandler(PointerMovedEvent, _logPointerMoved);
			target.RemoveHandler(PointerReleasedEvent, _logPointerReleased);
			target.RemoveHandler(PointerExitedEvent, _logPointerExited);
			target.RemoveHandler(PointerCanceledEvent, _logPointerCanceled);
			target.RemoveHandler(PointerCaptureLostEvent, _logPointerCaptureLost);
			target.RemoveHandler(PointerWheelChangedEvent, _logPointerWheel);
			target.RemoveHandler(ManipulationStartingEvent, _logManipulationStarting);
			target.RemoveHandler(ManipulationStartedEvent, _logManipulationStarted);
			target.RemoveHandler(ManipulationDeltaEvent, _logManipulationDelta);
			target.RemoveHandler(ManipulationInertiaStartingEvent, _logManipulationInertia);
			target.RemoveHandler(ManipulationCompletedEvent, _logManipulationCompleted);
			target.RemoveHandler(TappedEvent, _logTapped);
			target.RemoveHandler(DoubleTappedEvent, _logDoubleTapped);
			target.RemoveHandler(RightTappedEvent, _logRightTapped);

			if (allEvents || _ptEntered.IsOn)
				target.AddHandler(PointerEnteredEvent, _logPointerEntered, handledToo);
			if (allEvents || _ptPressed.IsOn)
				target.AddHandler(PointerPressedEvent, _logPointerPressed, handledToo);
			if (allEvents || _ptMoved.IsOn)
				target.AddHandler(PointerMovedEvent, _logPointerMoved, handledToo);
			if (allEvents || _ptReleased.IsOn)
				target.AddHandler(PointerReleasedEvent, _logPointerReleased, handledToo);
			if (allEvents || _ptExited.IsOn)
				target.AddHandler(PointerExitedEvent, _logPointerExited, handledToo);
			if (allEvents || _ptCanceled.IsOn)
				target.AddHandler(PointerCanceledEvent, _logPointerCanceled, handledToo);
			if (allEvents || _ptCaptureLost.IsOn)
				target.AddHandler(PointerCaptureLostEvent, _logPointerCaptureLost, handledToo);
			if (allEvents || _ptWheel.IsOn)
				target.AddHandler(PointerWheelChangedEvent, _logPointerWheel, handledToo);

			if (allEvents || _manipStarting.IsOn)
				target.AddHandler(ManipulationStartingEvent, _logManipulationStarting, handledToo);
			if (allEvents || _manipStarted.IsOn)
				target.AddHandler(ManipulationStartedEvent, _logManipulationStarted, handledToo);
			if (allEvents || _manipDelta.IsOn)
				target.AddHandler(ManipulationDeltaEvent, _logManipulationDelta, handledToo);
			if (allEvents || _manipInertia.IsOn)
				target.AddHandler(ManipulationInertiaStartingEvent, _logManipulationInertia, handledToo);
			if (allEvents || _manipCompleted.IsOn)
				target.AddHandler(ManipulationCompletedEvent, _logManipulationCompleted, handledToo);

			if (allEvents || _gestureTapped.IsOn)
				target.AddHandler(TappedEvent, _logTapped, handledToo);
			if (allEvents || _gestureDoubleTapped.IsOn)
				target.AddHandler(DoubleTappedEvent, _logDoubleTapped, handledToo);
			if (allEvents || _gestureRightTapped.IsOn)
				target.AddHandler(RightTappedEvent, _logRightTapped, handledToo);
		}

		private (EventValidity, string error) Validate(object snd, RoutedEvent evt, RoutedEventArgs args)
		{
			var validity = EventValidity.Valid;
			var error = "";
			if (args is PointerRoutedEventArgs pointer)
			{
				var expectedPointerType = (PointerDeviceType)Enum.Parse(typeof(PointerDeviceType), _pointerType.SelectedValue as string);
				if (expectedPointerType != (PointerDeviceType)pointer.Pointer.PointerDeviceType)
				{
					error += "pt_type ";
					validity = EventValidity.Invalid;
				}

				var expectedPointerId = _pointerId.Text;
				if (!expectedPointerId.IsNullOrWhiteSpace()
					&& (!uint.TryParse(expectedPointerId, out var pointerId) || pointer.Pointer.PointerId != pointerId))
				{
					error += "pt_id ";
					validity = EventValidity.Invalid;
				}

				var properties = pointer.GetCurrentPoint(null).Properties;
				Check(_inRange, pointer.Pointer.IsInRange, "in_range");
				Check(_inContact, pointer.Pointer.IsInContact, "in_contact");

				CheckButton(_leftButton, properties.IsLeftButtonPressed, "left");
				CheckButton(_middleButton, properties.IsMiddleButtonPressed, "middle");
				CheckButton(_rightButton, properties.IsRightButtonPressed, "right");
				CheckButton(_barrelButton, properties.IsBarrelButtonPressed, "barrel");
				CheckButton(_eraserButton, properties.IsEraser, "eraser");
				CheckButton(_x1Button, properties.IsXButton1Pressed, "x1");
				CheckButton(_x2Button, properties.IsXButton2Pressed, "x2");
			}

			return (validity, error);

			void Check(CheckBox expected, bool actual, string errorMessage)
			{
				if (expected.IsChecked.HasValue && expected.IsChecked.Value != actual)
				{
					error += errorMessage + " ";
					validity = EventValidity.Invalid;
				}
			}

			void CheckButton(CheckBox expected, bool actual, string errorMessage)
			{
				if (!expected.IsChecked.HasValue || expected.IsChecked.Value == actual)
				{
				}
				else if (!actual && (evt == PointerReleasedEvent || !Any(PointerPressedEvent) || Any(PointerReleasedEvent)))
				{
					error += errorMessage + " ";
					validity |= EventValidity.SequenceValid;
				}
				else
				{
					error += errorMessage + " ";
					validity = EventValidity.Invalid;
				}
			}

			bool Any(RoutedEvent re)
				=> _eventLog.Any(e => e.Sender == snd
					&& e.Event == re
					&& ((PointerRoutedEventArgs)e.Args).Pointer.PointerDeviceType == pointer.Pointer.PointerDeviceType
					&& ((PointerRoutedEventArgs)e.Args).Pointer.PointerId == pointer.Pointer.PointerId);
		}

		[Windows.UI.Xaml.Data.Bindable]
		public class RoutedEventLogEntry
		{
			public RoutedEventLogEntry(RoutedEvent evt, string eventName, object sender, RoutedEventArgs args, (EventValidity result, string errors) validity)
			{
				Event = evt;
				Name = eventName;
				Sender = sender;
				Args = args;
				Validity = validity.result;
				Errors = validity.errors;
				Details = Format();
			}

			public RoutedEvent Event { get; }

			public string Name { get; }

			public object Sender { get; }

			public RoutedEventArgs Args { get; }

			public EventValidity Validity { get; }

			public string Errors { get; }

			public string ValidityBullet
				=> Validity == EventValidity.Valid ? "🟩 ok"
					: Validity == EventValidity.SequenceValid ? $"🟨 ~ok ({Errors})"
					: $"🟥 error ({Errors})";

			public string Details { get; }

			/// <inheritdoc />
			public override string ToString()
				=> $"[{(Sender as FrameworkElement)?.Name ?? Sender?.ToString()}] {Name} ({Validity}) - {Details}";

			private string Format()
			{
				switch (Args)
				{
					case PointerRoutedEventArgs ptArgs:
						var point = ptArgs.GetCurrentPoint(Sender as UIElement);
						return $"{Src(ptArgs)} "
							+ $"| hd={ptArgs.Handled} "
							+ $"| ptId={ptArgs.Pointer.PointerId} "
							+ $"| frame={point.FrameId}"
							+ $"| type={ptArgs.Pointer.PointerDeviceType} "
							+ $"| position={F(point.Position)} "
#if !WINAPPSDK
							+ $"| rawPosition={F(point.RawPosition)} "
#endif
							+ $"| inContact={point.IsInContact} "
							+ $"| props={F(point.Properties)} "
							+ $"| intermediates={ptArgs.GetIntermediatePoints(Sender as UIElement)?.Count.ToString() ?? "null"} ";

					case ManipulationStartingRoutedEventArgs startingArgs:
						return $"{Src(startingArgs)} | hd={startingArgs.Handled} | mode={startingArgs.Mode}";
					case ManipulationStartedRoutedEventArgs startArgs:
						return $"{Src(startArgs)} | hd={startArgs.Handled} | position={F(startArgs.Position)} | {F(startArgs.Cumulative, startArgs.Cumulative)}";
					case ManipulationDeltaRoutedEventArgs deltaArgs:
						return $"{Src(deltaArgs)} | hd={deltaArgs.Handled} | position={F(deltaArgs.Position)} | {F(deltaArgs.Delta, deltaArgs.Cumulative)}";
					case ManipulationCompletedRoutedEventArgs endArgs:
						return $"{Src(endArgs)} | hd={endArgs.Handled} | position={F(endArgs.Position)} | {F(new ManipulationDelta { Scale = 1 }, endArgs.Cumulative)}";

					case TappedRoutedEventArgs tapped:
						return $"{Src(tapped)} | hd={tapped.Handled} | position={F(tapped.GetPosition(Sender as UIElement))}";
					case DoubleTappedRoutedEventArgs doubleTapped:
						return $"{Src(doubleTapped)} | hd={doubleTapped.Handled} | position={F(doubleTapped.GetPosition(Sender as UIElement))}";
					case RightTappedRoutedEventArgs rightTapped:
						return $"{Src(rightTapped)} | hd={rightTapped.Handled} | position={F(rightTapped.GetPosition(Sender as UIElement))}";

					default:
						return string.Empty;
				}
			}

			private static string Src(RoutedEventArgs args)
				=> $"src={(args.OriginalSource as FrameworkElement)?.Name ?? args.OriginalSource.GetType().Name} ";

			private static string F(ManipulationDelta delta, ManipulationDelta cumulative)
				=> $"X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00}) "
				+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00}) ";

			private static string F(global::Windows.Foundation.Point pt)
				=> $"[{pt.X:F2}, {pt.Y:F2}]";

			private static string F(PointerPointProperties props)
			{
				var builder = new StringBuilder();

				// Common
				if (props.IsPrimary) builder.Append("primary ");
				if (props.IsInRange) builder.Append("in_range ");

				if (props.IsLeftButtonPressed) builder.Append("left ");
				if (props.IsMiddleButtonPressed) builder.Append("middle ");
				if (props.IsRightButtonPressed) builder.Append("right ");

				// Mouse
				if (props.IsXButton1Pressed) builder.Append("alt_butt_1 ");
				if (props.IsXButton2Pressed) builder.Append("alt_butt_2");
				if (props.MouseWheelDelta != 0)
				{
					builder.Append("scroll");
					builder.Append(props.IsHorizontalMouseWheel ? "X (" : "Y (");
					builder.Append(props.MouseWheelDelta);
					builder.Append("px) ");
				}

				// Pen
				if (props.IsBarrelButtonPressed) builder.Append("barrel ");
				if (props.IsEraser) builder.Append("eraser ");

				// Misc
				builder.Append('(');
				builder.Append(props.PointerUpdateKind);
				builder.Append(')');

				return builder.ToString();
			}
		}

		public enum EventValidity
		{
			Valid = 0,
			SequenceValid = 1,
			Invalid = 255,
		}
	}
}
