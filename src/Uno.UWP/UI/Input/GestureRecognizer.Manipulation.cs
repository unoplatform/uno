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
		private class Manipulation
		{
			private enum ManipulationStates
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

			private ManipulationStates _state = ManipulationStates.Starting;
			private Points _origins;
			private Points _lastPublished;
			private Points _currents;

			public Manipulation(GestureRecognizer recognizer, PointerPoint pointer1)
			{
				_recognizer = recognizer;
				_deviceType = pointer1.PointerDevice.PointerDeviceType;

				_origins = _lastPublished = _currents = pointer1;

				var args = new ManipulationStartingEventArgs(_recognizer._gestureSettings);
				_recognizer.ManipulationStarting?.Invoke(_recognizer, args);

				var settings = args.Settings;
				_isTranslateXEnabled = (settings & (GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateRailsX)) != 0;
				_isTranslateYEnabled = (settings & (GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateRailsY)) != 0;
				_isRotateEnabled = (settings & (GestureSettings.ManipulationRotate | GestureSettings.ManipulationRotateInertia)) != 0;
				_isScaleEnabled = (settings & (GestureSettings.ManipulationScale | GestureSettings.ManipulationScaleInertia)) != 0;
			}

			public void Add(PointerPoint point)
			{
				if (_state == ManipulationStates.Completed
					|| point.PointerDevice.PointerDeviceType != _deviceType
					|| _currents.HasPointer2)
				{
					return;
				}

				_origins.SetPointer2(point);
				_lastPublished.SetPointer2(point);
				_currents.SetPointer2(point);

				// We force to start the manipulation (or update it) a second pointer is pressed
				NotifyUpdate(forceUpdate: true);
			}

			public void Update(IList<PointerPoint> updated)
			{
				if (_state == ManipulationStates.Completed)
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
					// not checked the UWP behavior for that!
					Complete();
				}
			}

			public void Complete()
			{
				// If the manipulation was not started, we just abort the manipulation without any event
				switch (_state)
				{
					case ManipulationStates.Started:
						_state = ManipulationStates.Completed;

						_recognizer.ManipulationCompleted?.Invoke(
							_recognizer,
							new ManipulationCompletedEventArgs(_deviceType, _currents.Center, GetDelta(), isInertial: false));
						break;

					default:
						_state = ManipulationStates.Completed;
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

			private void NotifyUpdate(bool forceUpdate = false)
			{
				// Note: Make sure to update the _manipulationLast before raising the event, so if an exception is raised
				//		 or if the manipulation is Completed, the Complete event args can use the updated _manipulationLast.

				switch (_state)
				{
					case ManipulationStates.Starting:
						var cumulative = GetDelta();
						if (forceUpdate || IsSignificant(cumulative))
						{
							_state = ManipulationStates.Started;
							_lastPublished = _currents;

							_recognizer.ManipulationStarted?.Invoke(
								_recognizer,
								new ManipulationStartedEventArgs(_deviceType, _currents.Center, cumulative));
						}

						break;

					case ManipulationStates.Started when _recognizer.ManipulationUpdated == null:
						_lastPublished = _currents;
						break;

					case ManipulationStates.Started:
						// Even if Scale and Angle are expected to be default when we add a pointer (i.e. forceUpdate == true),
						// the 'delta' and 'cumulative' might still contains some TranslateX|Y compared to the previous Pointer1 location.
						var delta = GetDelta(_lastPublished);
						if (forceUpdate || IsSignificant(delta))
						{
							_lastPublished = _currents;

							_recognizer.ManipulationUpdated.Invoke(
								_recognizer,
								new ManipulationUpdatedEventArgs(_deviceType, _currents.Center, delta, GetDelta(), isInertial: false));
						}
						break;
				}
			}

			private ManipulationDelta GetDelta() => GetDelta(_origins);
			private ManipulationDelta GetDelta(Points from)
			{
				var translateX = _isTranslateXEnabled ? _currents.Center.X - from.Center.X : 0;
				var translateY = _isTranslateYEnabled ? _currents.Center.Y - from.Center.Y : 0;

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
					rotate = _isRotateEnabled ? _currents.Angle - from.Angle : 0;
					scale = _isScaleEnabled ? _currents.Distance / from.Distance : 1;
					expansion = _isScaleEnabled ? _currents.Distance - from.Distance : 0;
				}

				return new ManipulationDelta
				{
					Translation = new Point(translateX, translateY),
					Rotation = (float)MathEx.ToDegreeNormalized(rotate),
					Scale = scale,
					Expansion = expansion
				};
			}

			private bool IsSignificant(ManipulationDelta delta)
				=> Math.Abs(delta.Translation.X) >= MinManipulationDeltaTranslateX
				|| Math.Abs(delta.Translation.Y) >= MinManipulationDeltaTranslateY
				|| delta.Rotation >= MinManipulationDeltaRotate // We used the ToDegreeNormalized, no need to check for negative angles
				|| Math.Abs(delta.Expansion) >= MinManipulationDeltaExpansion;

			// WARNING: This struct is ** MUTABLE **
			private struct Points
			{
				private PointerPoint _pointer1;
				private PointerPoint _pointer2;

				public Point Center;
				public float Distance;
				public double Angle;

				public bool HasPointer2 => _pointer2 != null;

				public Points(PointerPoint point)
				{
					_pointer1 = point;
					_pointer2 = default;

					Center = point.Position;
					Distance = 0;
					Angle = 0;
				}

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
					else if (_pointer2.PointerId == point.PointerId)
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
					if (_pointer2 == null)
					{
						Center = _pointer1.Position;
						Distance = 0;
						Angle = 0;
					}
					else
					{
						var p1 = _pointer1.Position;
						var p2 = _pointer2.Position;

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
