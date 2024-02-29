#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Uno;
using Uno.Disposables;
using Uno.Foundation.Logging;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class GestureRecognizer
	{
		// Note: this is also responsible to handle "Drag manipulations"
		internal partial class Manipulation
		{
			internal static readonly Thresholds StartTouch = new Thresholds { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartPen = new Thresholds { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartMouse = new Thresholds { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			internal static readonly Thresholds DeltaTouch = new Thresholds { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaPen = new Thresholds { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaMouse = new Thresholds { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			// Inertia thresholds are expressed in 'unit / millisecond' (unit being either 'logical px' or 'degree')
			internal static readonly Thresholds InertiaTouch = new Thresholds { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };
			internal static readonly Thresholds InertiaPen = new Thresholds { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };
			internal static readonly Thresholds InertiaMouse = new Thresholds { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };

			private enum ManipulationState
			{
				Starting = 1,
				Started = 2,
				Inertia = 3,
				Completed = 4,
			}

			private readonly GestureRecognizer _recognizer;
			private readonly PointerDeviceType _deviceType;
			private readonly GestureSettings _settings;
			private bool _isDraggingEnable; // Note: This might get disabled if user moves out of range while initial hold delay with finger
			private readonly bool _isTranslateXEnabled;
			private readonly bool _isTranslateYEnabled;
			private readonly bool _isRotateEnabled;
			private readonly bool _isScaleEnabled;

			private readonly Thresholds _startThresholds;
			private readonly Thresholds _deltaThresholds;
			private readonly Thresholds _inertiaThresholds;

			private DispatcherQueueTimer? _dragHoldTimer;

			private ManipulationState _state = ManipulationState.Starting;
			private Points _origins;
			private Points _currents;
			private (ushort onStart, ushort current) _contacts;
			private (ManipulationDelta sumOfDelta, ulong timestamp, ushort contacts) _lastPublishedState = (ManipulationDelta.Empty, 0, 1); // Note: not maintained for dragging manipulation
			private ManipulationVelocities _lastRelevantVelocities;
			private InertiaProcessor? _inertia;

			/// <summary>
			/// Indicates that this manipulation **has started** and is for drag-and-drop.
			/// (i.e. raises Drag event instead of Manipulation&lt;Started Delta Completed&gt; events).
			/// </summary>
			public bool IsDragManipulation { get; private set; }

			public GestureSettings Settings => _settings;
			public bool IsTranslateXEnabled => _isTranslateXEnabled;
			public bool IsTranslateYEnabled => _isTranslateYEnabled;
			public bool IsRotateEnabled => _isRotateEnabled;
			public bool IsScaleEnabled => _isScaleEnabled;

			internal static void AddPointer(GestureRecognizer recognizer, PointerPoint pointer)
			{
				var current = recognizer._manipulation;
				if (current != null)
				{
					if (current.TryAdd(pointer))
					{
						// The pending manipulation which can either handle the pointer, either is still active with another pointer,
						// just ignore the new pointer.
						return;
					}

					// The current manipulation should be discarded
					current.Complete(); // Will removed 'current' from the 'recognizer._manipulation' field.
				}

				var manipulation = new Manipulation(recognizer, pointer);
				if (manipulation._state == ManipulationState.Completed)
				{
					// The new manipulation has been cancelled in the ManipStarting handler, throw it away.
					manipulation.Complete(); // Should not do anything, safety only
				}
				else
				{
					recognizer._manipulation = manipulation;
				}
			}

			private Manipulation(GestureRecognizer recognizer, PointerPoint pointer1)
			{
				_recognizer = recognizer;
				_deviceType = (PointerDeviceType)pointer1.PointerDevice.PointerDeviceType;

				_origins = _currents = pointer1;
				_contacts = (0, 1);

				switch (_deviceType)
				{
					case PointerDeviceType.Mouse:
						_startThresholds = StartMouse;
						_deltaThresholds = DeltaMouse;
						_inertiaThresholds = InertiaMouse;
						break;

					case PointerDeviceType.Pen:
						_startThresholds = StartPen;
						_deltaThresholds = DeltaPen;
						_inertiaThresholds = InertiaPen;
						break;

					default:
					case PointerDeviceType.Touch:
						_startThresholds = StartTouch;
						_deltaThresholds = DeltaTouch;
						_inertiaThresholds = InertiaTouch;
						break;
				}

				UpdatePublishedState(ManipulationDelta.Empty);
				var args = new ManipulationStartingEventArgs(pointer1.Pointer, _recognizer._gestureSettings);
				_recognizer.ManipulationStarting?.Invoke(_recognizer, args);
				_settings = args.Settings;

				if ((_settings & (GestureSettingsHelper.Manipulations | GestureSettingsHelper.DragAndDrop)) == 0)
				{
					// The manipulation has been cancelled (all possible manip has been removed)
					// WARNING: The _gestureRecognizer._manipulation has not been set yet! We cannot invoke the Complete right now (cf. AddPointer)

					_state = ManipulationState.Completed;
					return;
				}

				_isDraggingEnable = (_settings & GestureSettings.Drag) != 0;
				_isTranslateXEnabled = (_settings & (GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateRailsX)) != 0;
				_isTranslateYEnabled = (_settings & (GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateRailsY)) != 0;
				_isRotateEnabled = (_settings & GestureSettings.ManipulationRotate) != 0;
				_isScaleEnabled = (_settings & GestureSettings.ManipulationScale) != 0;

				_recognizer.ManipulationConfigured?.Invoke(_recognizer, this);
				StartDragTimer();
			}

			public bool IsActive(PointerIdentifier pointer)
				=> _state != ManipulationState.Completed
					&& _deviceType == (PointerDeviceType)pointer.Type
					&& _origins.ContainsPointer(pointer.Id);

			private bool TryAdd(PointerPoint point)
			{
				if (point.Pointer == _origins.Pointer1.Pointer)
				{
					this.Log().Error(
						"Invalid manipulation state: We are receiving a down for the second time for the same pointer!"
						+ "This is however common when using iOS emulator with VNC where we might miss some pointer events "
						+ "due to focus being stole by debugger, in that case you can safely ignore this message.");
					return false; // Request to create a new manipulation
				}
				else if (_state >= ManipulationState.Inertia)
				{
					// A new manipulation has to be started
					return false;
				}
				else if ((PointerDeviceType)point.PointerDevice.PointerDeviceType != _deviceType
					|| _currents.HasPointer2)
				{
					// A manipulation is already active, but cannot handle this new pointer.
					// We don't support multiple active manipulation on a single element / gesture recognizer,
					// so even if we don't effectively add the given pointer to the active manipulation,
					// we return true to make sure that the current manipulation is not Completed.
					return true;
				}

				_origins.SetPointer2(point);
				_currents.SetPointer2(point);
				_contacts.current++;

				// We force to start the manipulation (or update it) as soon as a second pointer is pressed
				NotifyUpdate();

				return true;
			}

			public void Update(IList<PointerPoint> updated)
			{
				if (_state >= ManipulationState.Inertia)
				{
					// We no longer track pointers (even pointer2) once inertia has started
					return;
				}

				var hasUpdate = false;
				foreach (var point in updated)
				{
					hasUpdate |= TryUpdate(point);
				}

				if (hasUpdate)
				{
					NotifyUpdate();
				}
			}

			public void Remove(PointerPoint removed)
			{
				if (_state >= ManipulationState.Inertia)
				{
					// We no longer track pointers (even pointer2) once inertia has started
					return;
				}

				if (TryUpdate(removed))
				{
					_contacts.current--;

					NotifyUpdate();
				}
			}

			public void Complete()
			{
				StopDragTimer();

				// If the manipulation was not started, we just abort the manipulation without any event
				switch (_state)
				{
					case ManipulationState.Started when IsDragManipulation:
						_inertia?.Dispose(); // Safety, inertia should never been started when IsDragManipulation, especially if _state is ManipulationState.Started ^^
						_state = ManipulationState.Completed;

						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Completed, _contacts.onStart));
						break;

					case ManipulationState.Started:
					case ManipulationState.Inertia:
						_inertia?.Dispose();
						_state = ManipulationState.Completed;

						var position = GetPosition();
						var cumulative = GetCumulative();
						var delta = GetDelta(cumulative);
						var velocities = GetVelocities(delta);

						_recognizer.ManipulationCompleted?.Invoke(
							_recognizer,
							new ManipulationCompletedEventArgs(_currents.Identifiers, position, cumulative, velocities, _state == ManipulationState.Inertia, _contacts.onStart, _contacts.current));
						break;

					case ManipulationState.Starting:
						_inertia?.Dispose();
						_state = ManipulationState.Completed;

						_recognizer.ManipulationAborted?.Invoke(_recognizer, this);
						break;

					default: // Safety only
						_inertia?.Dispose();
						_state = ManipulationState.Completed;
						break;
				}

				// Self scavenge our self from the _recognizer ... yes it's a bit strange,
				// but it's the safest and easiest way to avoid invalid state.
				if (_recognizer._manipulation == this)
				{
					_recognizer._manipulation = null;
				}
			}

#if IS_UNIT_TESTS
			public void RunInertiaSync()
				=> _inertia?.RunSync();
#endif

			private bool TryUpdate(PointerPoint point)
			{
				if (_deviceType != (PointerDeviceType)point.PointerDevice.PointerDeviceType)
				{
					return false;
				}

				return _currents.TryUpdate(point);
			}

			private void NotifyUpdate()
			{
				// Note: Make sure to update the _sumOfPublishedDelta before raising the event, so if an exception is raised
				//		 or if the manipulation is Completed, the Complete event args can use the updated _sumOfPublishedDelta.

				var position = GetPosition();
				var cumulative = GetCumulative();
				var delta = GetDelta(cumulative);
				var velocities = GetVelocities(delta);
				var pointerAdded = _contacts.current > _lastPublishedState.contacts;
				var pointerRemoved = _contacts.current < _lastPublishedState.contacts;

				switch (_state)
				{
					case ManipulationState.Starting when IsBeginningOfDragManipulation():
						// On UWP if the element was configured to allow both Drag and Manipulations,
						// both events are going to be raised (... until the drag "content" is being render an captures all pointers).
						// This results as a manipulation started which is never completed.
						// If user uses double touch the manipulation will however start and complete when user adds / remove the 2nd finger.
						// On Uno, as allowing both Manipulations and drop on the same element is really a stretch case (and is bugish on UWP),
						// we accept as a known limitation that once dragging started no manipulation event would be fired.
						_state = ManipulationState.Started;
						_contacts.onStart = _contacts.current;
						IsDragManipulation = true;

						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Started, _contacts.onStart));
						break;

					case ManipulationState.Starting when pointerAdded:
						_state = ManipulationState.Started;
						_contacts.onStart = _contacts.current;

						UpdatePublishedState(cumulative);
						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_currents.Identifiers, _origins.Center, cumulative, _contacts.onStart));
						// No needs to publish an update when we start the manipulation due to an additional pointer as cumulative will be empty.
						break;

					case ManipulationState.Starting when cumulative.IsSignificant(_startThresholds):
						_state = ManipulationState.Started;
						_contacts.onStart = _contacts.current;

						// Note: We first start with an empty delta, then invoke Update.
						//		 This is required to patch a common issue in applications that are using only the
						//		 ManipulationUpdated.Delta property to track the pointer (like the WCT GridSplitter).
						//		 UWP seems to do that only for Touch and Pen (i.e. the Delta is not empty on start with a mouse),
						//		 but there is no side effect to use the same behavior for all pointer types.

						UpdatePublishedState(cumulative);
						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_currents.Identifiers, _origins.Center, ManipulationDelta.Empty, _contacts.onStart));
						_recognizer.ManipulationUpdated?.Invoke(
							_recognizer,
							new ManipulationUpdatedEventArgs(_currents.Identifiers, position, cumulative, cumulative, ManipulationVelocities.Empty, isInertial: false, _contacts.onStart, _contacts.current));
						break;

					case ManipulationState.Started when pointerRemoved && ShouldStartInertia(velocities):
						_state = ManipulationState.Inertia;
						_inertia = new InertiaProcessor(this, position, cumulative, velocities);

						UpdatePublishedState(delta);
						_recognizer.ManipulationInertiaStarting?.Invoke(
							_recognizer,
							new ManipulationInertiaStartingEventArgs(_currents.Identifiers, position, delta, cumulative, velocities, _contacts.onStart, _inertia));

						_inertia.Start();
						break;

					case ManipulationState.Starting when pointerRemoved:
					case ManipulationState.Started when pointerRemoved:
					// For now we complete the Manipulation as soon as a pointer was removed.
					// This is not the UWP behavior where for instance you can scale multiple times by releasing only one finger.
					// It's however the right behavior in case of drag conflicting with manipulation (which is not supported by Uno).
					case ManipulationState.Inertia when !_inertia!.IsRunning:
						Complete();
						break;

					case ManipulationState.Started when IsDragManipulation:
						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Continuing, _contacts.onStart));
						break;

					case ManipulationState.Started when pointerAdded:
					case ManipulationState.Started when delta.IsSignificant(_deltaThresholds):
					case ManipulationState.Inertia: // No significant check for inertia, we prefer smooth animations!
						UpdatePublishedState(delta);
						_recognizer.ManipulationUpdated?.Invoke(
							_recognizer,
							new ManipulationUpdatedEventArgs(_currents.Identifiers, position, delta, cumulative, velocities, _state == ManipulationState.Inertia, _contacts.onStart, _contacts.current));
						break;
				}
			}

			[Pure]
			private Point GetPosition()
			{
				return _inertia?.GetPosition() ?? _currents.Center;
			}

			[Pure]
			private ManipulationDelta GetCumulative()
			{
				if (_inertia is { } inertia)
				{
					return inertia.GetCumulative();
				}

				var translateX = _isTranslateXEnabled ? _currents.Center.X - _origins.Center.X : 0;
				var translateY = _isTranslateYEnabled ? _currents.Center.Y - _origins.Center.Y : 0;
#if __MACOS__
				// correction for translateY being inverted (#4700)
				translateY *= -1;
#endif

				double rotation;
				float scale, expansion;
				if (_currents.HasPointer2)
				{
					rotation = _isRotateEnabled ? Uno.Extensions.MathEx.ToDegree(_currents.Angle - _origins.Angle) : 0;
					scale = _isScaleEnabled ? _currents.Distance / _origins.Distance : 1;
					expansion = _isScaleEnabled ? _currents.Distance - _origins.Distance : 0;

					// The 'rotation' only contains the current angle compared to the 'origins'.
					// But user might have broke his wrist and made a 360° (2π) rotation, the cumulative must reflect it.
					// Also, the Math.ATan2 method used to compute that angle is not linear when changing to/from second from/to third quadrant,
					// which means that we might have a delta close to 2π while user actually moved only few degrees.
					// (https://docs.microsoft.com/en-us/dotnet/api/system.math.atan2?view=net-5.0)
					// So here, as long as possible, we try:
					//	1. to minimize the angle compared to the last known rotation;
					//	2. append that normalized rotation to that last known value in order to have a linear result which also includes the possible "more than 2π rotation".
					// Note: That correction is fairly important to properly compute the velocities (and then inertia)!
					var previousRotation = _lastPublishedState.sumOfDelta.Rotation;
					var rotationNormalizedDelta = (rotation - previousRotation) % 360; // Note: the '% 2π' is for safety only here
					if (rotationNormalizedDelta > 180) // π
					{
						// The angle gone from quadrant 3 to quadrant 2, i.e. the computed angle gone from -(π + α) to (π + β),
						// which means that the 'rotationDelta' is (2π + α + β) and we have to limit it to (α + β).
						rotationNormalizedDelta -= 360;
					}
					else if (rotationNormalizedDelta < -180) // -π
					{
						// The angle gone from quadrant 2 to quadrant 3, i.e. the computed angle gone from (π + α) to -(π + β),
						// which means that the 'rotationDelta' is (-2π + α + β) ane we have to limit it to (α + β).
						rotationNormalizedDelta += 360;
					}

					rotation = previousRotation + rotationNormalizedDelta;
				}
				else
				{
					rotation = 0;
					scale = 1;
					expansion = 0;
				}

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = (float)rotation,
					Scale = scale,
					Expansion = expansion
				};
			}

			[Pure]
			private ManipulationDelta GetDelta(ManipulationDelta cumulative)
			{
				var deltaSum = _lastPublishedState.sumOfDelta;

				var translateX = _isTranslateXEnabled ? cumulative.Translation.X - deltaSum.Translation.X : 0;
				var translateY = _isTranslateYEnabled ? cumulative.Translation.Y - deltaSum.Translation.Y : 0;
				var rotation = _isRotateEnabled ? cumulative.Rotation - deltaSum.Rotation : 0;
				var scale = _isScaleEnabled ? cumulative.Scale / deltaSum.Scale : 1;
				var expansion = _isScaleEnabled ? cumulative.Expansion - deltaSum.Expansion : 0;

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = rotation,
					Scale = scale,
					Expansion = expansion
				};
			}

			private ManipulationVelocities GetVelocities(ManipulationDelta delta)
			{
				// The _currents.Timestamp is not updated once inertia as started, we must get the elapsed duration from the inertia processor
				// (and not compare it to PointerPoint.Timestamp in any way, cf. remarks on InertiaProcessor.Elapsed)
				var elapsedTicks = _inertia?.Elapsed ?? (double)_currents.Timestamp - _lastPublishedState.timestamp;
				var elapsedMs = elapsedTicks / TimeSpan.TicksPerMillisecond;

				// With uno a single native event might produce multiple managed pointer events.
				// In that case we would get an empty velocities ... which is often not relevant!
				// When we detect that case, we prefer to replay the last known velocities.
				if (delta.IsEmpty || elapsedMs == 0)
				{
					return _lastRelevantVelocities;
				}

				var linearX = delta.Translation.X / elapsedMs;
				var linearY = delta.Translation.Y / elapsedMs;
				var angular = delta.Rotation / elapsedMs;
				var expansion = delta.Expansion / elapsedMs;

				var velocities = new ManipulationVelocities
				{
					Linear = new Point(linearX, linearY),
					Angular = (float)angular,
					Expansion = (float)expansion
				};

				if (velocities.IsAnyAbove(default))
				{
					_lastRelevantVelocities = velocities;
				}

				return _lastRelevantVelocities;
			}

			// This has to be invoked before any event being raised, it will update the internal that is used to compute delta and velocities.
			private void UpdatePublishedState(ManipulationDelta delta)
			{
				_lastPublishedState = (_lastPublishedState.sumOfDelta.Add(delta), _currents.Timestamp, _contacts.current);
			}

			private void StartDragTimer()
			{
				if (_isDraggingEnable && _deviceType == PointerDeviceType.Touch)
				{
					_dragHoldTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
					_dragHoldTimer.Interval = new TimeSpan(DragWithTouchMinDelayTicks);
					_dragHoldTimer.IsRepeating = false;
					_dragHoldTimer.Tick += TouchDragMightStart;
					_dragHoldTimer.Start();

					void TouchDragMightStart(DispatcherQueueTimer sender, object? o)
					{
						sender.Tick -= TouchDragMightStart;
						sender.Stop();

						if (_isDraggingEnable) // Sanity only, the timer should have been stopped by the IsBeginningOfDragManipulation()
						{
							_recognizer.DragReady?.Invoke(_recognizer, this);
						}
					}
				}
			}

			private void StopDragTimer()
			{
				_dragHoldTimer?.Stop();
			}

			// For pen and mouse this only means down -> * moves out of tap range;
			// For touch it means down -> * moves close to origin for DragUsingFingerMinDelayTicks -> * moves far from the origin 
			private bool IsBeginningOfDragManipulation()
			{
				if (!_isDraggingEnable)
				{
					return false;
				}

				// Note: We use the TapRange and not the manipulation's start threshold as, for mouse and pen,
				//		 those thresholds are lower than a Tap (and actually only 1px), which does not math the UWP behavior.
				var down = _origins.Pointer1;
				var current = _currents.Pointer1;
				var isOutOfRange = Gesture.IsOutOfTapRange(down.Position, current.Position);

				switch (_deviceType)
				{
					case PointerDeviceType.Mouse:
					case PointerDeviceType.Pen:
						return isOutOfRange;

					default:
					case PointerDeviceType.Touch:
						// As for holding, here we rely on the fact that we get a lot of small moves due to the lack of precision
						// of the touch device (cf. Gesture.NeedsHoldingTimer).
						// This means that this method is expected to be invoked on each move (until manipulation starts)
						// in order to update the _isDraggingEnable state.

						var isInHoldPhase = current.Timestamp - down.Timestamp < DragWithTouchMinDelayTicks;
						if (isInHoldPhase && isOutOfRange)
						{
							// The pointer moved out of range while in the hold phase, so we completely disable the drag manipulation
							_isDraggingEnable = false;
							StopDragTimer();
							return false;
						}
						else
						{
							// The drag should start only after the hold delay and if the pointer moved out of the range
							return !isInHoldPhase && isOutOfRange;
						}
				}
			}

			[Pure]
			private bool ShouldStartInertia(ManipulationVelocities velocities)
				=> _inertia is null
					&& !IsDragManipulation
					&& (_settings & GestureSettingsHelper.Inertia) != 0
					&& velocities.IsAnyAbove(_inertiaThresholds);

			internal struct Thresholds
			{
				public double TranslateX;
				public double TranslateY;
				public double Rotate; // Degrees
				public double Expansion;
			}

			// WARNING: This struct is ** MUTABLE **
			private struct Points
			{
				public PointerPoint Pointer1;
				private PointerPoint? _pointer2;

				public ulong Timestamp;
				public Point Center; // This is the center in ** absolute ** coordinates spaces (i.e. relative to the screen)
				public float Distance;
				public double Angle;

				public bool HasPointer2 => _pointer2 != null;

				public PointerIdentifier[] Identifiers;

				public Points(PointerPoint point)
				{
					Pointer1 = point;
					_pointer2 = default;

					Identifiers = new[] { point.Pointer };
					Timestamp = point.Timestamp;
					Center = point.RawPosition; // RawPosition => cf. Note in UpdateComputedValues().
					Distance = 0;
					Angle = 0;
				}

				public bool ContainsPointer(uint pointerId)
					=> Pointer1.PointerId == pointerId
					|| (HasPointer2 && _pointer2!.PointerId == pointerId);

				public void SetPointer2(PointerPoint point)
				{
					_pointer2 = point;
					Identifiers = new[] { Pointer1.Pointer, _pointer2.Pointer };
					UpdateComputedValues();
				}

				public bool TryUpdate(PointerPoint point)
				{
					if (Pointer1.PointerId == point.PointerId)
					{
						Pointer1 = point;
						Timestamp = point.Timestamp;

						UpdateComputedValues();

						return true;
					}
					else if (_pointer2 != null && _pointer2.PointerId == point.PointerId)
					{
						_pointer2 = point;
						Timestamp = point.Timestamp;

						UpdateComputedValues();

						return true;
					}
					else
					{
						return false;
					}
				}

				private void UpdateComputedValues()
				{
					// Note: Here we use the RawPosition in order to work in the ** absolute ** screen coordinates system
					//		 This is required to avoid to be impacted the any transform applied on the element,
					//		 and it's sufficient as values of the manipulation events are only values relative to the original touch point.

					if (_pointer2 == null)
					{
						Center = Pointer1.RawPosition;
						Distance = 0;
						Angle = 0;
					}
					else
					{
						var p1 = Pointer1.RawPosition;
						var p2 = _pointer2.RawPosition;

						Center = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
						Distance = Vector2.Distance(p1.ToVector2(), p2.ToVector2());
						Angle = Math.Atan2(p1.Y - p2.Y, p1.X - p2.X);
					}
				}

				public static implicit operator Points(PointerPoint pointer1)
					=> new Points(pointer1);
			}
		}
	}
}
