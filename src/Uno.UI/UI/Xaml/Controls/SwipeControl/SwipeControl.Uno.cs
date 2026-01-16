using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SwipeControl
	{
		private TranslateTransform _transform;
		private TranslateTransform _swipeStackPaneltransform;
		private Vector2 _positionWhenCaptured = Vector2.Zero;
		private Vector2 _desiredPosition = Vector2.Zero;
		private Vector2 _desiredStackPanelPosition = Vector2.Zero;

		private bool _isFarOpen;
		private bool _isNearOpen;
		private bool _hasLeftContent;
		private bool _hasRightContent;
		private bool _hasTopContent;
		private bool _hasBottomContent;
		private bool _isReady; // Template applied

		[Conditional("DEBUG")]
		private static void SWIPECONTROL_TRACE_INFO(SwipeControl that, [CallerLineNumber] int TRACE_MSG_METH = -1, [CallerMemberName] string METH_NAME = null, SwipeControl _ = null)
		{

		}

		[Conditional("DEBUG")]
		private static void SWIPECONTROL_TRACE_VERBOSE(SwipeControl that, [CallerLineNumber] int TRACE_MSG_METH = -1, [CallerMemberName] string METH_NAME = null, SwipeControl _ = null)
		{

		}

		private void InitializeInteractionTracker()
		{
			if (m_content is not null && (m_content.RenderTransform is null || _transform is null))
			{
				m_content.RenderTransform = _transform = new TranslateTransform();
			}

			if (m_swipeContentStackPanel is not null && (m_swipeContentStackPanel.RenderTransform is null || _swipeStackPaneltransform is null))
			{
				m_swipeContentStackPanel.RenderTransform = _swipeStackPaneltransform = new TranslateTransform();
			}
		}

		private void UnoAttachEventHandlers()
		{
			m_content.ManipulationMode = m_isHorizontal ? ManipulationModes.TranslateX : ManipulationModes.TranslateY /*| ManipulationModes.TranslateInertia*/;
			m_content.ManipulationStarting += OnSwipeManipulationStarting;
			m_content.ManipulationStarted += OnSwipeManipulationStarted;
			m_content.ManipulationDelta += OnSwipeManipulationDelta;
			//m_content.ManipulationInertiaStarting += OnSwipeManipulationInertiaStarting;
			m_content.ManipulationCompleted += OnSwipeManipulationCompleted;
		}

		private void UnoDetachEventHandlers()
		{
			if (m_content != null)
			{
				m_content.ManipulationStarting -= OnSwipeManipulationStarting;
				m_content.ManipulationStarted -= OnSwipeManipulationStarted;
				m_content.ManipulationDelta -= OnSwipeManipulationDelta;
				//m_content.ManipulationInertiaStarting -= OnSwipeManipulationInertiaStarting;
				m_content.ManipulationCompleted -= OnSwipeManipulationCompleted;
			}
		}

		private void OnSwipeManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
		}

		private void OnSwipeManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
#if !DEBUG
			// On UWP, SwipeControl works only with touch.
			// We do allow other pointers in DEBUG ... well, because it easier to debug on a PC :)
			if (e.PointerDeviceType != PointerDeviceType.Touch)
			{
				e.Complete();
				return;
			}
#endif

			if (m_isIdle)
			{
				m_isIdle = false;
			}

			m_lastActionWasClosing = false;
			m_lastActionWasOpening = false;
			m_isInteracting = true;

			//Once the user has started interacting with a SwipeControl in the closed state we are free to unblock contents.
			//Contents of items opposite the currently opened ones will not be created.
			if (!m_isOpen)
			{
				m_blockNearContent = false;
				m_blockFarContent = false;
			}

			_positionWhenCaptured = new Vector2((float)_transform.X, (float)_transform.Y);

			// As soon as manipulation starts, we make sure to abort any pending explicit pointer capture,
			// so button nested (or containing) this SwipeControl won't fire their commands.
			// It's not the common behavior for buttons, but this has been observed in SwipeControl on UWP as of 2021-07-27.
			foreach (var pointer in e.Pointers)
			{
				if (PointerCapture.TryGet(pointer, out var capture))
				{
					var targets = capture.GetTargets(PointerCaptureKind.Explicit).ToList();
					global::System.Diagnostics.Debug.Assert(targets.Count <= 1);
					foreach (var target in targets)
					{
						target.Element.ReleasePointerCapture(capture.Pointer);
					}
				}
			}
		}

		private void OnSwipeManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
#if !DEBUG
			// On UWP, SwipeControl works only with touch.
			// We do allow other pointers in DEBUG ... well, because it easier to debug on a PC :)
			if (e.PointerDeviceType != PointerDeviceType.Touch)
			{
				return;
			}
#endif

			UpdateDesiredPosition(e);
			UpdateStackPanelDesiredPosition();

			s_lastInteractedWithSwipeControl.TryGetTarget(out var lastInteractedWithSwipeControl);
			if (m_isInteracting && (lastInteractedWithSwipeControl == null || lastInteractedWithSwipeControl != this))
			{
				if (lastInteractedWithSwipeControl is { })
				{
					lastInteractedWithSwipeControl.CloseIfNotRemainOpenExecuteItem();
				}

				s_lastInteractedWithSwipeControl.SetTarget(this);

				var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
				if (globalTestHooks != null)
				{
					globalTestHooks.NotifyLastInteractedWithSwipeControlChanged();
				}
			}

			float value = 0.0f;

			if (m_isHorizontal)
			{
				value = -_desiredPosition.X;
				if (!m_blockNearContent && m_createdContent != CreatedContent.Left && value < -c_epsilon)
				{
					CreateLeftContent();
				}
				else if (!m_blockFarContent && m_createdContent != CreatedContent.Right && value > c_epsilon)
				{
					CreateRightContent();
				}
			}
			else
			{
				value = -_desiredPosition.Y;
				if (!m_blockNearContent && m_createdContent != CreatedContent.Top && value < -c_epsilon)
				{
					CreateTopContent();
				}
				else if (!m_blockFarContent && m_createdContent != CreatedContent.Bottom && value > c_epsilon)
				{
					CreateBottomContent();
				}
			}

			UpdateThresholdReached(value);

			UpdateTransforms();
		}

		private void UpdateDesiredPosition(ManipulationDeltaRoutedEventArgs e)
		{
			var rawDesiredPosition = _positionWhenCaptured + e.Cumulative.Translation.ToVector2();
			var clampedPosition = GetClampedPosition(rawDesiredPosition);
			var attenuatedPosition = GetAttenuatedPosition(clampedPosition);

			_desiredPosition = attenuatedPosition;
		}

		private Vector2 GetClampedPosition(Vector2 rawDesiredPosition)
		{
			var harNearContent = _hasLeftContent || _hasTopContent;
			var hasFarContent = _hasRightContent || _hasBottomContent;
			var min = _isNearOpen || !hasFarContent ? 0 : -10000;
			var max = _isFarOpen || !harNearContent ? 0 : 10000;

			var clampedPosition = Vector2.Max(Vector2.Min(rawDesiredPosition, Vector2.One * max), Vector2.One * min);
			return clampedPosition;
		}

		private void UpdateStackPanelDesiredPosition()
		{
			// When the items have SwipeMode.Execute, there is a parallax effect between the button and the swipe content.

			switch (m_createdContent)
			{
				case CreatedContent.Left:
					_desiredStackPanelPosition.X = (float)Math.Min(0, _desiredPosition.X - m_swipeContentStackPanel.ActualWidth);
					_desiredStackPanelPosition.Y = 0;
					break;
				case CreatedContent.Right:
					if (m_currentItems.Mode is SwipeMode.Execute)
					{
						_desiredStackPanelPosition.X = (float)(m_content.ActualWidth + _desiredPosition.X);
					}
					else
					{
						_desiredStackPanelPosition.X = 0;
					}
					_desiredStackPanelPosition.Y = 0;
					break;
				case CreatedContent.Top:
					_desiredStackPanelPosition.Y = (float)Math.Min(0, _desiredPosition.Y - m_swipeContentStackPanel.ActualHeight);
					_desiredStackPanelPosition.X = 0;
					break;
				case CreatedContent.Bottom:
					if (m_currentItems.Mode is SwipeMode.Execute)
					{
						_desiredStackPanelPosition.Y = (float)(m_content.ActualHeight + _desiredPosition.Y);
					}
					else
					{
						_desiredStackPanelPosition.Y = 0;
					}
					_desiredStackPanelPosition.X = 0;
					break;
				case CreatedContent.None:
					_desiredStackPanelPosition.X = 0;
					_desiredStackPanelPosition.Y = 0;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async void OnSwipeManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			try
			{
#if !DEBUG
				// On UWP, SwipeControl works only with touch.
				// We do allow other pointers in DEBUG ... well, because it easier to debug on a PC :)
				// Note: The OnSwipeManipulationCompleted is invoked with null args in the Close()
				if (e != null && e.PointerDeviceType != PointerDeviceType.Touch)
				{
					return;
				}
#endif
				await SimulateInertia(e?.Velocities ?? default);

				m_isInteracting = false;
				UpdateIsOpen(_desiredPosition != Vector2.Zero);

				if (m_isOpen)
				{
					if (m_currentItems is { } && m_currentItems.Mode == SwipeMode.Execute && m_currentItems.Count > 0)
					{
						var swipeItem = (SwipeItem)(m_currentItems.GetAt(0));
						swipeItem.InvokeSwipe(this);
					}
				}
				else
				{
					var swipeContentStackPanel = m_swipeContentStackPanel;
					if (swipeContentStackPanel != null)
					{
						swipeContentStackPanel.Background = null;
						var swipeContentStackPanelChildren = swipeContentStackPanel.Children;
						if (swipeContentStackPanelChildren != null)
						{
							swipeContentStackPanelChildren.Clear();
						}
					}
					var swipeContentRoot = m_swipeContentRoot;
					if (swipeContentRoot != null)
					{
						swipeContentRoot.Background = null;
					}

					m_currentItems = null;
					m_createdContent = CreatedContent.None;
				}

				if (!m_isIdle)
				{
					m_isIdle = true;
					var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
					if (globalTestHooks != null)
					{
						globalTestHooks.NotifyIdleStatusChanged(this);
					}
				}
			}
			catch (Exception ex)
			{
				Application.Current.RaiseRecoverableUnhandledException(ex);
			}
		}

		private async Task SimulateInertia(ManipulationVelocities v)
		{
			float speedThreshold = (float)GestureRecognizer.Manipulation.InertiaTouch.TranslateX * 1000; // pixel/s
			const float simulatedInertiaDuration = 0.5f; // in seconds.

			var unit = m_isHorizontal ? Vector2.UnitX : Vector2.UnitY;
			var estimatedSpeed = (float)(m_isHorizontal ? v.Linear.X : v.Linear.Y) * 1000;
			var useAfterInertiaPosition = false;
			var estimatedPositionAfterInertia = _desiredPosition;

			if (Math.Abs(estimatedSpeed) > speedThreshold)
			{
				// If the stackpanel IsMeasureDirty or IsArrangeDirty are true, it means we can't use its ActualSize, so we close the content.
				// This solves a problem when the user swipes the content from one side to the other quickly and the stackpanel doesn't have time to measure and arrange itself before the inertia starts.
				// When that happens, the content can open using the previous stackpanel size, causing an invalid behavior.
				if (m_swipeContentStackPanel.IsMeasureDirty || m_swipeContentStackPanel.IsArrangeDirty)
				{
					await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _ = SimulateInertia(v));
					return;
				}

				estimatedPositionAfterInertia = _desiredPosition + unit * estimatedSpeed * simulatedInertiaDuration; // What would be the position if we let the movement go at the current speed during 'simulatedInertiaDuration'.
				estimatedPositionAfterInertia = GetClampedPosition(estimatedPositionAfterInertia);

				// If inertia would flip sign of the transform, we close instead.
				var flickToOppositeSideCheck = _desiredPosition * estimatedPositionAfterInertia;
				if ((m_isHorizontal ? flickToOppositeSideCheck.X < 0 : flickToOppositeSideCheck.Y < 0))
				{
					CloseWithoutAnimation();
					return;
				}

				_desiredPosition = estimatedPositionAfterInertia;
				useAfterInertiaPosition = true;
			}

			var displacement = m_isHorizontal ? _desiredPosition.X : _desiredPosition.Y;
			var absoluteDisplacement = Math.Abs(displacement);
			var effectiveStackSize = m_isHorizontal ? m_swipeContentStackPanel.ActualWidth : m_swipeContentStackPanel.ActualHeight;

			if (m_isOpen)
			{
				if (absoluteDisplacement < effectiveStackSize)
				{
					_desiredPosition = Vector2.Zero;
				}
				else
				{
					_desiredPosition = unit * (float)effectiveStackSize * Math.Sign(displacement);
				}
			}
			else
			{
				if (m_createdContent != CreatedContent.None && (absoluteDisplacement > effectiveStackSize || absoluteDisplacement > c_ThresholdValue))
				{
					_desiredPosition = unit * (float)effectiveStackSize * Math.Sign(displacement);
				}
				else
				{
					_desiredPosition = Vector2.Zero;
				}
			}

			var displacementFromInertiaVector = _desiredPosition - estimatedPositionAfterInertia;
			var displacementFromInertia = m_isHorizontal ? displacementFromInertiaVector.X : displacementFromInertiaVector.Y;
			if (displacementFromInertia * estimatedSpeed < 0)
			{
				// If the inertia speed and the direction to the final position are opposite, we don't use the inertia speed.
				useAfterInertiaPosition = false;
			}

			UpdateStackPanelDesiredPosition();

			await AnimateTransforms(useAfterInertiaPosition, estimatedSpeed);

			m_isInteracting = false;

			if (m_isIdle)
			{
				m_isIdle = false;
				var globalTestHooks = SwipeTestHooks.GetGlobalTestHooks();
				if (globalTestHooks != null)
				{
					globalTestHooks.NotifyIdleStatusChanged(this);
				}
			}

			UpdateIsOpen(_desiredPosition != Vector2.Zero);
			// If the user has panned the interaction tracker past 0 in the opposite direction of the previously
			// opened swipe items then when we set m_isOpen to true the animations will snap to that value.
			// To avoid this we block that side of the animation until the interacting state is entered.
			if (m_isOpen)
			{
				switch (m_createdContent)
				{
					case CreatedContent.Bottom:
					case CreatedContent.Right:
						m_blockNearContent = true;
						m_blockFarContent = false;
						break;
					case CreatedContent.Top:
					case CreatedContent.Left:
						m_blockNearContent = false;
						m_blockFarContent = true;
						break;
					case CreatedContent.None:
						m_blockNearContent = false;
						m_blockFarContent = false;
						break;
					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}
			}
		}

		private void IdleStateEntered(object @null, object @also_null) { }

		private void UpdateTransforms()
		{
			if (_transform is null || _swipeStackPaneltransform is null)
			{
				return;
			}

			if (m_isHorizontal)
			{
				_transform.X = _desiredPosition.X;
				_swipeStackPaneltransform.X = _desiredStackPanelPosition.X;

				// This is a workaround. We shouldn't have to set the property using Animation precedence.
				_transform.SetValue(TranslateTransform.XProperty, (double)_desiredPosition.X, DependencyPropertyValuePrecedences.Animations);
				_swipeStackPaneltransform.SetValue(TranslateTransform.XProperty, (double)_desiredStackPanelPosition.X, DependencyPropertyValuePrecedences.Animations);
			}
			else
			{
				_transform.Y = _desiredPosition.Y;
				_swipeStackPaneltransform.Y = _desiredStackPanelPosition.Y;

				// This is a workaround. We shouldn't have to set the property using Animation precedence.
				_transform.SetValue(TranslateTransform.YProperty, (double)_desiredPosition.Y, DependencyPropertyValuePrecedences.Animations);
				_swipeStackPaneltransform.SetValue(TranslateTransform.YProperty, (double)_desiredStackPanelPosition.Y, DependencyPropertyValuePrecedences.Animations);
			}
		}

		private async Task AnimateTransforms(bool useInertiaSpeed, double inertiaSpeed)
		{
			var currentPosition = m_isHorizontal ? _transform.X : _transform.Y;
			var desiredPosition = m_isHorizontal ? _desiredPosition.X : _desiredPosition.Y;
			var distance = Math.Abs(desiredPosition - currentPosition);
			var duration = Math.Min(distance / c_MinimumCloseVelocity, 0.3);
			if (useInertiaSpeed)
			{
				duration = distance / inertiaSpeed;
			}

			var storyboard = new Storyboard();
			var animation = new DoubleAnimation()
			{
				To = desiredPosition,
				Duration = new Duration(TimeSpan.FromSeconds(duration)),
				EasingFunction = useInertiaSpeed ? (IEasingFunction)LinearEase.Instance : new QuadraticEase()
				{
					EasingMode = EasingMode.EaseInOut
				}
			};
			Storyboard.SetTarget(animation, _transform);
			Storyboard.SetTargetProperty(animation, m_isHorizontal ? "X" : "Y");
			storyboard.Children.Add(animation);

			var currentStackPosition = m_isHorizontal ? _swipeStackPaneltransform.X : _swipeStackPaneltransform.Y;
			var stackDesiredPosition = m_isHorizontal ? _desiredStackPanelPosition.X : _desiredStackPanelPosition.Y;
			if (currentStackPosition != stackDesiredPosition)
			{
				var stackAnimation = new DoubleAnimation()
				{
					To = stackDesiredPosition,
					Duration = new Duration(TimeSpan.FromSeconds(duration)),
					EasingFunction = useInertiaSpeed ? (IEasingFunction)LinearEase.Instance : new QuadraticEase()
					{
						EasingMode = EasingMode.EaseInOut
					}
				};
				Storyboard.SetTarget(stackAnimation, _swipeStackPaneltransform);
				Storyboard.SetTargetProperty(stackAnimation, m_isHorizontal ? "X" : "Y");
				storyboard.Children.Add(stackAnimation);
			}

			await storyboard.Run();
		}

		/// <summary>
		/// Attenuates the displacement by 50% past the stack panel size.
		/// This creates an elastic effect.
		/// </summary>
		private Vector2 GetAttenuatedPosition(Vector2 desiredPosition)
		{
			var stackPanelSize = m_isHorizontal ? m_swipeContentStackPanel.ActualWidth : m_swipeContentStackPanel.ActualHeight;
			var desiredPos = m_isHorizontal ? desiredPosition.X : desiredPosition.Y;
			var axis = m_isHorizontal ? Vector2.UnitX : Vector2.UnitY;

			var overStackDistance = Math.Abs(desiredPos) - stackPanelSize;
			if (overStackDistance > 0)
			{
				return axis * Math.Sign(desiredPos) * (float)(stackPanelSize + overStackDistance * 0.5);
			}
			else
			{
				return desiredPosition;
			}
		}
	}
}
