// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedIcon.cpp, commit 1b9db23

using System;
using System.Globalization;
using System.Numerics;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIcon : IconElement
	{
		private const string s_progressPropertyName = "Progress";
		private const string s_foregroundPropertyName = "Foreground";
		private const string s_transitionInfix = "To";
		private const string s_transitionStartSuffix = "_Start";
		private const string s_transitionEndSuffix = "_End";

		public AnimatedIcon()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_AnimatedIcon);

#if !HAS_UNO
			m_progressPropertySet = Microsoft.UI.Xaml.Window.Current.Compositor.CreatePropertySet();
			m_progressPropertySet.InsertScalar(s_progressPropertyName, 0);
#else
			m_progressPropertySet = new CompositionPropertySet(null);
#endif
			Loaded += OnLoaded;
			Unloaded += OnIconUnloaded;

			this.RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
			this.RegisterPropertyChangedCallback(FlowDirectionProperty, OnFlowDirectionPropertyChanged);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Construct the visual from the Source property in on apply template so that it participates
			// in the initial measure for the object.
			ConstructAndInsertVisual();
			var panel = VisualTreeHelper.GetChild(this, 0) as Panel;
			m_rootPanel = panel;
			m_currentState = GetState(this);

			if (panel != null)
			{
				// Animated icon implements IconElement through PathIcon. We don't need the Path that
				// PathIcon creates, however when you set the foreground on AnimatedIcon, it assumes
				// its grid's first child has a fill property which it sets by known index. So we
				// keep this child around but collapse it so this behavior doesn't crash us when the
				// fallback is used.
				if (panel.Children.Count > 0)
				{
					if (panel.Children[0] is { } path)
					{
						path.Visibility = Visibility.Collapsed;
					}
				}
				if (m_animatedVisual is { } visual)
				{
					ElementCompositionPreview.SetElementChildVisual(panel, visual.RootVisual);
				}

				TrySetForegroundProperty();
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
#if HAS_UNO
			// Uno specific: Called to ensure OnApplyTemplate runs and Foreground is subscribed
			EnsureInitialized();
#endif

			// AnimatedIcon might get added to a UI which has already set the State property on an ancestor.
			// If this is the case and the animated icon being added doesn't have its own state property
			// We copy the ancestor value when we load. Additionally we attach to our ancestor's property
			// changed event for AnimatedIcon.State to copy the value to AnimatedIcon.
			var property = StateProperty;

			(DependencyObject, string) GetAncestorWithState()
			{
				var parent = VisualTreeHelper.GetParent(this);
				while (parent != null)
				{
					var stateValue = parent.GetValue(property);
					if (!string.IsNullOrEmpty((string)stateValue))
					{
						return (parent, (string)stateValue);
					}
					parent = VisualTreeHelper.GetParent(parent);
				}
				return ((DependencyObject)(null), string.Empty);
			}
			var (ancestorWithState, stateValue) = GetAncestorWithState();

			if (string.IsNullOrEmpty((string)GetValue(property)))
			{
				SetValue(property, stateValue);
			}

			if (ancestorWithState != null)
			{
				m_ancestorStatePropertyChangedRevoker.Disposable = null;
				var token = ancestorWithState.RegisterPropertyChangedCallback(property, OnAncestorAnimatedIconStatePropertyChanged);
				m_ancestorStatePropertyChangedRevoker.Disposable = Disposable.Create(() =>
				{
					ancestorWithState.UnregisterPropertyChangedCallback(property, token);
				});
			}

			// Wait until loaded to apply the fallback icon source property because we need the icon source
			// properties to be set before we create the icon element from it.  If those poperties are bound in,
			// they will not have been set during OnApplyTemplate.
			OnFallbackIconSourcePropertyChanged(null);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (m_animatedVisual is { } visual)
			{
				// Animated Icon scales using the Uniform strategy, meaning that it scales the horizonal and vertical
				// dimensions equally by the maximum amount that doesn't exceed the available size in either dimension.
				// If the available size is infinite in both dimensions then we don't scale the visual. Otherwise, we
				// calculate the scale factor by comparing the default visual size to the available size. This produces 2
				// scale factors, one for each dimension. We choose the smaller of the scale factors to not exceed the
				// available size in that dimension.
				var visualSize = visual.Size;
				if (visualSize != Vector2.Zero)
				{
					var widthScale = availableSize.Width == double.PositiveInfinity ? float.PositiveInfinity : availableSize.Width / visualSize.X;
					var heightScale = availableSize.Height == double.PositiveInfinity ? float.PositiveInfinity : availableSize.Height / visualSize.Y;
					if (widthScale == double.PositiveInfinity && heightScale == double.PositiveInfinity)
					{
						return new Size(visualSize.X, visualSize.Y);
					}
					else if (widthScale == double.PositiveInfinity)
					{
						return new Size(visualSize.X * heightScale, availableSize.Height);
					}
					else if (heightScale == double.PositiveInfinity)
					{
						return new Size(availableSize.Width, visualSize.Y * widthScale);
					}
					else
					{
						return (heightScale > widthScale) ?
							new Size(availableSize.Width, visualSize.Y * widthScale) :
							new Size(visualSize.X * heightScale, availableSize.Height);
					}
				}
				return new Size(visualSize.X, visualSize.Y);
			}
			// If we don't have a visual, we will show the fallback icon, so we need to do a traditional measure.
			else
			{
				return base.MeasureOverride(availableSize);
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (m_animatedVisual is { } visual)
			{
				var visualSize = visual.Size;

				Vector2 GetScale(Size finalSize, Vector2 visualSize)
				{
					var scaleX = finalSize.Width / visualSize.X;
					var scaleY = finalSize.Height / visualSize.Y;
					if (scaleX < scaleY)
					{
						scaleY = scaleX;
					}
					else
					{
						scaleX = scaleY;
					}
					return new Vector2((float)scaleX, (float)scaleY);
				}
				var scale = GetScale(finalSize, visualSize);

				Vector2 arrangedSize = new Vector2(
					(float)Math.Min(finalSize.Width / scale.X, visualSize.X),
					(float)Math.Min(finalSize.Height / scale.Y, visualSize.Y));

				var offset = new Vector2(
					(float)(finalSize.Width - (visualSize * scale).X) / 2,
					(float)(finalSize.Height - (visualSize * scale).Y) / 2);
				var rootVisual = visual.RootVisual;
				rootVisual.Offset = new Vector3(offset, 0.0f);
				rootVisual.Size = arrangedSize;
				rootVisual.Scale = new Vector3(scale, 1.0f);
				return finalSize;
			}

			else
			{
				return base.ArrangeOverride(finalSize);
			}
		}

		private static void OnAnimatedIconStatePropertyChanged(
			 DependencyObject sender,
			 DependencyPropertyChangedEventArgs args)
		{
			if (sender is AnimatedIcon senderAsAnimatedIcon)
			{
				senderAsAnimatedIcon.OnStatePropertyChanged();
			}
		}

		private void OnAncestorAnimatedIconStatePropertyChanged(
			 DependencyObject sender,
			 DependencyProperty args)
		{
			SetValue(StateProperty, sender.GetValue(args));
		}

		// When we receive a state change it might be erroneous. This is because these state changes often come from Animated Icon's parent control's
		// Visual state manager.  Many of our controls assume that it is okay to call GoToState("Foo") followed immediately by GoToState("Bar") in
		// order to end up in the "Bar" state. However Animated Icon also cares what state you are coming from, so this pattern would not trigger
		// the NormalToBar transition but instead NormalToFoo followed by FooToBar. Since we can't change these controls logic in WinUI2 we handle
		// this by waiting until the next layout cycle to play an animated icon transition. However, the state dependency property changed is not
		// enough to ensure that a layout updated will trigger, so we must also invalidate a layout property, arrange was chosen arbitrarily.
		private void OnStatePropertyChanged()
		{
			m_pendingState = (string)(this.GetValue(StateProperty));
			m_layoutUpdatedRevoker.Disposable = null;
			LayoutUpdated += OnLayoutUpdatedAfterStateChanged;
			m_layoutUpdatedRevoker.Disposable = Disposable.Create(() =>
			{
				LayoutUpdated -= OnLayoutUpdatedAfterStateChanged;
			});

			SharedHelpers.QueueCallbackForCompositionRendering(() => InvalidateArrange());
		}

		private void OnLayoutUpdatedAfterStateChanged(object sender, object args)
		{
			m_layoutUpdatedRevoker.Disposable = null;
			switch (m_queueBehavior)
			{
				case AnimatedIconAnimationQueueBehavior.Cut:
					TransitionAndUpdateStates(m_currentState, m_pendingState);
					break;
				case AnimatedIconAnimationQueueBehavior.QueueOne:
					if (m_isPlaying)
					{
						/// If we already have too many queued states, cancel the current animation with the previously queued transition
						// then Queue this new transition.
						if (m_queuedStates.Count >= m_queueLength)
						{
							TransitionAndUpdateStates(m_currentState, m_queuedStates.Peek());
						}
						m_queuedStates.Enqueue(m_pendingState);
					}
					else
					{
						TransitionAndUpdateStates(m_currentState, m_pendingState);
					}
					break;
				case AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne:
					if (m_isPlaying)
					{
						// If we already have too many queued states, cancel the current animation with the previously queued transition
						//  played speed up then Queue this new transition.
						if (m_queuedStates.Count >= m_queueLength)
						{
							// Cancel the previous animation completed handler, before we cancel that animation by starting a new one.
							if (m_batch != null)
							{
								m_batchCompletedRevoker.Disposable = null;
							}
							TransitionAndUpdateStates(m_currentState, m_queuedStates.Peek(), m_speedUpMultiplier);
							m_queuedStates.Enqueue(m_pendingState);
						}
						else
						{
							m_queuedStates.Enqueue(m_pendingState);
							if (!m_isSpeedUp)
							{
								// Cancel the previous animation completed handler, before we cancel that animation by starting a new one.
								if (m_batch != null)
								{
									m_batchCompletedRevoker.Disposable = null;
								}

								m_isSpeedUp = true;

								var markers = Source.Markers;
								string transitionEndName = StringUtil.FormatString("%1!s!%2!s!%3!s!%4!s!", m_previousState, s_transitionInfix, m_currentState, s_transitionEndSuffix);
								var hasEndMarker = markers.ContainsKey(transitionEndName);
								if (hasEndMarker)
								{
									PlaySegment(float.NaN, (float)markers[transitionEndName], null, m_speedUpMultiplier);
								}
							}
						}
					}
					else
					{
						TransitionAndUpdateStates(m_currentState, m_pendingState);
					}
					break;
			}
			m_pendingState = "";
		}

		private void TransitionAndUpdateStates(string fromState, string toState, float playbackMultiplier = 1.0f)
		{
			// TODO Uno specific - adjust for multithreaded access according to MUX source code.
			bool cleanedUpFlag = false;
			Action cleanupAction = () =>
			{
				if (!cleanedUpFlag)
				{
					m_previousState = fromState;
					m_currentState = toState;
					if (m_queuedStates.Count > 0)
					{
						m_queuedStates.Dequeue();
					}
				}
			};
			TransitionStates(fromState, toState, cleanupAction, playbackMultiplier);
			cleanupAction();
		}

		private void TransitionStates(string fromState, string toState, Action cleanupAction, float playbackMultiplier = 1.0f)
		{
			if (Source is { } source)
			{
				if (source.Markers is { } markers)
				{
					string transitionName = StringUtil.FormatString("%1!s!%2!s!%3!s!", fromState, s_transitionInfix, toState);
					string transitionStartName = StringUtil.FormatString("%1!s!%2!s!", transitionName, s_transitionStartSuffix);
					string transitionEndName = StringUtil.FormatString("%1!s!%2!s!", transitionName, s_transitionEndSuffix);

					var hasStartMarker = markers.ContainsKey(transitionStartName);
					var hasEndMarker = markers.ContainsKey(transitionEndName);
					if (hasStartMarker && hasEndMarker)
					{
						var fromProgress = (float)(markers[transitionStartName]);
						var toProgress = (float)(markers[transitionEndName]);
						PlaySegment(fromProgress, toProgress, cleanupAction, playbackMultiplier);
						m_lastAnimationSegmentStart = transitionStartName;
						m_lastAnimationSegmentEnd = transitionEndName;
					}
					else if (hasEndMarker)
					{
						var toProgress = (float)(markers[transitionEndName]);
						m_progressPropertySet.InsertScalar(s_progressPropertyName, toProgress);
						m_lastAnimationSegmentStart = "";
						m_lastAnimationSegmentEnd = transitionEndName;
					}
					else if (hasStartMarker)
					{
						var toProgress = (float)(markers[transitionStartName]);
						m_progressPropertySet.InsertScalar(s_progressPropertyName, toProgress);
						m_lastAnimationSegmentStart = transitionStartName;
						m_lastAnimationSegmentEnd = "";
					}
					else if (markers.ContainsKey(transitionName))
					{
						var toProgress = (float)(markers[transitionName]);
						m_progressPropertySet.InsertScalar(s_progressPropertyName, toProgress);
						m_lastAnimationSegmentStart = "";
						m_lastAnimationSegmentEnd = transitionName;
					}
					else if (markers.ContainsKey(toState))
					{
						var toProgress = (float)(markers[toState]);
						m_progressPropertySet.InsertScalar(s_progressPropertyName, toProgress);
						m_lastAnimationSegmentStart = "";
						m_lastAnimationSegmentEnd = toState;
					}
					else
					{
						// Since we can't find an animation for this transition, try to find one that ends in the same place
						// and cut to that position instead.
						(bool found, float value) FindValue()
						{
							string fragment = StringUtil.FormatString("%1!s!%2!s!%3!s!", s_transitionInfix, toState, s_transitionEndSuffix);
							foreach (var marker in markers)
							{
								string value = marker.Key;
								if (value.IndexOf(fragment, StringComparison.Ordinal) > -1)
								{
									m_lastAnimationSegmentStart = "";
									m_lastAnimationSegmentEnd = marker.Key;
									return (true, (float)(marker.Value));
								}
							}
							return (false, 0.0f);
						}

						var (found, value) = FindValue();

						if (found)
						{
							m_progressPropertySet.InsertScalar(s_progressPropertyName, value);
						}
						else
						{
							// We also support setting the state proprety to a float value, which instructs the animated icon
							// to animate the Progress property to the provided value. Because wcstof returns 0.0f when the
							// provided string doesn't parse to a float we can't distinguish between the string "0.0" and
							// the string "a" (for example) from the parse output alone. Instead we use the wcstof's second
							// parameter to determine if the 0.0 return value came from a valid parse or from the default return.
							string strEnd = null;
							var parsedFloat = Wcstof(toState, ref strEnd);

							if (strEnd == toState)
							{
								PlaySegment(float.NaN, parsedFloat, cleanupAction, playbackMultiplier);
								m_lastAnimationSegmentStart = "";
								m_lastAnimationSegmentEnd = toState;
							}
							else
							{
								// None of our attempt to find an animation to play or frame to show have worked, so just cut
								// to frame 0.
								m_progressPropertySet.InsertScalar(s_progressPropertyName, 0.0f);
								m_lastAnimationSegmentStart = "";
								m_lastAnimationSegmentEnd = "0.0";
							}
						}
					}
					m_lastAnimationSegment = transitionName;
					AnimatedIconTestHooks.NotifyLastAnimationSegmentChanged(this);
				}
			}
		}

		private float Wcstof(string input, ref string strEnd)
		{
			for (int currentLength = input.Length; currentLength > 0; currentLength--)
			{
				var shortenedInput = input.Substring(0, currentLength);
				if (float.TryParse(shortenedInput, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var parsed))
				{
					if (input.Length - currentLength == 0)
					{
						strEnd = null;
					}
					else
					{
						strEnd = input.Substring(currentLength, input.Length - currentLength);
					}
					return parsed;
				}
			}
			strEnd = input;
			return 0.0f;
		}

		void PlaySegment(float from, float to, Action cleanupAction, float playbackMultiplier = 1.0f)
		{
			float GetSegmentLength(float from, float to, float previousSegmentLength)
			{
				if (float.IsNaN(from))
				{
					return previousSegmentLength;
				}
				return Math.Abs(to - from);
			}
			var segmentLength = GetSegmentLength(from, to, m_previousSegmentLength);

			m_previousSegmentLength = segmentLength;
			var duration = m_animatedVisual != null ?
				TimeSpan.FromMilliseconds(m_animatedVisual.Duration.TotalMilliseconds * segmentLength * (1.0 / playbackMultiplier) * m_durationMultiplier) :
				TimeSpan.Zero;
			// If the duration is really short (< 20ms) don't bother trying to animate, or if animations are disabled.
			if (duration < TimeSpan.FromMilliseconds(20) || !SharedHelpers.IsAnimationsEnabled())
			{
				m_progressPropertySet.InsertScalar(s_progressPropertyName, to);
				OnAnimationCompleted(null, null);
			}
			else
			{
				var compositor = m_progressPropertySet.Compositor;
				var animation = compositor.CreateScalarKeyFrameAnimation();
				animation.Duration = duration;
				var linearEasing = compositor.CreateLinearEasingFunction();

				// Play from fromProgress.
				if (!float.IsNaN(from))
				{
					animation.InsertKeyFrame(0, from);
				}

				// Play to toProgress
				animation.InsertKeyFrame(1, to, linearEasing);

				animation.IterationBehavior = AnimationIterationBehavior.Count;
				animation.IterationCount = 1;

				if (m_batch != null)
				{
					m_batchCompletedRevoker.Disposable = null;
				}
				m_batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

				m_batchCompletedRevoker.Disposable = null;
				m_batch.Completed += OnAnimationCompleted;
				m_batchCompletedRevoker.Disposable = Disposable.Create(() =>
				{
					m_batch.Completed -= OnAnimationCompleted;
				});

				m_isPlaying = true;
				m_progressPropertySet.StartAnimation(s_progressPropertyName, animation);
				if (cleanupAction != null)
				{
					cleanupAction();
				}
				m_batch.End();
			}
		}

		private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (!ConstructAndInsertVisual())
			{
				SetRootPanelChildToFallbackIcon();
			}
		}

		private void UpdateMirrorTransform()
		{
			ScaleTransform GetScaleTransform()
			{
				if (m_scaleTransform == null)
				{
					// Initialize the scale transform that will be used for mirroring and the
					// render transform origin as center in order to have the icon mirrored in place.
					Microsoft.UI.Xaml.Media.ScaleTransform scaleTransform = new ScaleTransform();

					RenderTransform = scaleTransform;
					RenderTransformOrigin = new Point(0.5, 0.5);
					m_scaleTransform = scaleTransform;
					return scaleTransform;
				}
				return m_scaleTransform;
			}

			var scaleTransform = GetScaleTransform();

			scaleTransform.ScaleX =
				FlowDirection == FlowDirection.RightToLeft &&
				!MirroredWhenRightToLeft &&
				m_canDisplayPrimaryContent ?
					-1.0f : 1.0f;
		}

		private void OnMirroredWhenRightToLeftPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateMirrorTransform();
		}

		private bool ConstructAndInsertVisual()
		{
			Visual GetVisual()
			{
				if (Source is { } source)
				{
					TrySetForegroundProperty(source);

					//object diagnostics{ };
#if HAS_UNO
					// TODO Uno - TryCreateAnimatedVisual method does not currently exist on IAnimatedVisualSource
					IAnimatedVisual visual = null;
#else
					var visual = source.TryCreateAnimatedVisual(Window.Current.Compositor, diagnostics);
#endif
					m_animatedVisual = visual;
					return visual != null ? visual.RootVisual : null;
				}

				else
				{
					m_animatedVisual = null;
					return (Visual)(null);
				}
			}

			var visual = GetVisual();

			if (m_rootPanel is { } rootPanel)
			{
				ElementCompositionPreview.SetElementChildVisual(rootPanel, visual);
			}

			if (visual != null)
			{
				m_canDisplayPrimaryContent = true;
				if (m_rootPanel is { } innerRootPanel)
				{
					// Remove the second child, if it exists, as this is the fallback icon.
					// Which we don't need because we have a visual now.
					if (innerRootPanel.Children.Count > 1)
					{
						innerRootPanel.Children.RemoveAt(1);
					}
				}
				visual.Properties.InsertScalar(s_progressPropertyName, 0.0F);

				// Tie the animated visual's Progress property to the player Progress with an ExpressionAnimation.
				var compositor = visual.Compositor;
				var expression = StringUtil.FormatString("_.%1!s!", s_progressPropertyName);
				var progressAnimation = compositor.CreateExpressionAnimation(expression);
				progressAnimation.SetReferenceParameter("_", m_progressPropertySet);
				visual.Properties.StartAnimation(s_progressPropertyName, progressAnimation);

				return true;
			}
			else
			{
				m_canDisplayPrimaryContent = false;
				return false;
			}

			// This seems to be a bug in WinUI - this code cannot be reached.
			// UpdateMirrorTransform();
		}

		private void OnFallbackIconSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			if (!m_canDisplayPrimaryContent)
			{
				SetRootPanelChildToFallbackIcon();
			}
		}

		private void SetRootPanelChildToFallbackIcon()
		{
			if (FallbackIconSource is { } iconSource)
			{
				var iconElement = iconSource.CreateIconElement();
				if (m_rootPanel is { } rootPanel)
				{
					// Remove the second child, if it exists, as this is the previous
					// fallback icon which we don't need because we have a visual now.
					if (rootPanel.Children.Count > 1)
					{
						rootPanel.Children.RemoveAt(1);
					}
					rootPanel.Children.Add(iconElement);
				}
			}
		}

		private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			m_foregroundColorPropertyChangedRevoker.Disposable = null;
			if (Foreground is SolidColorBrush foregroundSolidColorBrush)
			{
				var token = foregroundSolidColorBrush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, OnForegroundBrushColorPropertyChanged);
				m_foregroundColorPropertyChangedRevoker.Disposable = Disposable.Create(() =>
				{
					foregroundSolidColorBrush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, token);
				});
				TrySetForegroundProperty(foregroundSolidColorBrush.Color);
			}
		}

		private void OnFlowDirectionPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			UpdateMirrorTransform();
		}

		private void OnForegroundBrushColorPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			TrySetForegroundProperty((Color)sender.GetValue(args));
		}

		private void TrySetForegroundProperty(IAnimatedVisualSource2 source = null)
		{
			if (Foreground is SolidColorBrush foregroundSolidColorBrush)
			{
				TrySetForegroundProperty(foregroundSolidColorBrush.Color, source);
			}
		}

		private void TrySetForegroundProperty(Color color, IAnimatedVisualSource2 source = null)
		{
			var localSource = source != null ? source : Source;
			if (localSource != null)
			{
				localSource.SetColorProperty(s_foregroundPropertyName, color);
			}
		}

		private void OnAnimationCompleted(object sender, CompositionBatchCompletedEventArgs args)
		{
			if (m_batch != null)
			{
				m_batchCompletedRevoker.Disposable = null;
			}
			m_isPlaying = false;

			switch (m_queueBehavior)
			{
				case AnimatedIconAnimationQueueBehavior.Cut:
					break;
				case AnimatedIconAnimationQueueBehavior.QueueOne:
					if (m_queuedStates.Count > 0)
					{
						TransitionAndUpdateStates(m_currentState, m_queuedStates.Peek());
					}
					break;
				case AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne:
					if (m_queuedStates.Count > 0)
					{
						if (m_queuedStates.Count == 1)
						{
							TransitionAndUpdateStates(m_currentState, m_queuedStates.Peek());
						}
						else
						{
							TransitionAndUpdateStates(m_currentState, m_queuedStates.Peek(), m_isSpeedUp ? m_speedUpMultiplier : 1.0f);
						}
					}
					break;
			}
		}

		// Test hooks
		internal void SetAnimationQueueBehavior(AnimatedIconAnimationQueueBehavior behavior)
		{
			m_queueBehavior = behavior;
		}

		internal void SetDurationMultiplier(float multiplier)
		{
			m_durationMultiplier = multiplier;
		}

		internal void SetSpeedUpMultiplier(float multiplier)
		{
			m_speedUpMultiplier = multiplier;
		}

		internal void SetQueueLength(int length)
		{
			m_queueLength = length;
		}

		internal string GetLastAnimationSegment()
		{
			return m_lastAnimationSegment;
		}

		internal string GetLastAnimationSegmentStart()
		{
			return m_lastAnimationSegmentStart;
		}

		internal string GetLastAnimationSegmentEnd()
		{
			return m_lastAnimationSegmentEnd;
		}

	}
}
