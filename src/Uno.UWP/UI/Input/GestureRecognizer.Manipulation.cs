#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.Extensions;

namespace Windows.UI.Input
{
	public partial class GestureRecognizer
	{
		// Note: this is also responsible to handle "Drag manipulations"
		internal class Manipulation
		{
			internal static readonly Thresholds StartTouch = new Thresholds { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartPen = new Thresholds { TranslateX = 15, TranslateY = 15, Rotate = 5, Expansion = 15 };
			internal static readonly Thresholds StartMouse = new Thresholds { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			internal static readonly Thresholds DeltaTouch = new Thresholds { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaPen = new Thresholds { TranslateX = 2, TranslateY = 2, Rotate = .1, Expansion = 1 };
			internal static readonly Thresholds DeltaMouse = new Thresholds { TranslateX = 1, TranslateY = 1, Rotate = .1, Expansion = 1 };

			private enum ManipulationState
			{
				Starting = 1,
				Started = 2,
				// Inertia = 3 // Not supported yet
				Completed = 4,
			}

			private readonly GestureRecognizer _recognizer;
			private readonly PointerDeviceType _deviceType;
			private bool _isDraggingEnable; // Note: This might get disabled if user moves out of range while initial hold delay with finger
			private readonly bool _isTranslateXEnabled;
			private readonly bool _isTranslateYEnabled;
			private readonly bool _isRotateEnabled;
			private readonly bool _isScaleEnabled;

			private readonly Thresholds _startThresholds;
			private readonly Thresholds _deltaThresholds;

			private ManipulationState _state = ManipulationState.Starting;
			private Points _origins;
			private Points _currents;
			private ManipulationDelta _sumOfPublishedDelta = ManipulationDelta.Empty; // Note: not maintained for dragging manipulation

			public bool IsDragManipulation { get; private set; }

			public Manipulation(GestureRecognizer recognizer, PointerPoint pointer1)
			{
				_recognizer = recognizer;
				_deviceType = pointer1.PointerDevice.PointerDeviceType;

				_origins = _currents = pointer1;

				switch (_deviceType)
				{
					case PointerDeviceType.Mouse:
						_startThresholds = StartMouse;
						_deltaThresholds = DeltaMouse;
						break;

					case PointerDeviceType.Pen:
						_startThresholds = StartPen;
						_deltaThresholds = DeltaPen;
						break;

					default:
					case PointerDeviceType.Touch:
						_startThresholds = StartTouch;
						_deltaThresholds = DeltaTouch;
						break;
				}

				var args = new ManipulationStartingEventArgs(_recognizer._gestureSettings);
				_recognizer.ManipulationStarting?.Invoke(_recognizer, args);

				var settings = args.Settings;
				if ((settings & (GestureSettingsHelper.Manipulations | GestureSettingsHelper.DragAndDrop)) == 0)
				{
					_state = ManipulationState.Completed;
				}
				else
				{
					_isDraggingEnable = (settings & GestureSettings.Drag) != 0;
					_isTranslateXEnabled = (settings & (GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateRailsX)) != 0;
					_isTranslateYEnabled = (settings & (GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateRailsY)) != 0;
					_isRotateEnabled = (settings & (GestureSettings.ManipulationRotate | GestureSettings.ManipulationRotateInertia)) != 0;
					_isScaleEnabled = (settings & (GestureSettings.ManipulationScale | GestureSettings.ManipulationScaleInertia)) != 0;
				}
			}

			public bool IsActive(PointerDeviceType type, uint id)
				=> _state != ManipulationState.Completed
					&& _deviceType == type
					&& _origins.ContainsPointer(id);

			public void Add(PointerPoint point)
			{
				if (_state == ManipulationState.Completed
					|| point.PointerDevice.PointerDeviceType != _deviceType
					|| _currents.HasPointer2)
				{
					return;
				}

				_origins.SetPointer2(point);
				_currents.SetPointer2(point);

				// We force to start the manipulation (or update it) as soon as a second pointer is pressed
				NotifyUpdate(pointerAdded: true);
			}

			public void Update(IList<PointerPoint> updated)
			{
				if (_state == ManipulationState.Completed)
				{
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
				if (TryUpdate(removed))
				{
					// For now we complete the Manipulation as soon as a pointer was removed.
					// This is not the UWP behavior where for instance you can scale multiple times by releasing only one finger.
					// It's however the right behavior in case of drag conflicting with manipulation (which is not supported by Uno).
					Complete();
				}
			}

			public void Complete()
			{
				// If the manipulation was not started, we just abort the manipulation without any event
				switch (_state)
				{
					case ManipulationState.Started when IsDragManipulation:
						_state = ManipulationState.Completed;

						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Completed));
						break;

					case ManipulationState.Started:
						_state = ManipulationState.Completed;

						_recognizer.ManipulationCompleted?.Invoke(
							_recognizer,
							new ManipulationCompletedEventArgs(_deviceType, _currents.Center, GetCumulative(), isInertial: false));
						break;

					default:
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

			private bool TryUpdate(PointerPoint point)
			{
				if (_deviceType != point.PointerDevice.PointerDeviceType)
				{
					return false;
				}

				return _currents.TryUpdate(point);
			}

			private void NotifyUpdate(bool pointerAdded = false)
			{
				// Note: Make sure to update the _sumOfPublishedDelta before raising the event, so if an exception is raised
				//		 or if the manipulation is Completed, the Complete event args can use the updated _sumOfPublishedDelta.

				var cumulative = GetCumulative();

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
						IsDragManipulation = true;

						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Started));
						break;
					
					case ManipulationState.Starting when pointerAdded: 
						_state = ManipulationState.Started;
						_sumOfPublishedDelta = cumulative;

						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_deviceType, _currents.Center, cumulative));

						// No needs to publish an update when we start the manipulation due to an additional pointer as cumulative will be empty.

						break;

					case ManipulationState.Starting when cumulative.IsSignificant(_startThresholds):
						_state = ManipulationState.Started;
						_sumOfPublishedDelta = cumulative;

						// Note: We first start with an empty delta, then invoke Update.
						//		 This is required to patch a common issue in applications that are using only the
						//		 ManipulationUpdated.Delta property to track the pointer (like the WCT GridSplitter).
						//		 UWP seems to do that only for Touch and Pen (i.e. the Delta is not empty on start with a mouse),
						//		 but there is no side effect to use the same behavior for all pointer types.
						_recognizer.ManipulationStarted?.Invoke(
							_recognizer,
							new ManipulationStartedEventArgs(_deviceType, _origins.Center, ManipulationDelta.Empty));
						_recognizer.ManipulationUpdated?.Invoke(
							_recognizer,
							new ManipulationUpdatedEventArgs(_deviceType, _currents.Center, cumulative, cumulative, isInertial: false));

						break;

					case ManipulationState.Started when IsDragManipulation:
						_recognizer.Dragging?.Invoke(
							_recognizer,
							new DraggingEventArgs(_currents.Pointer1, DraggingState.Continuing));
						break;

					case ManipulationState.Started:
						// Even if Scale and Angle are expected to be default when we add a pointer (i.e. forceUpdate == true),
						// the 'delta' and 'cumulative' might still contains some TranslateX|Y compared to the previous Pointer1 location.
						var delta = GetDelta(cumulative);

						if (pointerAdded || delta.IsSignificant(_deltaThresholds))
						{
							_sumOfPublishedDelta = _sumOfPublishedDelta.Add(delta);

							_recognizer.ManipulationUpdated?.Invoke(
								_recognizer,
								new ManipulationUpdatedEventArgs(_deviceType, _currents.Center, delta, cumulative, isInertial: false));
						}
						break;
				}
			}

			private ManipulationDelta GetCumulative()
			{
				var translateX = _isTranslateXEnabled ? _currents.Center.X - _origins.Center.X : 0;
				var translateY = _isTranslateYEnabled ? _currents.Center.Y - _origins.Center.Y : 0;
#if __MACOS__
				// correction for translateY being inverted (#4700)
				translateY *= -1;
#endif

				double rotate;
				float scale, expansion;
				if (!_currents.HasPointer2)
				{
					rotate = 0;
					scale = 1;
					expansion = 0;
				}
				else
				{
					rotate = _isRotateEnabled ? _currents.Angle - _origins.Angle : 0;
					scale = _isScaleEnabled ? _currents.Distance / _origins.Distance : 1;
					expansion = _isScaleEnabled ? _currents.Distance - _origins.Distance : 0;
				}

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = (float)MathEx.ToDegreeNormalized(rotate),
					Scale = scale,
					Expansion = expansion
				};
			}

			private ManipulationDelta GetDelta(ManipulationDelta cumulative)
			{
				var deltaSum = _sumOfPublishedDelta;

				var translateX = _isTranslateXEnabled ? cumulative.Translation.X - deltaSum.Translation.X : 0;
				var translateY = _isTranslateYEnabled ? cumulative.Translation.Y - deltaSum.Translation.Y : 0;
				var rotate = _isRotateEnabled ? cumulative.Rotation - deltaSum.Rotation : 0;
				var scale = _isScaleEnabled ? cumulative.Scale / deltaSum.Scale : 1;
				var expansion = _isScaleEnabled ? cumulative.Expansion - deltaSum.Expansion : 0;

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = (float)MathEx.NormalizeDegree(rotate),
					Scale = scale,
					Expansion = expansion
				};
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
							return false;
						}
						else
						{
							// The drag should start only after the hold delay and if the pointer moved out of the range
							return !isInHoldPhase && isOutOfRange;
						}
				}
			}

			internal struct Thresholds
			{
				public int TranslateX;
				public int TranslateY;
				public double Rotate; // Degrees
				public double Expansion;
			}

			// WARNING: This struct is ** MUTABLE **
			private struct Points
			{
				public PointerPoint Pointer1;
				private PointerPoint? _pointer2;

				public Point Center; // This is the center in ** absolute ** coordinates spaces (i.e. relative to the screen)
				public float Distance;
				public double Angle;

				public bool HasPointer2 => _pointer2 != null;

				public Points(PointerPoint point)
				{
					Pointer1 = point;
					_pointer2 = default;

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
					UpdateComputedValues();
				}

				public bool TryUpdate(PointerPoint point)
				{
					if (Pointer1.PointerId == point.PointerId)
					{
						Pointer1 = point;
						UpdateComputedValues();

						return true;
					}
					else if (_pointer2 != null && _pointer2.PointerId == point.PointerId)
					{
						_pointer2 = point;
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
						Distance = Vector2.Distance(p1.ToVector(), p2.ToVector());
						Angle = Math.Atan2(p1.Y - p2.Y, p1.X - p2.X);
					}
				}

				public static implicit operator Points(PointerPoint pointer1)
					=> new Points(pointer1);
			}
		}
	}
}
