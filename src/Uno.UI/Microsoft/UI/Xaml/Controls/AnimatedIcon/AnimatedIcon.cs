using System;
using System.Numerics;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

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

			m_progressPropertySet = Window.Current.Compositor.CreatePropertySet();
			m_progressPropertySet.InsertScalar(s_progressPropertyName, 0);
			Loaded += OnLoaded;

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
					if (var path = panel.Children[0].GetAt(0))
            {
						path.Visibility(Visibility.Collapsed);
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
			// AnimatedIcon might get added to a UI which has already set the State property on an ancestor.
			// If this is the case and the animated icon being added doesn't have its own state property
			// We copy the ancestor value when we load. Additionally we attach to our ancestor's property
			// changed event for AnimatedIcon.State to copy the value to AnimatedIcon.
			var property = StateProperty;

			var[ancestorWithState, stateValue] = [this, property]()


	{
				var parent = VisualTreeHelper.GetParent(this);
				while (parent)
				{
					var stateValue = parent.GetValue(property);
					if (!(string)stateValue).empty()


			{
						return Tuple.Create(parent, stateValue);
					}
					parent = VisualTreeHelper.GetParent(parent);
				}
				return Tuple.Create((DependencyObject)(null), string{ });
			} ();

			if ((string)GetValue(property)).empty()


	{
				SetValue(property, stateValue);
			}

			if (ancestorWithState)
			{
				m_ancestorStatePropertyChangedRevoker = RegisterPropertyChanged(ancestorWithState, property, { this, &AnimatedIcon.OnAncestorAnimatedIconStatePropertyChanged });
			}

			// Wait until loaded to apply the fallback icon source property because we need the icon source
			// properties to be set before we create the icon element from it.  If those poperties are bound in,
			// they will not have been set during OnApplyTemplate.
			OnFallbackIconSourcePropertyChanged(null);
		}


		Size MeasureOverride(Size & availableSize)
		{
			if (var visual = m_animatedVisual)
    {
				// Animated Icon scales using the Uniform strategy, meaning that it scales the horizonal and vertical
				// dimensions equally by the maximum amount that doesn't exceed the available size in either dimension.
				// If the available size is infinite in both dimensions then we don't scale the visual. Otherwise, we
				// calculate the scale factor by comparing the default visual size to the available size. This produces 2
				// scale factors, one for each dimension. We choose the smaller of the scale factors to not exceed the
				// available size in that dimension.
				var visualSize = visual.Size();
				if (visualSize != float2.zero())
				{
					var widthScale = availableSize.Width == std.numeric_limits<double>.infinity() ? std.numeric_limits<float>.infinity() : availableSize.Width / visualSize.x;
					var heightScale = availableSize.Height == std.numeric_limits<double>.infinity() ? std.numeric_limits<float>.infinity() : availableSize.Height / visualSize.y;
					if (widthScale == std.numeric_limits<double>.infinity() && heightScale == std.numeric_limits<double>.infinity())
					{
						return visualSize;
					}
					else if (widthScale == std.numeric_limits<double>.infinity())
					{
						return Size{ visualSize.x* heightScale, availableSize.Height };
					}
					else if (heightScale == std.numeric_limits<double>.infinity())
					{
						return Size{ availableSize.Width, visualSize.y* widthScale };
					}
					else
					{
						return (heightScale > widthScale)
							? Size{ availableSize.Width, visualSize.y* widthScale }
                : Size{ visualSize.x* heightScale, availableSize.Height };
					}
				}
				return visualSize;
			}
	// If we don't have a visual, we will show the fallback icon, so we need to do a traditional measure.
			else
			{
				return __super.MeasureOverride(availableSize);
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (m_animatedVisual is { } visual)
			{
				var visualSize = visual.Size;

				Vector2 GetScale(Size finalSize, Vector2 visualSize)
				{
					var scale = (Vector2)(finalSize) / visualSize;
					if (scale.X < scale.Y)
					{
						scale.Y = scale.X;
					}
					else
					{
						scale.X = scale.Y;
					}
					return scale;
				}
				var scale = GetScale(finalSize, visualSize);

				Vector2 arrangedSize = new Vector2(
					Math.Min(finalSize.Width / scale.X, visualSize.X),
					Math.Min(finalSize.Height / scale.Y, visualSize.Y));
				var offset = (finalSize - (visualSize * scale)) / 2;
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
		void OnStatePropertyChanged()
		{
			m_pendingState = ValueHelper<string>.CastOrUnbox(this.GetValue(AnimatedIconStateProperty()));
			m_layoutUpdatedRevoker = this.LayoutUpdated(auto_revoke, { this, &AnimatedIcon.OnLayoutUpdatedAfterStateChanged });
			SharedHelpers.QueueCallbackForCompositionRendering(


				[strongThis = get_strong()]


		{
				strongThis.InvalidateArrange();
			}
    );
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
						// If we already have a queued state, cancel the current animation with the previously queued transition
						// then Queue this new transition.
						if (!string.IsNullOrEmpty(m_queuedState))
						{
							TransitionAndUpdateStates(m_currentState, m_queuedState);
						}
						m_queuedState = m_pendingState;
					}
					else
					{
						TransitionAndUpdateStates(m_currentState, m_pendingState);
					}
					break;
				case AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne:
					if (m_isPlaying)
					{
						// Cancel the previous animation completed handler, before we cancel that animation by starting a new one.
						if (m_batch != null)
						{
							m_batchCompletedRevoker.Disposable = null;
						}

						// If we already have a queued state, cancel the current animation with the previously queued transition
						//  played speed up then Queue this new transition.
						if (!string.IsNullOrEmpty(m_queuedState))
						{
							TransitionAndUpdateStates(m_currentState, m_queuedState, m_speedUpMultiplier);
							m_queuedState = m_pendingState;
						}
						else
						{
							m_queuedState = m_pendingState;

							var markers = Source.Markers;
							string transitionEndName = StringUtil.FormatString("%1!s!%2!s!%3!s!%4!s!", m_previousState, s_transitionInfix, m_currentState, s_transitionEndSuffix);
							var hasEndMarker = markers.ContainsKey(transitionEndName);
							if (hasEndMarker)
							{
								PlaySegment(float.NaN, (float)(markers[transitionEndName]), m_speedUpMultiplier);
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
			TransitionStates(fromState, toState, playbackMultiplier);
			m_previousState = fromState;
			m_currentState = toState;
			m_queuedState = "";
		}

		private void TransitionStates(string fromState, string toState, float playbackMultiplier = 1.0f)
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
						PlaySegment(fromProgress, toProgress, playbackMultiplier);
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
						var[found, value] = [toState, markers, this]()


				{
							string fragment = StringUtil.FormatString("%1!s!%2!s!%3!s!", s_transitionInfix.data(), toState.c_str(), s_transitionEndSuffix.data());
							for (var[key, val] : markers)
							{
								std.string value = key.data();
								if (value.find(fragment) != std.wstring.npos)
								{
									m_lastAnimationSegmentStart = "";
									m_lastAnimationSegmentEnd = key;
									return Tuple.Create(true, (float)(val));
								}
							}
							return Tuple.Create(false, 0.0f);
						} ();
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
							wchar_t* strEnd = null;
							var parsedFloat = wcstof(toState.c_str(), &strEnd);

							if (strEnd == toState.c_str() + toState.size())
							{
								PlaySegment(NAN, parsedFloat, playbackMultiplier);
								m_lastAnimationSegmentStart = "";
								m_lastAnimationSegmentEnd = toState;
							}
							else
							{
								// None of our attempt to find an animation to play or frame to show have worked, so just cut
								// to frame 0.
								m_progressPropertySet.InsertScalar(s_progressPropertyName, 0.0);
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

		void PlaySegment(float from, float to, float playbackMultiplier = 1.0f)
		{
			var segmentLength = [from, to, previousSegmentLength = m_previousSegmentLength]()


	{
				if (std.isnan(from))
				{
					return previousSegmentLength;
				}
				return std.abs(to - from);
			} ();

			m_previousSegmentLength = segmentLength;
			var duration = m_animatedVisual ?
				std.chrono.duration_cast<TimeSpan>(m_animatedVisual.Duration() * segmentLength * (1.0 / playbackMultiplier) * m_durationMultiplier) :
				TimeSpan.zero();
			// If the duration is really short (< 20ms) don't bother trying to animate, or if animations are disabled.
			if (duration < TimeSpan{ 20ms } || !SharedHelpers.IsAnimationsEnabled())
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
				m_batchCompletedRevoker = RegisterScopedBatchCompleted(m_batch, { this, &AnimatedIcon.OnAnimationCompleted });

				m_isPlaying = true;
				m_progressPropertySet.StartAnimation(s_progressPropertyName, animation);
				m_batch.End();
			}
		}

		private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs&)
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
					Windows.UI.Xaml.Media.ScaleTransform scaleTransform = new ScaleTransform();

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

		private void OnMirroredWhenRightToLeftPropertyChanged(DependencyPropertyChangedEventArgs&)
		{
			UpdateMirrorTransform();
		}

		private bool ConstructAndInsertVisual()
		{
			var visual = [this]()


	{
				if (Source is { } source)
        {
					TrySetForegroundProperty(source);

					object diagnostics{ };
					var visual = source.TryCreateAnimatedVisual(Window.Current.Compositor, diagnostics);
					m_animatedVisual = visual;
					return visual ? visual.RootVisual() : null;
				}

		else
				{
					m_animatedVisual = null;
					return (Visual)(null);
				}
			} ();

			if (m_rootPanel is { } rootPanel)
			{
				ElementCompositionPreview.SetElementChildVisual(rootPanel, visual);
			}

			if (visual)
			{
				m_canDisplayPrimaryContent = true;
				if (m_rootPanel is { } rootPanel)
				{
					// Remove the second child, if it exists, as this is the fallback icon.
					// Which we don't need because we have a visual now.
					if (rootPanel.Children.Count > 1)
					{
						rootPanel.Children.RemoveAt(1);
					}
				}
				visual.Properties().InsertScalar(s_progressPropertyName, 0.0F);

				// Tie the animated visual's Progress property to the player Progress with an ExpressionAnimation.
				var compositor = visual.Compositor();
				var expression = StringUtil.FormatString("_.%1!s!", s_progressPropertyName.data());
				var progressAnimation = compositor.CreateExpressionAnimation(expression);
				progressAnimation.SetReferenceParameter("_", m_progressPropertySet);
				visual.Properties().StartAnimation(s_progressPropertyName, progressAnimation);

				return true;
			}
			else
			{
				m_canDisplayPrimaryContent = false;
				return false;
			}

			UpdateMirrorTransform();
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
				m_foregroundColorPropertyChangedRevoker = foregroundSolidColorBrush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, OnForegroundBrushColorPropertyChanged);
				TrySetForegroundProperty(foregroundSolidColorBrush.Color);
			}
		}

		private void OnFlowDirectionPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			UpdateMirrorTransform();
		}

		private void OnForegroundBrushColorPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			TrySetForegroundProperty(sender.GetValue(args).as< Color > ());
		}

		private void TrySetForegroundProperty(IAnimatedVisualSource2 source)
		{
			if (Foreground is SolidColorBrush foregroundSolidColorBrush)
			{
				TrySetForegroundProperty(foregroundSolidColorBrush.Color, source);
			}
		}

		private void TrySetForegroundProperty(Color color, IAnimatedVisualSource2 source)
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
				case AnimatedIconAnimationQueueBehavior.SpeedUpQueueOne:
					if (!string.IsNullOrEmpty(m_queuedState))
					{
						TransitionAndUpdateStates(m_currentState, m_queuedState);
					}
					break;
			}
		}

		// Test hooks
		private void SetAnimationQueueBehavior(AnimatedIconAnimationQueueBehavior behavior)
		{
			m_queueBehavior = behavior;
		}

		private void SetDurationMultiplier(float multiplier)
		{
			m_durationMultiplier = multiplier;
		}

		private void SetSpeedUpMultiplier(float multiplier)
		{
			m_speedUpMultiplier = multiplier;
		}

		private string GetLastAnimationSegment()
		{
			return m_lastAnimationSegment;
		}

		private string GetLastAnimationSegmentStart()
		{
			return m_lastAnimationSegmentStart;
		}

		private string GetLastAnimationSegmentEnd()
		{
			return m_lastAnimationSegmentEnd;
		}

	}
}
