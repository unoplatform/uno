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
			private readonly bool _isTranslateXEnabled;
			private readonly bool _isTranslateYEnabled;
			private readonly bool _isRotateEnabled;
			private readonly bool _isScaleEnabled;

			private readonly Thresholds _startThresholds;
			private readonly Thresholds _deltaThresholds;

			private ManipulationState _state = ManipulationState.Starting;
			private Points _origins;
			private Points _currents;
			private ManipulationDelta _sumOfPublishedDelta = ManipulationDelta.Empty;

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
				_isTranslateXEnabled = (settings & (GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateRailsX)) != 0;
				_isTranslateYEnabled = (settings & (GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateRailsY)) != 0;
				_isRotateEnabled = (settings & (GestureSettings.ManipulationRotate | GestureSettings.ManipulationRotateInertia)) != 0;
				_isScaleEnabled = (settings & (GestureSettings.ManipulationScale | GestureSettings.ManipulationScaleInertia)) != 0;

				if ((settings & GestureSettingsHelper.Manipulations) == 0)
				{
					_state = ManipulationState.Completed;
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
					// For now we complete the Manipulation as soon as a pointer was removed ...
					// did not check the UWP behavior for that!
					Complete();
				}
			}

			public void Complete()
			{
				// If the manipulation was not started, we just abort the manipulation without any event
				switch (_state)
				{
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
				private PointerPoint _pointer1;
				private PointerPoint _pointer2;

				public Point Center; // This is the center in ** absolute ** coordinates spaces (i.e. relative to the screen)
				public float Distance;
				public double Angle;

				public bool HasPointer2 => _pointer2 != null;

				public Points(PointerPoint point)
				{
					_pointer1 = point;
					_pointer2 = default;

					Center = point.RawPosition; // RawPosition => cf. Note in UpdateComputedValues().
					Distance = 0;
					Angle = 0;
				}

				public bool ContainsPointer(uint pointerId)
					=> _pointer1.PointerId == pointerId
					|| (HasPointer2 && _pointer2.PointerId == pointerId);

				public void SetPointer2(PointerPoint point)
				{
					_pointer2 = point;
					UpdateComputedValues();
				}

				public bool TryUpdate(PointerPoint point)
				{
					if (_pointer1.PointerId == point.PointerId)
					{
						_pointer1 = point;
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
						Center = _pointer1.RawPosition;
						Distance = 0;
						Angle = 0;
					}
					else
					{
						var p1 = _pointer1.RawPosition;
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
