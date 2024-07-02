// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshVisualizer.cpp, commit de78834

using System;
using System.Numerics;
using Microsoft.UI.Private.Controls;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Represents a control that provides animated state indicators for content refresh.
/// </summary>
public partial class RefreshVisualizer : Control, IRefreshVisualizerPrivate
{
#if !HAS_UNO
	//The Opacity of the progress indicator in the non-pending non-executing states
	private const float MINIMUM_INDICATOR_OPACITY = 0.4f;
#endif

	//The size of the default progress indicator
	private const int DEFAULT_INDICATOR_SIZE = 30;

#if !HAS_UNO
	//The position the progress indicator parallax animation places the indicator during manipulation
	private const float PARALLAX_POSITION_RATIO = 0.5f;
#endif

	~RefreshVisualizer()
	{
		if (m_refreshInfoProvider is { } refreshInfoProvider)
		{
			m_RefreshInfoProvider_InteractingForRefreshChangedToken.Disposable = null;
			m_RefreshInfoProvider_InteractionRatioChangedToken.Disposable = null;
		}
	}

	/// <summary>
	/// Initializes a new instance of the RefreshVisualizer class.
	/// </summary>
	public RefreshVisualizer()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_RefreshVisualizer);

		DefaultStyleKey = typeof(RefreshVisualizer);
	}

	protected override void OnApplyTemplate()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		// BEGIN: Populate template children
		m_root = (Panel)GetTemplateChild("Root");
		// END: Populate template children

		// BEGIN: Initialize our private backers to the dependency properties and initialize state.
		m_state = State;

		m_orientation = Orientation;
		OnOrientationChangedImpl();

		m_content = Content;
		if (m_content == null)
		{
			SymbolIcon defaultContent = new SymbolIcon(Symbol.Refresh);
			defaultContent.Height = DEFAULT_INDICATOR_SIZE;
			defaultContent.Width = DEFAULT_INDICATOR_SIZE;

			Content = defaultContent;
		}
		else
		{
			OnContentChangedImpl();
		}
		// END: Initialize

		UpdateContent();
	}

	/// <summary>
	/// Initiates an update of the content.
	/// </summary>
	public void RequestRefresh()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		UpdateRefreshState(RefreshVisualizerState.Refreshing);
		if (m_refreshInfoProvider != null)
		{
			m_refreshInfoProvider.OnRefreshStarted();
		}
		RaiseRefreshRequested();
	}

	//Private Interface methods
	private IRefreshInfoProvider InfoProvider
	{
		get => (IRefreshInfoProvider)GetValue(InfoProviderProperty);
		set => SetValue(InfoProviderProperty, value);
	}

	IRefreshInfoProvider IRefreshVisualizerPrivate.InfoProvider
	{
		get => InfoProvider;
		set => InfoProvider = value;
	}

	private void SetInternalPullDirection(RefreshPullDirection value)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, value);
		m_pullDirection = value;
		OnOrientationChangedImpl();
		UpdateContent();
	}

	void IRefreshVisualizerPrivate.SetInternalPullDirection(RefreshPullDirection value) => SetInternalPullDirection(value);

	//Privates
	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		if (property == InfoProviderProperty)
		{
			OnRefreshInfoProviderChanged(args);
		}
		else if (property == OrientationProperty)
		{
			OnOrientationChanged(args);
		}
		else if (property == StateProperty)
		{
			OnStateChanged(args);
		}
		else if (property == ContentProperty)
		{
			OnContentChanged(args);
		}
	}

	private void OnRefreshInfoProviderChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_PTR_PTR, METH_NAME, this, args.OldValue(), args.NewValue());
		if (m_refreshInfoProvider != null)
		{
			m_RefreshInfoProvider_InteractingForRefreshChangedToken.Disposable = null;
			m_RefreshInfoProvider_InteractionRatioChangedToken.Disposable = null;
		}

		m_refreshInfoProvider = InfoProvider;

		if (m_refreshInfoProvider != null)
		{
			m_refreshInfoProvider.IsInteractingForRefreshChanged += RefreshInfoProvider_InteractingForRefreshChanged;
			m_RefreshInfoProvider_InteractingForRefreshChangedToken.Disposable = Disposable.Create(() =>
			{
				m_refreshInfoProvider.IsInteractingForRefreshChanged -= RefreshInfoProvider_InteractingForRefreshChanged;
			});

			m_refreshInfoProvider.InteractionRatioChanged += RefreshInfoProvider_InteractionRatioChanged;
			m_RefreshInfoProvider_InteractionRatioChangedToken.Disposable = Disposable.Create(() =>
			{
				m_refreshInfoProvider.InteractionRatioChanged -= RefreshInfoProvider_InteractionRatioChanged;
			});

			m_executionRatio = m_refreshInfoProvider.ExecutionRatio;
		}
		else
		{
			m_RefreshInfoProvider_InteractingForRefreshChangedToken = default;
			m_RefreshInfoProvider_InteractionRatioChangedToken = default;

			m_executionRatio = 1.0f;
		}
	}

	private void OnOrientationChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.OldValue(), args.NewValue());
		m_orientation = Orientation;
		OnOrientationChangedImpl();
		UpdateContent();
	}

	private void OnOrientationChangedImpl()
	{
		switch (m_orientation)
		{
			case RefreshVisualizerOrientation.Auto:
				switch (m_pullDirection)
				{
					case RefreshPullDirection.TopToBottom:
					case RefreshPullDirection.BottomToTop:
						m_startingRotationAngle = 0.0f;
						break;
					case RefreshPullDirection.LeftToRight:
						m_startingRotationAngle = (float)-Math.PI / 2;
						break;
					case RefreshPullDirection.RightToLeft:
						m_startingRotationAngle = (float)Math.PI / 2;
						break;
				}
				break;
			case RefreshVisualizerOrientation.Normal:
				m_startingRotationAngle = 0.0f;
				break;
			case RefreshVisualizerOrientation.Rotate270DegreesCounterclockwise:
				m_startingRotationAngle = (float)-Math.PI / 2;
				break;
			case RefreshVisualizerOrientation.Rotate90DegreesCounterclockwise:
				m_startingRotationAngle = (float)Math.PI / 2;
				break;
			default:
				MUX_ASSERT(false);
				break;
		}
	}

	private void OnStateChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, args.OldValue(), args.NewValue());
		RefreshVisualizerState oldstate = m_state;
		m_state = State;
		UpdateContent();
		RaiseRefreshStateChanged(oldstate, m_state);
	}


	private void OnContentChanged(DependencyPropertyChangedEventArgs args)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_PTR_PTR, METH_NAME, this, args.OldValue(), args.NewValue());
		m_content = Content;
		OnContentChangedImpl();
		UpdateContent();
	}

	private void OnContentChangedImpl()
	{
		if (m_root != null)
		{
			// There is a slight parallax animation of the progress indicator as the IRefreshInfoProvider updates
			// the interaction ratio. Unfortunately, this composition animation would be clobbered by setting the alignment
			// properties of the visual's Xaml object. To get around this we wrap the indicator in a container and set the
			// container's alignment properties instead. On RS2+ we can instead animate the Translation XAML property, if
			// the progress Indicator is a Framework Element and has the Vertical/Horizontal Alignment properties.
			m_root.Children.Clear();

			if (m_content == null)
			{
				SymbolIcon defaultContent = new SymbolIcon(Symbol.Refresh);
				defaultContent.Height = DEFAULT_INDICATOR_SIZE;
				defaultContent.Width = DEFAULT_INDICATOR_SIZE;

				m_content = defaultContent;
			}

			Panel m_containerPanel = m_root;
			var contentAsFrameworkElement = m_content as FrameworkElement;

			if (!SharedHelpers.IsRS2OrHigher() || contentAsFrameworkElement == null)
			{
				m_containerPanel = new Grid();
				m_root.Children.Insert(0, m_containerPanel);
				m_containerPanel.VerticalAlignment = VerticalAlignment.Center;
				m_containerPanel.HorizontalAlignment = HorizontalAlignment.Center;
			}
			else
			{
				ElementCompositionPreview.SetIsTranslationEnabled(m_content, true);
				contentAsFrameworkElement.VerticalAlignment = VerticalAlignment.Center;
				contentAsFrameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			}

			m_containerPanel.Children.Insert(0, m_content);
		}
	}

	private void UpdateContent()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_content != null)
		{
#if !HAS_UNO
			Visual contentVisual = ElementCompositionPreview.GetElementVisual(m_content);

			Size contentSize = m_content.RenderSize;
			contentVisual.CenterPoint = new Vector3((float)(contentSize.Height / 2), (float)(contentSize.Width / 2), 0.0f);

			switch (m_state)
			{
				case RefreshVisualizerState.Idle:
					contentVisual.Opacity = MINIMUM_INDICATOR_OPACITY;
					contentVisual.RotationAngle = m_startingRotationAngle;
					//On RS2 and above we achieve the parallax animation using the Translation property, so we set the appropriate field here.
					if (SharedHelpers.IsRS2OrHigher())
					{
						contentVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
					}
					else
					{
						contentVisual.Offset = new Vector3(0.0f, 0.0f, 0.0f);
					}

					break;
				case RefreshVisualizerState.Peeking:
					contentVisual.Opacity = 1.0f;
					contentVisual.RotationAngle = m_startingRotationAngle;
					break;
				case RefreshVisualizerState.Interacting:
					contentVisual.Opacity = MINIMUM_INDICATOR_OPACITY;
					ExecuteInteractingAnimations();
					break;
				case RefreshVisualizerState.Pending:
					ExecuteScaleUpAnimation();
					contentVisual.Opacity = 1.0f;
					contentVisual.RotationAngle = m_startingRotationAngle;
					break;
				case RefreshVisualizerState.Refreshing:
					ExecuteExecutingRotationAnimation();
					contentVisual.Opacity = 1.0f;
					if (m_root != null)
					{
						float GetTranslationRatio()
						{
							if (m_refreshInfoProvider is { } refreshInfoProvider)
							{
								return (1.0f - (float)(refreshInfoProvider.ExecutionRatio)) * PARALLAX_POSITION_RATIO;
							}
							return 1.0f;
						}
						float translationRatio = GetTranslationRatio();
						translationRatio = IsPullDirectionFar() ? -1.0f * translationRatio : translationRatio;
						//On RS2 and above we achieve the parallax animation using the Translation property, so we set the appropriate field here.
						if (SharedHelpers.IsRS2OrHigher())
						{
							if (IsPullDirectionVertical())
							{
								contentVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, translationRatio * (float)m_root.ActualHeight, 0.0f));
							}
							else
							{
								contentVisual.Properties.InsertVector3("Translation", new Vector3(translationRatio * (float)m_root.ActualWidth, 0.0f, 0.0f));
							}
						}
						else
						{
							if (IsPullDirectionVertical())
							{
								contentVisual.Offset = new Vector3(0.0f, translationRatio * (float)m_root.ActualHeight, 0.0f);
							}
							else
							{
								contentVisual.Offset = new Vector3(translationRatio * (float)m_root.ActualHeight, 0.0f, 0.0f);
							}
						}
					}

					break;
				default:
					MUX_ASSERT(false);
					break;
			}
#else
			Size contentSize = m_content.RenderSize;

			m_content.RenderTransformOrigin = new Point(0.5, 0.5);
			if (m_content.RenderTransform is not MatrixTransform matrixTransform)
			{
				matrixTransform = new MatrixTransform();
				m_content.RenderTransform = matrixTransform;
			}
#endif

#if !HAS_UNO
			switch (m_state)
			{
				case RefreshVisualizerState.Idle:
					m_content.Opacity = MINIMUM_INDICATOR_OPACITY;
					matrixTransform.Matrix = new Matrix(Matrix3x2.CreateRotation(m_startingRotationAngle));
					//On RS2 and above we achieve the parallax animation using the Translation property, so we set the appropriate field here.
					if (SharedHelpers.IsRS2OrHigher())
					{
						contentVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, 0.0f, 0.0f));
					}
					else
					{
						contentVisual.Offset = new Vector3(0.0f, 0.0f, 0.0f);
					}

					break;
				case RefreshVisualizerState.Peeking:
					m_content.Opacity = 1.0f;
					contentVisual.RotationAngle = m_startingRotationAngle;
					break;
				case RefreshVisualizerState.Interacting:
					m_content.Opacity = MINIMUM_INDICATOR_OPACITY;
					ExecuteInteractingAnimations();
					break;
				case RefreshVisualizerState.Pending:
					ExecuteScaleUpAnimation();
					m_content.Opacity = 1.0f;
					contentVisual.RotationAngle = m_startingRotationAngle;
					break;
				case RefreshVisualizerState.Refreshing:
					ExecuteExecutingRotationAnimation();
					m_content.Opacity = 1.0f;
					if (m_root != null)
					{
						float GetTranslationRatio()
						{
							if (m_refreshInfoProvider is { } refreshInfoProvider)
							{
								return (1.0f - (float)(refreshInfoProvider.ExecutionRatio)) * PARALLAX_POSITION_RATIO;
							}
							return 1.0f;
						}
						float translationRatio = GetTranslationRatio();
						translationRatio = IsPullDirectionFar() ? -1.0f * translationRatio : translationRatio;
						//On RS2 and above we achieve the parallax animation using the Translation property, so we set the appropriate field here.
						if (SharedHelpers.IsRS2OrHigher())
						{
							if (IsPullDirectionVertical())
							{
								contentVisual.Properties.InsertVector3("Translation", new Vector3(0.0f, translationRatio * (float)m_root.ActualHeight, 0.0f));
							}
							else
							{
								contentVisual.Properties.InsertVector3("Translation", new Vector3(translationRatio * (float)m_root.ActualWidth, 0.0f, 0.0f));
							}
						}
						else
						{
							if (IsPullDirectionVertical())
							{
								contentVisual.Offset = new Vector3(0.0f, translationRatio * (float)m_root.ActualHeight, 0.0f);
							}
							else
							{
								contentVisual.Offset = new Vector3(translationRatio * (float)m_root.ActualHeight, 0.0f, 0.0f);
							}
						}
					}

					break;
				default:
					MUX_ASSERT(false);
					break;
			}
#endif
		}
	}

#if !HAS_UNO
	private void ExecuteInteractingAnimations()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_content != null && m_refreshInfoProvider != null)
		{
			Visual contentVisual = ElementCompositionPreview.GetElementVisual(m_content);
			if (m_compositor == null)
			{
				m_compositor = contentVisual.Compositor;
			}

			//Set up the InteractionRatioRotationAnimation
			Size contentSize = m_content.RenderSize;
			contentVisual.CenterPoint = new Vector3((float)(contentSize.Height / 2), (float)(contentSize.Width / 2), 0.0f);

			string interactionRatioPropertyName = m_refreshInfoProvider.InteractionRatioCompositionProperty;
			CompositionPropertySet interactionRatioPropertySet = m_refreshInfoProvider.CompositionProperties;

			ExpressionAnimation contentInteractionRatioRotationAnimation = m_compositor.CreateExpressionAnimation(
			   "startingRotationAngle + (Pi * (Clamp(RefreshInteractionRatioPropertySet." +
			   (string)(interactionRatioPropertyName) +
			   ", 0.0f, contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO) / contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO) * 2)");

			var thresholdRatioName = "DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO";
			contentVisual.Properties.InsertScalar(thresholdRatioName, (float)(m_executionRatio));
			contentInteractionRatioRotationAnimation.SetReferenceParameter("contentVisual", contentVisual);
			contentInteractionRatioRotationAnimation.SetReferenceParameter("RefreshInteractionRatioPropertySet", interactionRatioPropertySet);
			contentInteractionRatioRotationAnimation.SetScalarParameter("startingRotationAngle", m_startingRotationAngle);

			contentVisual.StartAnimation("RotationAngle", contentInteractionRatioRotationAnimation);

			//Set up the InteractionRatioOpacityAnimation
			ExpressionAnimation contentInteractionRatioOpacityAnimation = m_compositor.CreateExpressionAnimation(
			   "((1.0f - contentVisual.MINIMUM_INDICATOR_OPACITY) * RefreshInteractionRatioPropertySet."
			   + (string)(interactionRatioPropertyName) +
			   ") + contentVisual.MINIMUM_INDICATOR_OPACITY");
			var minOpacityName = "MINIMUM_INDICATOR_OPACITY";
			contentVisual.Properties.InsertScalar(minOpacityName, MINIMUM_INDICATOR_OPACITY);
			contentInteractionRatioOpacityAnimation.SetReferenceParameter("contentVisual", contentVisual);
			contentInteractionRatioOpacityAnimation.SetReferenceParameter("RefreshInteractionRatioPropertySet", interactionRatioPropertySet);

			//contentVisual.StartAnimation("Opacity", contentInteractionRatioOpacityAnimation);

			//Set up the InteractionRatioParallaxAnimation
			ExpressionAnimation? contentInteractionRatioParallaxAnimation = null;
			if (IsPullDirectionFar())
			{
				contentInteractionRatioParallaxAnimation = m_compositor.CreateExpressionAnimation(
					"((1.0f - contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO) * rootSize * 0.5f * -1.0f) * min((RefreshInteractionRatioPropertySet."
					+ (string)(interactionRatioPropertyName) +
					" / contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO), 1.0f)");
			}
			else
			{
				contentInteractionRatioParallaxAnimation = m_compositor.CreateExpressionAnimation(
					"((1.0f - contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO) * rootSize * 0.5f) * min((RefreshInteractionRatioPropertySet."
					+ (string)(interactionRatioPropertyName) +
					" / contentVisual.DEFAULT_REFRESHINDICATOR_THRESHOLD_RATIO), 1.0f)");
			}
			if (m_root != null)
			{
				contentInteractionRatioParallaxAnimation.SetScalarParameter("rootSize", (float)(IsPullDirectionVertical() ? m_root.ActualHeight : m_root.ActualWidth));
			}
			else
			{
				contentInteractionRatioParallaxAnimation.SetScalarParameter("rootSize", 0.0f);
			}

			contentInteractionRatioParallaxAnimation.SetReferenceParameter("contentVisual", contentVisual);
			contentInteractionRatioParallaxAnimation.SetReferenceParameter("RefreshInteractionRatioPropertySet", interactionRatioPropertySet);

			//On RS2 and above we achieve the parallax animation using the Translation property, so we animate the appropriate field here.
			if (!SharedHelpers.IsRS2OrHigher())
			{
				if (IsPullDirectionVertical())
				{
					contentVisual.StartAnimation("Offset.Y", contentInteractionRatioParallaxAnimation);
				}
				else
				{
					contentVisual.StartAnimation("Offset.X", contentInteractionRatioParallaxAnimation);
				}
			}
			else
			{
				if (IsPullDirectionVertical())
				{
					contentVisual.StartAnimation("Translation.Y", contentInteractionRatioParallaxAnimation);
				}
				else
				{
					contentVisual.StartAnimation("Translation.X", contentInteractionRatioParallaxAnimation);
				}
			}
		}
	}

	private void ExecuteScaleUpAnimation()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_content != null)
		{
			Visual contentVisual = ElementCompositionPreview.GetElementVisual(m_content);
			if (m_compositor == null)
			{
				m_compositor = contentVisual.Compositor;
			}

			Vector2KeyFrameAnimation contentScaleAnimation = m_compositor.CreateVector2KeyFrameAnimation();
			contentScaleAnimation.InsertKeyFrame(0.5f, new Vector2(1.50f, 1.50f));
			contentScaleAnimation.InsertKeyFrame(1.0f, new Vector2(1.0f, 1.0f));
			contentScaleAnimation.Duration = TimeSpan.FromMilliseconds(300);

			Size contentSize = m_content.RenderSize;
			contentVisual.CenterPoint = new Vector3((float)(contentSize.Height / 2), (float)(contentSize.Width / 2), 0.0f);

			contentVisual.StartAnimation("Scale.XY", contentScaleAnimation);
		}
	}

	private void ExecuteExecutingRotationAnimation()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		if (m_content != null)
		{
			Visual contentVisual = ElementCompositionPreview.GetElementVisual(m_content);
			if (m_compositor == null)
			{
				m_compositor = contentVisual.Compositor;
			}

			ScalarKeyFrameAnimation contentExecutionRotationAnimation = m_compositor.CreateScalarKeyFrameAnimation();
			contentExecutionRotationAnimation.InsertKeyFrame(0.0f, m_startingRotationAngle, m_compositor.CreateLinearEasingFunction());
			contentExecutionRotationAnimation.InsertKeyFrame(1.0f, m_startingRotationAngle + (float)(2.0f * Math.PI), m_compositor.CreateLinearEasingFunction());
			contentExecutionRotationAnimation.Duration = TimeSpan.FromMilliseconds(500);
			contentExecutionRotationAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

			Size contentSize = m_content.RenderSize;
			contentVisual.CenterPoint = new Vector3((float)(contentSize.Height / 2), (float)(contentSize.Width / 2), 0.0f);

			contentVisual.StartAnimation("RotationAngle", contentExecutionRotationAnimation);
		}
	}
#endif

	private void UpdateRefreshState(RefreshVisualizerState newState)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, newState);
		if (newState != m_state)
		{
			State = newState;
		}
	}

	private void RaiseRefreshStateChanged(RefreshVisualizerState oldState, RefreshVisualizerState newState)
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT_INT, METH_NAME, this, oldState, newState);
		var e = new RefreshStateChangedEventArgs(oldState, newState);
		RefreshStateChanged?.Invoke(this, e);
	}

	private void RaiseRefreshRequested()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		//	com_ptr<RefreshVisualizer> strongThis = get_strong();

		var instance = new Deferral(() =>
		{
			//this.CheckThread();
			RefreshCompleted();
		});

		var args = new RefreshRequestedEventArgs(instance);

		//This makes sure that everyone registered for this event can get access to the deferral
		//Otherwise someone could complete the deferral before someone else has had a chance to grab it
		args.IncrementDeferralCount();
		RefreshRequested?.Invoke(this, args);
		args.DecrementDeferralCount();
	}

	private void RefreshCompleted()
	{
		//PTR_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
		UpdateRefreshState(RefreshVisualizerState.Idle);
		if (m_refreshInfoProvider != null)
		{
			m_refreshInfoProvider.OnRefreshCompleted();
		}
	}

	private void RefreshInfoProvider_InteractingForRefreshChanged(object sender, object e)
	{
		if (m_refreshInfoProvider != null)
		{
			m_isInteractingForRefresh = m_refreshInfoProvider.IsInteractingForRefresh;
			//PTR_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, m_isInteractingForRefresh);
			if (!m_isInteractingForRefresh)
			{
				switch (m_state)
				{
					case RefreshVisualizerState.Pending:
						// User stopped interaction after it hit the Pending state.
						RequestRefresh();
						break;
					case RefreshVisualizerState.Refreshing:
						// We don't want to interrupt a currently executing refresh.
						break;
					default:
						//Peeking, interacting, or idle results in idle.
						UpdateRefreshState(RefreshVisualizerState.Idle);
						break;
				}
			}
		}
	}

	private void RefreshInfoProvider_InteractionRatioChanged(IRefreshInfoProvider sender, RefreshInteractionRatioChangedEventArgs e)
	{
		//PTR_TRACE_INFO(nullptr, TRACE_MSG_METH_DBL, METH_NAME, this, e.InteractionRatio());
		bool wasAtZero = m_interactionRatio == 0.0f;
		m_interactionRatio = e.InteractionRatio;
		if (m_isInteractingForRefresh)
		{
			if (m_state == RefreshVisualizerState.Idle)
			{
				if (wasAtZero)
				{
					if (m_interactionRatio > m_executionRatio)
					{
						//Sometimes due to missed frames in the interplay of comp and xaml the interaction tracker will 'jump' passed the executionRatio on the first Value changed.
						UpdateRefreshState(RefreshVisualizerState.Pending);
					}
					else if (m_interactionRatio > 0.0f)
					{
						UpdateRefreshState(RefreshVisualizerState.Interacting);
					}
				}
				else if (m_interactionRatio > 0.0f)
				{
					// TODO: IRefreshInfoProvider does not raise InteractionRatioChanged yet when DManip is overpanning. Thus we do not yet
					// enter the Peeking state when DManip overpans in inertia.
					UpdateRefreshState(RefreshVisualizerState.Peeking);
				}
			}
			else if (m_state == RefreshVisualizerState.Interacting)
			{
				if (m_interactionRatio <= 0.0f)
				{
					UpdateRefreshState(RefreshVisualizerState.Idle);
				}
				else if (m_interactionRatio > m_executionRatio)
				{
					UpdateRefreshState(RefreshVisualizerState.Pending);
				}
			}
			else if (m_state == RefreshVisualizerState.Pending)
			{
				if (m_interactionRatio <= m_executionRatio)
				{
					UpdateRefreshState(RefreshVisualizerState.Interacting);
				}
				else if (m_interactionRatio <= 0.0f)
				{
					UpdateRefreshState(RefreshVisualizerState.Idle);
				}
			}
			//If we are in Refreshing or Peeking we want to stay in those states.
		}
		else
		{
			//If we are not refreshing or interacting for refresh then the only valid states are Peeking and Idle
			if (m_state != RefreshVisualizerState.Refreshing)
			{
				if (m_interactionRatio > 0.0f)
				{
					UpdateRefreshState(RefreshVisualizerState.Peeking);
				}
				else
				{
					UpdateRefreshState(RefreshVisualizerState.Idle);
				}
			}
		}
	}

#if !HAS_UNO
	private bool IsPullDirectionVertical()
	{
		return m_pullDirection == RefreshPullDirection.TopToBottom || m_pullDirection == RefreshPullDirection.BottomToTop;
	}

	private bool IsPullDirectionFar()
	{
		return m_pullDirection == RefreshPullDirection.BottomToTop || m_pullDirection == RefreshPullDirection.RightToLeft;
	}
#endif
}
