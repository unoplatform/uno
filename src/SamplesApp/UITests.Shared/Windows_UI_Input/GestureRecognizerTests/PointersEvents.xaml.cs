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
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Uno.Extensions;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizer
{
	[SampleControlInfo("Gesture recognizer", "Pointer Events")]
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
		private readonly ManipulationStartingEventHandler _logManipulationStarting;
		private readonly ManipulationStartedEventHandler _logManipulationStarted;
		private readonly ManipulationDeltaEventHandler _logManipulationDelta;
		private readonly ManipulationCompletedEventHandler _logManipulationCompleted;
		private readonly TappedEventHandler _logTapped;
		private readonly DoubleTappedEventHandler _logDoubleTapped;
		
		public PointersEvents()
		{
			_logPointerPressed = new PointerEventHandler((snd, e) =>
			{
				Log(PointerPressedEvent, "Pressed")(snd, e);
				if (_capture.IsOn)
				{
					((UIElement)snd).CapturePointer(e.Pointer);
				}
			});

			_logPointerEntered = new PointerEventHandler(Log(PointerEnteredEvent, "Entered"));
			_logPointerMoved = new PointerEventHandler(Log(PointerMovedEvent, "Moved"));
			_logPointerReleased = new PointerEventHandler(Log(PointerReleasedEvent, "Released"));
			_logPointerExited = new PointerEventHandler(Log(PointerExitedEvent, "Exited"));
			_logPointerCanceled = new PointerEventHandler(Log(PointerCanceledEvent, "Canceled"));
			_logPointerCaptureLost = new PointerEventHandler(Log(PointerCaptureLostEvent, "CaptureLost"));
			_logManipulationStarting = new ManipulationStartingEventHandler(Log(ManipulationStartingEvent, "Manip starting"));
			_logManipulationStarted = new ManipulationStartedEventHandler(Log(ManipulationStartedEvent, "Manip started"));
			_logManipulationDelta = new ManipulationDeltaEventHandler(Log(ManipulationDeltaEvent, "Manip delta"));
			_logManipulationCompleted = new ManipulationCompletedEventHandler(Log(ManipulationCompletedEvent, "Manip completed"));
			_logTapped = new TappedEventHandler(Log(TappedEvent, "Tapped"));
			_logDoubleTapped = new DoubleTappedEventHandler(Log(DoubleTappedEvent, "DoubleTapped"));

			this.InitializeComponent();

			_log.ItemsSource = _eventLog;
			_pointerType.ItemsSource = Enum.GetNames(typeof(PointerDeviceType));
			_pointerType.SelectedValue = PointerDeviceType.Touch.ToString();
			_manipMode.ItemsSource = _manipulationModes.Keys;
			_manipMode.SelectedValue = _manipulationModes.First().Key;
		}

		private Action<object, RoutedEventArgs> Log(RoutedEvent evt, string eventName)
			=> (object sender, RoutedEventArgs args) => _eventLog.Add(new RoutedEventLogEntry(evt, eventName, sender, args, Validate(sender, evt, args)));

		private void ClearLog(object sender, RoutedEventArgs e)
			=> _eventLog.Clear();

		private void OnPointerTypeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!Enum.TryParse<PointerDeviceType>(_pointerType.SelectedValue?.ToString(), out var type))
			{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _pointerType.SelectedValue = PointerDeviceType.Mouse.ToString());
#pragma warning restore CS4014
				return;
			}

			OnConfigChanged(sender, e);
		}

		private void OnManipModeChanged(object sender, SelectionChangedEventArgs e)
			=> OnConfigChanged(sender, e);

		private void OnConfigChanged(object sender, RoutedEventArgs e)
		{
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
				_touchTarget.ManipulationMode = mode;
			}

			var handledToo = _handledEventsToo.IsOn;
			SetupEvents(_touchTargetParent, handledToo);
			SetupEvents(_touchTarget, handledToo);
		}

		private void SetupEvents(FrameworkElement target, bool handledToo)
		{
			target.RemoveHandler(PointerEnteredEvent, _logPointerEntered);
			target.RemoveHandler(PointerPressedEvent, _logPointerPressed);
			target.RemoveHandler(PointerMovedEvent, _logPointerMoved);
			target.RemoveHandler(PointerReleasedEvent, _logPointerReleased);
			target.RemoveHandler(PointerExitedEvent, _logPointerExited);
			target.RemoveHandler(PointerCanceledEvent, _logPointerCanceled);
			target.RemoveHandler(PointerCaptureLostEvent, _logPointerCaptureLost);
			target.RemoveHandler(ManipulationStartingEvent, _logManipulationStarting);
			target.RemoveHandler(ManipulationStartedEvent, _logManipulationStarted);
			target.RemoveHandler(ManipulationDeltaEvent, _logManipulationDelta);
			target.RemoveHandler(ManipulationCompletedEvent, _logManipulationCompleted);
			target.RemoveHandler(TappedEvent, _logTapped);
			target.RemoveHandler(DoubleTappedEvent, _logDoubleTapped);

			target.AddHandler(PointerEnteredEvent, _logPointerEntered, handledToo);
			target.AddHandler(PointerPressedEvent, _logPointerPressed, handledToo);
			target.AddHandler(PointerMovedEvent, _logPointerMoved, handledToo);
			target.AddHandler(PointerReleasedEvent, _logPointerReleased, handledToo);
			target.AddHandler(PointerExitedEvent, _logPointerExited, handledToo);
			target.AddHandler(PointerCanceledEvent, _logPointerCanceled, handledToo);
			target.AddHandler(PointerCaptureLostEvent, _logPointerCaptureLost, handledToo);

			if (_manipStarting.IsOn)
				target.AddHandler(ManipulationStartingEvent, _logManipulationStarting, handledToo);
			if (_manipStarted.IsOn)
				target.AddHandler(ManipulationStartedEvent, _logManipulationStarted, handledToo);
			if (_manipDelta.IsOn)
				target.AddHandler(ManipulationDeltaEvent, _logManipulationDelta, handledToo);
			if (_manipCompleted.IsOn)
				target.AddHandler(ManipulationCompletedEvent, _logManipulationCompleted, handledToo);

			if (_gestureTapped.IsOn)
				target.AddHandler(TappedEvent, _logTapped, handledToo);
			if (_gestureDoubleTapped.IsOn)
				target.AddHandler(DoubleTappedEvent, _logDoubleTapped, handledToo);
		}

		private EventValidity Validate(object snd, RoutedEvent evt, RoutedEventArgs args)
		{
			if (args is PointerRoutedEventArgs pointer)
			{
				var expectedPointerType = (PointerDeviceType)Enum.Parse(typeof(PointerDeviceType), _pointerType.SelectedValue as string);
				if (expectedPointerType != pointer.Pointer.PointerDeviceType)
				{
					return EventValidity.Invalid;
				}

				var expectedPointerId = _pointerId.Text;
				if (expectedPointerId.HasValueTrimmed()
					&& (!uint.TryParse(expectedPointerId, out var pointerId) || pointer.Pointer.PointerId != pointerId))
				{
					return EventValidity.Invalid;
				}

				var properties = pointer.GetCurrentPoint(null).Properties;
				if (!Check(_inRange, pointer.Pointer.IsInRange)
					|| !Check(_inContact, pointer.Pointer.IsInContact))
				{
					return EventValidity.Invalid;
				}

				var validity = EventValidity.Valid;
				if (CheckButton(_leftButton, properties.IsLeftButtonPressed)
					&& CheckButton(_middleButton, properties.IsMiddleButtonPressed)
					&& CheckButton(_rightButton,properties.IsRightButtonPressed)
					&& CheckButton(_barrelButton, properties.IsBarrelButtonPressed)
					&& CheckButton(_eraserButton, properties.IsEraser)
					&& CheckButton(_x1Button, properties.IsXButton1Pressed)
					&& CheckButton(_x2Button, properties.IsXButton2Pressed))
				{	
					return validity;
				}
				else
				{
					return EventValidity.Invalid;
				}

				bool CheckButton(CheckBox expected, bool actual)
				{
					if (!expected.IsChecked.HasValue || expected.IsChecked.Value == actual)
					{
						return true;
					}
					else if (!actual && (evt == PointerReleasedEvent || !Any(PointerPressedEvent) || Any(PointerReleasedEvent)))
					{
						validity = EventValidity.SequenceValid;
						return true;
					}
					else
					{
						validity = EventValidity.Invalid;
						return false;
					}
				}
			}
			else
			{
				return EventValidity.Valid;
			}

			bool Check(CheckBox expected, bool actual)
				=> !expected.IsChecked.HasValue || expected.IsChecked.Value == actual;

			bool Any(RoutedEvent re)
				=> _eventLog.Any(e => e.Sender == snd
					&& e.Event == re
					&& ((PointerRoutedEventArgs)e.Args).Pointer.PointerDeviceType == pointer.Pointer.PointerDeviceType
					&& ((PointerRoutedEventArgs)e.Args).Pointer.PointerId == pointer.Pointer.PointerId);
		}

		[Windows.UI.Xaml.Data.Bindable]
		public class RoutedEventLogEntry
		{
			public RoutedEventLogEntry(RoutedEvent evt, string eventName, object sender, RoutedEventArgs args, EventValidity validity)
			{
				Event = evt;
				Name = eventName;
				Sender = sender;
				Args = args;
				Validity = validity;
			}

			public RoutedEvent Event { get; }

			public string Name { get; }

			public object Sender { get; }

			public RoutedEventArgs Args { get; }

			public EventValidity Validity { get; }

			public string ValidityBullet
				=> Validity == EventValidity.Valid ? "🟩 ok"
					: Validity == EventValidity.SequenceValid ? "🟨 ~ok"
					: "🟥 error";

			public string Details
			{
				get
				{
					switch (Args)
					{
						case PointerRoutedEventArgs ptArgs:
							var point = ptArgs.GetCurrentPoint(Sender as UIElement);
							return $"ptId={ptArgs.Pointer.PointerId} "
								+ $"| frame={point.FrameId}"
								+ $"| type={ptArgs.Pointer.PointerDeviceType} "
								+ $"| position={point.Position} "
								+ $"| rawPosition={point.RawPosition} "
								+ $"| inContact={point.IsInContact} "
								+ $"| props={ToString(point.Properties)} "
								+ $"| intermediates={ptArgs.GetIntermediatePoints(Sender as UIElement)?.Count.ToString() ?? "null"} ";

						case ManipulationStartingRoutedEventArgs startingArgs:
							return $"mode={startingArgs.Mode}";
						case ManipulationStartedRoutedEventArgs startArgs:
							return $"position={startArgs.Position} | {ToString(startArgs.Cumulative, startArgs.Cumulative)}";
						case ManipulationDeltaRoutedEventArgs deltaArgs:
							return $"position={deltaArgs.Position} | {ToString(deltaArgs.Delta, deltaArgs.Cumulative)}";
						case ManipulationCompletedRoutedEventArgs endArgs:
							return $"position={endArgs.Position} | {ToString(new ManipulationDelta {Scale = 1}, endArgs.Cumulative)}";

						case TappedRoutedEventArgs tapped:
							return $"position={tapped.GetPosition(Sender as UIElement)}";
						case DoubleTappedRoutedEventArgs doubleTapped:
							return $"position={doubleTapped.GetPosition(Sender as UIElement)}";

						default:
							return string.Empty;
					}
				}
			}

			/// <inheritdoc />
			public override string ToString()
				=> $"[{(Sender as FrameworkElement)?.Name ?? Sender?.ToString()}] {Name} ({Validity}) - {Details}";

			private static string ToString(ManipulationDelta delta, ManipulationDelta cumulative)
				=>  $"X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00}) "
				+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00}) ";

			private static string ToString(PointerPointProperties props)
			{
				var builder = new StringBuilder();

				// Common
				if (props.IsPrimary) builder.Append("primary ");
				if (props.IsInRange) builder.Append("in_range ");

				if (props.IsLeftButtonPressed) builder.Append("left ");
				if (props.IsMiddleButtonPressed) builder.Append("middle ");
				if (props.IsRightButtonPressed) builder.Append("right ");

				// Mouse
				if (props.IsHorizontalMouseWheel) builder.Append("scroll_Y ");
				if (props.IsXButton1Pressed) builder.Append("alt_butt_1 ");
				if (props.IsXButton2Pressed) builder.Append("alt_butt_2");

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
			Valid,
			SequenceValid,
			Invalid,
		}
	}
}
