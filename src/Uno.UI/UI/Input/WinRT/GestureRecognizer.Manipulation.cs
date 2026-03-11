#nullable enable

using System;
using System.Collections;
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
using CollectionExtensions = Uno.Extensions.CollectionExtensions;

#if IS_UNO_UI_PROJECT
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
			internal static readonly Thresholds StartTouch = new() { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartPen = new() { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartMouse = new() { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			internal static readonly Thresholds DeltaTouch = new() { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaPen = new() { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaMouse = new() { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			// Inertia thresholds are expressed in 'unit / millisecond' (unit being either 'logical px' or 'degree')
			internal static readonly Thresholds InertiaTouch = new() { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };
			internal static readonly Thresholds InertiaPen = new() { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };
			internal static readonly Thresholds InertiaMouse = new() { TranslateX = 15d / 1000, TranslateY = 15d / 1000, Rotate = 5d / 1000, Expansion = 15d / 1000 };

			internal static readonly Thresholds VelocitiesTouch = new() { TranslateX = 3, TranslateY = 3, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds VelocitiesPen = new() { TranslateX = 3, TranslateY = 3, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds VelocitiesMouse = new() { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			private enum ManipulationStatus
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
			private bool _isTranslateXEnabled;
			private bool _isTranslateYEnabled;
			private readonly bool _isRotateEnabled;
			private readonly bool _isScaleEnabled;

			private readonly Thresholds _startThresholds;
			private readonly Thresholds _deltaThresholds;
			private readonly Thresholds _inertiaThresholds;
			private readonly Thresholds _velocitiesThresholds;

			private DispatcherQueueTimer? _dragHoldTimer;

			private ManipulationStatus _status = ManipulationStatus.Starting;
			private Points _origins;
			private Points _currents;
			private (ushort onStart, ushort current) _contacts;
			private ManipulationCommit _head = new(ManipulationDelta.Empty, default, 0);

			private ManipulationVelocities _lastRelevantVelocities;
			private InertiaProcessor? _inertia;

			/// <summary>
			/// Type of the pointers handled by this manipulation.
			/// </summary>
			public PointerDeviceType PointerDeviceType => _deviceType;

			/// <summary>
			/// Indicates that this manipulation **has started** and is for drag-and-drop.
			/// (i.e. raises Drag event instead of Manipulation&lt;Started Delta Completed&gt; events).
			/// </summary>
			public bool IsDragManipulation { get; private set; }

			/// <summary>
			/// Gets the inertia processor if the manipulation is in the inertia state.
			/// </summary>
			public InertiaProcessor? Inertia => _inertia;

			public GestureSettings Settings => _settings;
			public bool IsTranslateXEnabled => _isTranslateXEnabled;
			public bool IsTranslateYEnabled => _isTranslateYEnabled;
			public bool IsRotateEnabled => _isRotateEnabled;
			public bool IsScaleEnabled => _isScaleEnabled;
			public bool IsDraggingEnabled => _isDraggingEnable;

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
				if (manipulation._status == ManipulationStatus.Completed)
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
						_velocitiesThresholds = VelocitiesMouse;
						break;

					case PointerDeviceType.Pen:
						_startThresholds = StartPen;
						_deltaThresholds = DeltaPen;
						_inertiaThresholds = InertiaPen;
						_velocitiesThresholds = VelocitiesPen;
						break;

					default:
					case PointerDeviceType.Touch:
						_startThresholds = StartTouch;
						_deltaThresholds = DeltaTouch;
						_inertiaThresholds = InertiaTouch;
						_velocitiesThresholds = VelocitiesTouch;
						break;
				}

				var args = new ManipulationStartingEventArgs(pointer1.Pointer, _recognizer._gestureSettings);
				_recognizer.ManipulationStarting?.Invoke(_recognizer, args);
				_settings = args.Settings;

				if ((_settings & (GestureSettingsHelper.Manipulations | GestureSettingsHelper.DragAndDrop)) == 0)
				{
					// The manipulation has been cancelled (all possible manip has been removed)
					// WARNING: The _gestureRecognizer._manipulation has not been set yet! We cannot invoke the Complete right now (cf. AddPointer)

					_status = ManipulationStatus.Completed;
					return;
				}

				_isDraggingEnable = (_settings & GestureSettings.Drag) != 0;
				_isTranslateXEnabled = (_settings & GestureSettings.ManipulationTranslateX) != 0;
				_isTranslateYEnabled = (_settings & GestureSettings.ManipulationTranslateY) != 0;
				_isRotateEnabled = (_settings & GestureSettings.ManipulationRotate) != 0;
				_isScaleEnabled = (_settings & GestureSettings.ManipulationScale) != 0;

				_recognizer.ManipulationConfigured?.Invoke(_recognizer, this);
				StartDragTimer();
			}

			/// <summary>
			/// Gets a boolean indicating if the given pointer is still active in the manipulation.
			/// Active means that manipulation is not completed and pointer has been used at least at some point in the manipulation.
			/// </summary>
			/// <param name="pointer">Identifier of the pointer to test.</param>
			public bool IsActive(PointerIdentifier pointer)
				=> _status != ManipulationStatus.Completed
					&& _deviceType == (PointerDeviceType)pointer.Type
					&& _origins.ContainsPointer(pointer.Id);

			private bool TryAdd(PointerPoint point)
			{
				if (point.Pointer == _origins.Pointer1.Pointer)
				{
					this.Log().Error(
						"Invalid manipulation state: We are receiving a down for the second time for the same pointer! "
						+ "This is however common when using iOS emulator with VNC where we might miss some pointer events "
						+ "due to focus being stole by debugger, in that case you can safely ignore this message.");
					return false; // Request to create a new manipulation
				}
				else if (_status >= ManipulationStatus.Inertia)
				{
					// A new manipulation has to be started
					return false;
				}
				else if ((PointerDeviceType)point.PointerDevice.PointerDeviceType != _deviceType
					|| _currents.Pointer2 is not null)
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
				if (_status >= ManipulationStatus.Inertia)
				{
					// We no longer track pointers (even pointer2) once inertia has started
					return;
				}

				var hasUpdate = false;
				foreach (var point in updated)
				{
					if (_deviceType == (PointerDeviceType)point.PointerDevice.PointerDeviceType)
					{
						hasUpdate |= _currents.TryUpdate(point);
					}
				}

				if (hasUpdate)
				{
					NotifyUpdate();
				}
			}

			public void Remove(PointerPoint removed)
			{
				if (_status >= ManipulationStatus.Inertia)
				{
					// We no longer track pointers (even pointer2) once inertia has started
					return;
				}

				if (_deviceType != (PointerDeviceType)removed.PointerDevice.PointerDeviceType)
				{
					return;
				}

				PatchSuspiciousRemovedPointer(ref removed);

				if (_currents.TryUpdate(removed))
				{
					_contacts.current--;

					NotifyUpdate();
				}
			}

			public void Complete()
			{
				StopDragTimer();

				// If the manipulation was not started, we just abort the manipulation without any event
				switch (_status)
				{
					case ManipulationStatus.Started when IsDragManipulation:
						_inertia?.Dispose(); // Safety, inertia should never been started when IsDragManipulation, especially if _state is ManipulationState.Started ^^
						_status = ManipulationStatus.Completed;

						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Completed, _contacts.onStart));
						break;

					case ManipulationStatus.Started:
					case ManipulationStatus.Inertia:
						var isInertial = _status == ManipulationStatus.Inertia;

						_inertia?.Dispose();
						_status = ManipulationStatus.Completed;

						var changes = StageChanges();
						CommitChanges(changes);

						_recognizer.ManipulationCompleted?.Invoke(
							_recognizer,
							new ManipulationCompletedEventArgs(_currents.Identifiers, changes.Position, changes.Cumulative, changes.Velocities, isInertial, _contacts.onStart, _contacts.current));
						break;

					case ManipulationStatus.Starting:
						_inertia?.Dispose();
						_status = ManipulationStatus.Completed;

						_recognizer.ManipulationAborted?.Invoke(_recognizer, this);
						break;

					default: // Safety only
						_inertia?.Dispose();
						_status = ManipulationStatus.Completed;
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

			private void NotifyUpdate()
			{
				var changeSet = StageChanges();
				var pointerAdded = changeSet.ActivePointerCount > changeSet.ParentCommit.PointerCount;
				var pointerRemoved = changeSet.ActivePointerCount < changeSet.ParentCommit.PointerCount;

				switch (_status)
				{
					case ManipulationStatus.Starting when IsBeginningOfDragManipulation():
						// On UWP if the element was configured to allow both Drag and Manipulations,
						// both events are going to be raised (... until the drag "content" is being render and captures all pointers).
						// This results as a manipulation started which is never completed.
						// If user uses double touch the manipulation will however start and complete when user adds / remove the 2nd finger.
						// On Uno, as allowing both Manipulations and drop on the same element is really a stretch case (and is bugish on UWP),
						// we accept as a known limitation that once dragging started no manipulation event would be fired.
						_status = ManipulationStatus.Started;
						_contacts.onStart = _contacts.current;
						IsDragManipulation = true;

						CommitChanges(changeSet);
						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Started, _contacts.onStart));
						break;

					case ManipulationStatus.Starting when changeSet.ActivePointerCount > 1:
						// When a second pointer is added while we are in starting state, we start the manipulation no mater the threshold!
						// Note: We don't rely on the `pointerAdded` here as we didn't commit any changes yet!
						_status = ManipulationStatus.Started;
						_contacts.onStart = _contacts.current;

						CommitChanges(changeSet);
						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_currents.Identifiers, changeSet.Position, changeSet.Cumulative, _contacts.onStart));
						// No needs to publish an update when we start the manipulation due to an additional pointer as cumulative will be empty.
						break;

					case ManipulationStatus.Starting when changeSet.Cumulative.IsSignificant(_startThresholds) || changeSet.Velocities.IsAnyAbove(_velocitiesThresholds):
						_status = ManipulationStatus.Started;
						_contacts.onStart = _contacts.current;

						// Note: We first start with an empty delta, then invoke Update.
						//		 This is required to patch a common issue in applications that are using only the
						//		 ManipulationUpdated.Delta property to track the pointer (like the WCT GridSplitter).
						//		 UWP seems to do that only for Touch and Pen (i.e. the Delta is not empty on start with a mouse),
						//		 but there is no side effect to use the same behavior for all pointer types.
						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_currents.Identifiers, _origins.State.Center, ManipulationDelta.Empty, _contacts.onStart));

						ApplyRailing(ref changeSet);

						CommitChanges(changeSet);
						Debug.Assert(changeSet.Delta == changeSet.Cumulative);
						_recognizer.ManipulationUpdated?.Invoke(
							_recognizer,
							new ManipulationUpdatedEventArgs(this, _currents.Identifiers, changeSet.Position, changeSet.Delta, changeSet.Cumulative, ManipulationVelocities.Empty, isInertial: false, _contacts.onStart, _contacts.current));
						break;

					case ManipulationStatus.Started when pointerRemoved && InertiaProcessor.TryStart(this, ref _inertia, changeSet):
						_status = ManipulationStatus.Inertia;

						var startingArgs = new ManipulationInertiaStartingEventArgs(this, _currents.Identifiers, changeSet.Position, changeSet.Delta, changeSet.Cumulative, changeSet.Velocities, _contacts.onStart);
						CommitChanges(changeSet);
						_recognizer.ManipulationInertiaStarting?.Invoke(_recognizer, startingArgs);

						if (_status is ManipulationStatus.Inertia) // The manipulation might have been completed in the event handler
						{
							_inertia.Start(startingArgs.UseCompositionTimer);
						}
						break;

					case ManipulationStatus.Starting when pointerRemoved || changeSet.ActivePointerCount == 0:
					case ManipulationStatus.Started when pointerRemoved:
					// For now we complete the Manipulation as soon as a pointer was removed.
					// This is not the UWP behavior where for instance you can scale multiple times by releasing only one finger.
					// It's however the right behavior in case of drag conflicting with manipulation (which is not supported by Uno).
					case ManipulationStatus.Inertia when !_inertia!.IsRunning:
						Complete();
						break;

					case ManipulationStatus.Started when IsDragManipulation:
						CommitChanges(changeSet);
						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Continuing, _contacts.onStart));
						break;

					case ManipulationStatus.Started when pointerAdded:
					case ManipulationStatus.Started when changeSet.Delta.IsSignificant(_deltaThresholds):
					case ManipulationStatus.Inertia: // No IsSignificant check for inertia, we prefer smooth animations!
						CommitChanges(changeSet);
						_recognizer.ManipulationUpdated?.Invoke(
							_recognizer,
							new ManipulationUpdatedEventArgs(this, _currents.Identifiers, changeSet.Position, changeSet.Delta, changeSet.Cumulative, changeSet.Velocities, _status == ManipulationStatus.Inertia, _contacts.onStart, _contacts.current));
						break;
				}
			}

			[Pure]
			private ManipulationChangeSet StageChanges(bool useHistory = true)
			{
				var parentCommit = _head;
				var pointerCount = _contacts.current;

				var originPoints = _origins.State;
				var currentPoints = _currents.State;

				ulong timestamp;
				Point position;
				ManipulationDelta cumulative;
				if (_inertia is null)
				{
					timestamp = currentPoints.Timestamp;
					position = currentPoints.Center;
					cumulative = ComputeDelta(originPoints, currentPoints, parentCommit.SumOfDelta);
				}
				else
				{
					(timestamp, position, cumulative) = _inertia.GetStateOnLastTick();
					//position = _inertia.GetPosition();
					//elapsedMicroseconds = _inertia.Elapsed.TotalMicroseconds;
					//cumulative = _inertia.GetCumulative();
				}

				var elapsedMicroseconds = timestamp - parentCommit.Timestamp;
				var delta = ComputeDelta(parentCommit.SumOfDelta, cumulative); // Delta from last commit

				ManipulationVelocities? velocities;
				if (useHistory && _inertia is null && !_currents.StateHistory.IsDefault) // Cannot use history when inertia is running: pointer points are no longer updated!
				{
					var velocitiesPoints = _currents.StateHistory.GetBoundaries(static p => p.Timestamp);
					var velocitiesElapsedMicroseconds = velocitiesPoints.to.Timestamp - velocitiesPoints.from.Timestamp;
					var velocitiesDelta = ComputeDelta(velocitiesPoints.from, velocitiesPoints.to, parentCommit.SumOfDelta);

					velocities = ComputeVelocities(velocitiesDelta, velocitiesElapsedMicroseconds);
				}
				else
				{
					velocities = ComputeVelocities(delta, elapsedMicroseconds);
				}

				// Note: We do not rely on the commited values here. Even if not published, previously computed velocities are still relevant.
				if (velocities is not { } effectiveVelocities || !effectiveVelocities.IsAnyAbove(default))
				{
					effectiveVelocities = _lastRelevantVelocities;
				}
				else
				{
					_lastRelevantVelocities = effectiveVelocities;
				}

				return new(parentCommit, timestamp, pointerCount, position, cumulative, delta, effectiveVelocities);
			}

			// This has to be invoked before any event being raised, it will update the internal state that is used to compute delta and velocities.
			private void CommitChanges(ManipulationChangeSet changeSet)
			{
				_head = new(_head.SumOfDelta.Add(changeSet.Delta), changeSet.Timestamp, changeSet.ActivePointerCount);
			}

			private void ApplyRailing(ref ManipulationChangeSet changeSet)
			{
				Debug.Assert(_status == ManipulationStatus.Started);

				var oldPosition = _origins.State.Center;
				var newPosition = changeSet.Position;

				var slope = newPosition.X == oldPosition.X
					? double.PositiveInfinity
					: (newPosition.Y - oldPosition.Y) / (newPosition.X - oldPosition.X);

				if ((_settings & GestureSettings.ManipulationTranslateRailsX) != 0 && isAngleNearXAxis(slope))
				{
					_isTranslateYEnabled = false;

					changeSet = changeSet with
					{
						Cumulative = changeSet.Cumulative with { Translation = new(changeSet.Cumulative.Translation.X, 0) },
						Delta = changeSet.Delta with { Translation = new(changeSet.Delta.Translation.X, 0) },
						Velocities = changeSet.Velocities with { Linear = new(changeSet.Velocities.Linear.X, 0) }
					};
				}
				else if ((_settings & GestureSettings.ManipulationTranslateRailsY) != 0 && isAngleNearYAxis(slope))
				{
					_isTranslateXEnabled = false;

					changeSet = changeSet with
					{
						Cumulative = changeSet.Cumulative with { Translation = new(0, changeSet.Cumulative.Translation.Y) },
						Delta = changeSet.Delta with { Translation = new(0, changeSet.Delta.Translation.Y) },
						Velocities = changeSet.Velocities with { Linear = new(0, changeSet.Velocities.Linear.Y) }
					};
				}

				// The horizontal rail is 22.5 degrees around the X-axis
				static bool isAngleNearXAxis(double slope)
					=> Math.Abs(slope) <= Math.Tan(22.5 * Math.PI / 180);

				// The vertical rail is 22.5 degrees around the Y-axis
				static bool isAngleNearYAxis(double slope)
					=> Math.Abs(slope) >= Math.Tan(67.5 * Math.PI / 180);
			}

			#region Maths (pure methods)
			[Pure]
			private ManipulationDelta ComputeDelta(in PointsState from, in PointsState to, in ManipulationDelta previous)
			{
				var translateX = _isTranslateXEnabled ? to.Center.X - from.Center.X : 0;
				var translateY = _isTranslateYEnabled ? to.Center.Y - from.Center.Y : 0;

				double rotation;
				float scale, expansion;
				if (to.SourcePointsCount > 1)
				{
					rotation = _isRotateEnabled ? Uno.Extensions.MathEx.ToDegree(to.Angle - from.Angle) : 0;
					scale = _isScaleEnabled ? to.Distance / from.Distance : 1;
					expansion = _isScaleEnabled ? to.Distance - from.Distance : 0;

					// The 'rotation' only contains the current angle compared to the 'origins'.
					// But user might have broke his wrist and made a 360° (2π) rotation, the cumulative must reflect it.
					// Also, the Math.ATan2 method used to compute that angle is not linear when changing to/from second from/to third quadrant,
					// which means that we might have a delta close to 2π while user actually moved only few degrees.
					// (https://docs.microsoft.com/en-us/dotnet/api/system.math.atan2?view=net-5.0)
					// So here, as long as possible, we try:
					//	1. to minimize the angle compared to the last known rotation;
					//	2. append that normalized rotation to that last known value in order to have a linear result which also includes the possible "more than 2π rotation".
					// Note: That correction is fairly important to properly compute the velocities (and then inertia)!
					var previousRotation = previous.Rotation;
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
			private ManipulationDelta ComputeDelta(in ManipulationDelta from, in ManipulationDelta to)
			{
				var translateX = _isTranslateXEnabled ? to.Translation.X - from.Translation.X : 0;
				var translateY = _isTranslateYEnabled ? to.Translation.Y - from.Translation.Y : 0;
				var rotation = _isRotateEnabled ? to.Rotation - from.Rotation : 0;
				var scale = _isScaleEnabled ? to.Scale / from.Scale : 1;
				var expansion = _isScaleEnabled ? to.Expansion - from.Expansion : 0;

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = rotation,
					Scale = scale,
					Expansion = expansion
				};
			}

			[Pure]
			private ManipulationVelocities? ComputeVelocities(ManipulationDelta delta, double elapsedMicroseconds)
			{
				// With uno a single native event might produce multiple managed pointer events.
				// In that case we would get an empty velocities ... which is often not relevant!
				// When we detect that case, we prefer to replay the last known velocities.
				if (delta.IsEmpty || elapsedMicroseconds == 0)
				{
					return null;
				}

				// On iOS (18.3) the last pointer event (release) is usually only 8.3 ms (120 fps) after and 2 px away from last move.
				// This cause velocities to be very low and not relevant, so we prefer to keep the last known velocities.
				if (elapsedMicroseconds < 10_000 && !delta.IsSignificant(_velocitiesThresholds))
				{
					return null;
				}

				var elapsedMs = elapsedMicroseconds / 1000;
				var linearX = delta.Translation.X / elapsedMs;
				var linearY = delta.Translation.Y / elapsedMs;
				var angular = delta.Rotation / elapsedMs;
				var expansion = delta.Expansion / elapsedMs;

				return new ManipulationVelocities
				{
					Linear = new Point(linearX, linearY),
					Angular = (float)angular,
					Expansion = (float)expansion
				};
			}
			#endregion

			private void StartDragTimer()
			{
				if (_isDraggingEnable && _deviceType == PointerDeviceType.Touch)
				{
					_dragHoldTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
					_dragHoldTimer.Interval = TimeSpan.FromMicroseconds(DragWithTouchMinDelayMicroseconds);
					_dragHoldTimer.IsRepeating = false;
					_dragHoldTimer.Tick += TouchDragMightStart;
					_dragHoldTimer.Start();

					void TouchDragMightStart(DispatcherQueueTimer sender, object o)
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
			// For touch it means down -> * moves close to origin for DragWithTouchMinDelayMicroseconds -> * moves far from the origin 
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
				var isOutOfRange = IsOutOfTapRange(down.Position, current.Position);

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

						var isInHoldPhase = current.Timestamp - down.Timestamp < DragWithTouchMinDelayMicroseconds;
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

			public void DisableDragging()
			{
				StopDragTimer();
				if (_status is ManipulationStatus.Starting)
				{
					_isDraggingEnable = false;
				}
				else if (_status is ManipulationStatus.Started && IsDragManipulation)
				{
					Complete();
				}
			}

			#region Patch pointer events
			private void PatchSuspiciousRemovedPointer(ref PointerPoint removed)
			{
				if (OperatingSystem.IsAndroid() && _recognizer.PatchCases.HasFlag(Uno.UI.Input.GestureRecognizerSuspiciousCases.AndroidMotionUpAtInvalidLocation))
				{
					PointerPoint previous;
					if (_currents.Pointer1.Pointer == removed.Pointer)
					{
						previous = _currents.Pointer1;
					}
					else if (_currents.Pointer2?.Pointer == removed.Pointer)
					{
						previous = _currents.Pointer2;
					}
					else
					{
						return;
					}

					var δTμs = removed.Timestamp - previous.Timestamp;
					if (δTμs > 100_000) // 100 ms => Extra high delay to avoid false positive on slow devices, but usually it's about 8ms
					{
						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace($"Cannot detect suspicious pointer event: Δt is to high! (Δμs={δTμs})");
						}

						return;
					}

					var δTms = δTμs / 1000.0;
					var δX = removed.Position.X - previous.Position.X;
					var δY = removed.Position.Y - previous.Position.Y;

					if (DirectionChanged(δX, _lastRelevantVelocities.Linear.X)
						|| DirectionChanged(δY, _lastRelevantVelocities.Linear.Y))
					{
						// Suspicious case detected: Direction is changing for the pointer release event, patch using the last known velocities

						var patchedRawPosition = _lastRelevantVelocities.Apply(previous.RawPosition, δTms);
						var patchedPosition = _lastRelevantVelocities.Apply(previous.Position, δTms);

						if (this.Log().IsEnabled(LogLevel.Trace))
						{
							this.Log().Trace(
								"Suspicious pointer event detected! Direction is changing on x-axis for the release pointer event. "
								+ $"Patching coordinates from {removed.Position.ToDebugString()} to {patchedPosition.ToDebugString()} (Δms={δTms} | v={_lastRelevantVelocities})");
						}

						removed = removed.At(patchedRawPosition, patchedPosition);
						return;
					}

					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"Suspicious pointer event NOT detected! (Δms={δTms} | Δx={δX} | Δy={δY} | v={_lastRelevantVelocities})");
					}

					static bool DirectionChanged(double δ, double velocity)
					{
						var direction = Math.Sign(δ);
						var previousDirection = Math.Sign(velocity);

						return direction is not 0 && previousDirection is not 0 && direction != previousDirection;
					}
				}
			}
			#endregion

			internal readonly record struct Thresholds(
				double TranslateX,
				double TranslateY,
				double Rotate, // Degrees
				double Expansion
			);

			internal record struct ManipulationState(ulong Timestamp, Point Position, ManipulationDelta Cumulative);

			internal record struct ManipulationChangeSet(
				ManipulationCommit ParentCommit,
				ulong Timestamp,
				uint ActivePointerCount, // Number of ACTIVE pointers (unlike the PointsState.PointerCount, this only being updated during inertia)
				Point Position,
				ManipulationDelta Cumulative,
				ManipulationDelta Delta,
				ManipulationVelocities Velocities);

			/// <summary>
			/// Set of values that has been published to the end-user through one of the Manipulation events.
			/// </summary>
			/// <param name="SumOfDelta">The sum of all Delta since the manipulation has started (used to build next Delta to avoid precision issue that would make the Σ(Delta) to diverge from Cumulative).</param>
			/// <param name="Timestamp">The timestamp of points used to create the commit (in microseconds).</param>
			internal readonly record struct ManipulationCommit(
				ManipulationDelta SumOfDelta,
				ulong Timestamp,
				uint PointerCount);

			// WARNING: This struct is ** MUTABLE **
			private struct RollingHistory<T> : IEnumerable<T>
			{
				private const int _maxLength = 10;
				private const int _maxDurationMicroSec = 100_000; // 100 ms

				private readonly T[] _values = new T[_maxLength];
				private int _index = -1;
				private bool _cycled = false;

				public RollingHistory()
				{
				}

				public bool IsDefault => _values is null;

				public void Add(T value)
				{
					_index++;
					if (_index >= _maxLength)
					{
						_cycled = true;
						_index = 0;
					}

					_values[_index] = value;
				}

				public (T from, T to) GetBoundaries(Func<T, ulong> timeStampSelector)
				{
					if (_index is -1)
					{
						return default;
					}

					var last = _values[_index];
					var lastTs = timeStampSelector(last);
					if (_cycled)
					{
						for (var i = _index + 1; i < _maxLength; i++)
						{
							var item = _values[i];
							var itemTs = timeStampSelector(item);
							if (lastTs - itemTs < _maxDurationMicroSec)
							{
								return (item, last);
							}
						}
					}

					for (var i = 0; i < _index; i++)
					{
						var item = _values[i];
						var itemTs = timeStampSelector(item);
						if (lastTs - itemTs < _maxDurationMicroSec)
						{
							return (item, last);
						}
					}

					// Unable to find any items that matches the predicate, just use the last item and it first previous value
					return (_cycled, _index) switch
					{
						(false, 0) => (last, last), // Only one item in the history (suspicious!!), use it for both bounds.
						(true, 0) => (_values[_maxLength - 1], last),
						_ => (_values[_index - 1], last),
					};
				}

				/// <inheritdoc />
				IEnumerator IEnumerable.GetEnumerator()
					=> GetEnumerator();

				/// <inheritdoc />
				public IEnumerator<T> GetEnumerator()
				{
					if (_cycled)
					{
						for (var i = _index + 1; i < _maxLength; i++)
						{
							yield return _values[i];
						}
					}

					for (var i = 0; i < _index; i++)
					{
						yield return _values[i];
					}
				}
			}

			private record struct PointsState(
				ulong Timestamp, // The timestamp of the latest pointer update to either pointer
				Point Center, // This is the center in ** absolute ** coordinates spaces (i.e. relative to the screen)
				float Distance, // The distance between the 2 points, or zero if !HasPointer2
				double Angle, // The angle between the horizontal axis and the line segment formed by the 2 points, or zero if !HasPointer2
				uint SourcePointsCount // The number of pointers that are has been considered for the state, this NEVER goes down has. When a pointer is being removed, the counter remains at 2.
			);

			// WARNING: This struct is ** MUTABLE **
			private struct Points(PointerPoint pointer1)
			{
				public PointerPoint Pointer1 = pointer1;

				public PointerPoint? Pointer2 = default;

				public PointsState State { get; private set; } = new(
					pointer1.Timestamp,
					pointer1.RawPosition, // RawPosition => cf. Note in UpdateComputedValues().
					Distance: 0,
					Angle: 0,
					SourcePointsCount: 1);

				private RollingHistory<PointsState> _stateHistory; // Initialized only for the _currents (through the TryUpdate)
				public RollingHistory<PointsState> StateHistory => _stateHistory;


				public PointerIdentifier[] Identifiers = [pointer1.Pointer];

				public bool ContainsPointer(uint pointerId)
					=> Pointer1.PointerId == pointerId
					|| (Pointer2 is not null && Pointer2.PointerId == pointerId);

				public void SetPointer2(PointerPoint point)
				{
					Pointer2 = point;
					Identifiers = [Pointer1.Pointer, Pointer2.Pointer];
					State = ComputeState(point.Timestamp);
				}

				public bool TryUpdate(PointerPoint point)
				{
					if (_stateHistory.IsDefault)
					{
						_stateHistory = [State];
					}

					if (Pointer1.PointerId == point.PointerId)
					{
						Pointer1 = point;
						State = ComputeState(point.Timestamp);
						_stateHistory.Add(State);

						return true;
					}
					else if (Pointer2 != null && Pointer2.PointerId == point.PointerId)
					{
						Pointer2 = point;
						State = ComputeState(point.Timestamp);
						_stateHistory.Add(State);

						return true;
					}
					else
					{
						return false;
					}
				}

				[Pure]
				private PointsState ComputeState(ulong timestamp)
				{
					// Note: Here we use the RawPosition in order to work in the ** absolute ** screen coordinates system
					//		 This is required to avoid to be impacted the any transform applied on the element,
					//		 and it's sufficient as values of the manipulation events are only values relative to the original touch point.

					if (Pointer2 is null)
					{
						return new(
							timestamp,
							Center: Pointer1.RawPosition,
							Distance: 0,
							Angle: 0,
							SourcePointsCount: 1);
					}
					else
					{
						var p1 = Pointer1.RawPosition;
						var p2 = Pointer2.RawPosition;

						return new(
							timestamp,
							Center: new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2),
							Distance: Vector2.Distance(p1.ToVector2(), p2.ToVector2()),
							Angle: Math.Atan2(p1.Y - p2.Y, p1.X - p2.X),
							SourcePointsCount: 2);
					}
				}

				public static implicit operator Points(PointerPoint pointer1)
					=> new(pointer1);
			}
		}
	}
}
