using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls
{
	public partial class SwipeControl
	{
		private const int MovementCount = 3;
		private readonly LinkedList<MoveUpdate> _lastMoves = new LinkedList<MoveUpdate>(); // Used to calculate the drawer's speed when releasing.
		private readonly Stopwatch _grabbedTimer = new Stopwatch(); // Used to calculate the drawer's speed when releasing.

		private TranslateTransform _transform = null;
		private TranslateTransform _swipeStackPaneltransform = null;
		private Vector2 _positionWhenCaptured = Vector2.Zero;
		private Vector2 _desiredPosition = Vector2.Zero;
		private Vector2 _desiredStackPanelPosition = Vector2.Zero;

		private bool _isFarOpen = false;
		private bool _isNearOpen = false;
		private bool _hasLeftContent = false;
		private bool _hasRightContent = false;
		private bool _hasTopContent = false;
		private bool _hasBottomContent = false;

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
			if (m_content.RenderTransform is null || _transform is null)
			{
				m_content.RenderTransform = _transform = new TranslateTransform();
			}

			if (m_swipeContentStackPanel.RenderTransform is null || _swipeStackPaneltransform is null)
			{
				m_swipeContentStackPanel.RenderTransform = _swipeStackPaneltransform = new TranslateTransform();
			}
		}

		private void UnoAttachEventHandlers()
		{
			m_content.ManipulationMode = ManipulationModes.TranslateX /*| ManipulationModes.TranslateInertia*/;
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
			_grabbedTimer.Start();
		}

		private void OnSwipeManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			RecordMovements(e);
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

			void RecordMovements(ManipulationDeltaRoutedEventArgs e)
			{
				// Record the last movements to calculate the speed when releasing.
				_lastMoves.AddLast(new MoveUpdate()
				{
					DeltaX = e.Delta.Translation.X,
					DeltaY = e.Delta.Translation.Y,
					DeltaT = _grabbedTimer.Elapsed.TotalSeconds
				});
				_grabbedTimer.Restart();
				if (_lastMoves.Count > MovementCount)
				{
					// Only keep the last N movements.
					_lastMoves.RemoveFirst();
				}
			}
		}

		private void UpdateDesiredPosition(ManipulationDeltaRoutedEventArgs e)
		{
			var rawDesiredPosition = _positionWhenCaptured + e.Cumulative.Translation.ToVector2();

			var harNearContent = _hasLeftContent || _hasTopContent;
			var hasFarContent = _hasRightContent || _hasBottomContent;
			var min = _isNearOpen || !hasFarContent ? 0 : -10000;
			var max = _isFarOpen || !harNearContent ? 0 : 10000;

			var clampedPosition = Vector2.Max(Vector2.Min(rawDesiredPosition, Vector2.One * max), Vector2.One * min);
			var attenuatedPosition = GetAttenuatedPosition(clampedPosition);

			_desiredPosition = attenuatedPosition;
		}

		private void UpdateStackPanelDesiredPosition()
		{
			// When the items have SwipeMode.Execute, there is a parallax effect between the button and the swipe content.
			var sign = m_createdContent == CreatedContent.Left || m_createdContent == CreatedContent.Top ? -1 : 1;

			if (m_isHorizontal)
			{
				if (m_swipeContentStackPanel.HorizontalAlignment == HorizontalAlignment.Stretch)
				{
					_desiredStackPanelPosition.X = (float)(sign * ActualWidth * 0.5 + _desiredPosition.X * 0.5);
				}
				else
				{
					_desiredStackPanelPosition.X = 0;
				}
			}
			else
			{
				if (m_swipeContentStackPanel.VerticalAlignment == VerticalAlignment.Stretch)
				{
					_desiredStackPanelPosition.Y = (float)(sign * ActualHeight * 0.5 + _desiredPosition.Y * 0.5);
				}
				else
				{
					_desiredStackPanelPosition.Y = 0;
				}
			}
		}

		private async void OnSwipeManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			try
			{
				_grabbedTimer.Stop();

				await SimulateInertia();

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

		private async Task SimulateInertia()
		{
			// TODO: Use the recorded speeds to ajust the _desiredPosition.

			var displacement = m_isHorizontal ? _desiredPosition.X : _desiredPosition.Y;
			var absoluteDisplacement = Math.Abs(displacement);
			var effectiveStackSize = m_isHorizontal ? m_swipeContentStackPanel.ActualWidth : m_swipeContentStackPanel.ActualHeight;
			var unit = m_isHorizontal ? Vector2.UnitX : Vector2.UnitY;

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

			UpdateStackPanelDesiredPosition();

			await AnimateTransforms();

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

			//It is possible that the user has flicked from a negative position to a position that would result in the interaction
			//tracker coming to rest at the positive open position (or vise versa). The != zero check does not account for this.
			//Instead we check to ensure that the current position and the ModifiedRestingPosition have the same sign (multiply to a positive number)
			//If they do not then we are in this situation and want the end result of the interaction to be the closed state, so close without any animation and return
			//to prevent further processing of this inertia state.

			// TODO:
			var positionAfterInertia = _desiredPosition;
			var flickToOppositeSideCheck = _desiredPosition * positionAfterInertia;
			if (m_isHorizontal ? flickToOppositeSideCheck.X < 0 : flickToOppositeSideCheck.X < 0)
			{
				CloseWithoutAnimation();
				return;
			}

			UpdateIsOpen(positionAfterInertia != Vector2.Zero);
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

		private void ConfigurePositionInertiaRestingValues() { }

		private void IdleStateEntered(object @null, object @also_null) { }

		private void UpdateTransforms()
		{
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

		private async Task AnimateTransforms()
		{
			var currentPosition = m_isHorizontal ? _transform.X : _transform.Y;
			var desiredPosition = m_isHorizontal ? _desiredPosition.X : _desiredPosition.Y;
			var distance = Math.Abs(desiredPosition - currentPosition);
			var duration = Math.Min(distance / c_MinimumCloseVelocity, 0.3);

			var storyboard = new Storyboard();
			var animation = new DoubleAnimation()
			{
				To = desiredPosition,
				Duration = new Duration(TimeSpan.FromSeconds(duration)),
				EasingFunction = new QuadraticEase()
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
					EasingFunction = new QuadraticEase()
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

		private struct MoveUpdate
		{
			public double DeltaX { get; set; }

			public double DeltaY { get; set; }

			public double DeltaT { get; set; }
		}
	}

	/// <summary>
	/// Extension methods for classes in the Windows.UI.Xaml.Media.Animation namespace.
	/// </summary>
	internal static class StoryboardExtensions
	{
		/// <summary>
		/// Begins a Storyboard and await for its completion
		/// </summary>
		/// <param name="storyboard">The storyboard</param>
		internal static async Task Run(this Storyboard storyboard)
		{
			var cts = new TaskCompletionSource<bool>();
			void OnCompleted(object sender, object e)
			{
				cts.SetResult(true);
				storyboard.Completed -= OnCompleted;
			}

			storyboard.Completed += OnCompleted;
			storyboard.Begin();
			await cts.Task;
		}
	}
}
