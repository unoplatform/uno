#if HAS_UNO
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

using RefreshPullDirection = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshPullDirection;
using RefreshInteractionRatioChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshInteractionRatioChangedEventArgs;
using IAdapterAnimationHandler = Microsoft.UI.Private.Controls.IAdapterAnimationHandler;
using IRefreshInfoProvider = Microsoft.UI.Private.Controls.IRefreshInfoProvider;
using IRefreshInfoProviderAdapter = Microsoft.UI.Private.Controls.IRefreshInfoProviderAdapter;
using PullToRefreshHelperTestApi = Microsoft.UI.Private.Controls.PullToRefreshHelperTestApi;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace MUXControlsTestApp
{
	internal class ImageIRefreshInfoProviderAdapter : IRefreshInfoProviderAdapter
	{
		RefreshPullDirection refreshPullDirection;
		InteractionTracker interactionTracker;
		VisualInteractionSource visualInteractionSource;
		IAdapterAnimationHandler animationHandler;
		Image image;
		RefreshInfoProviderImplementation infoProvider;

		public ImageIRefreshInfoProviderAdapter(RefreshPullDirection refreshPullDirection, IAdapterAnimationHandler animationHandler)
		{
			this.refreshPullDirection = refreshPullDirection;

			if (animationHandler != null)
			{
				this.animationHandler = animationHandler;
			}
			else
			{
				this.animationHandler = new AnimationHandler(null, refreshPullDirection);
			}
		}

		public IRefreshInfoProvider AdaptFromTree(UIElement root, Size visualizerSize)
		{
			int depth = 0;
			if (root is Image)
			{
				return Adapt((Image)root, visualizerSize);
			}
			else
			{
				while (depth < 10)
				{
					Image result = AdaptFromTreeRecursiveHelper(root, depth);
					if (result != null)
					{
						return Adapt(result, visualizerSize);
					}
					depth++;
				}
			}
			return null;
		}

		public void SetAnimations(UIElement refreshVisualizerAnimatableContainer)
		{
			animationHandler.InteractionTrackerAnimation(refreshVisualizerAnimatableContainer, image, interactionTracker);
		}

		public IRefreshInfoProvider Adapt(Image image, Size visualizerSize)
		{
			this.image = image;
			UIElement parent = (UIElement)VisualTreeHelper.GetParent(image);

			Visual parentVisual = ElementCompositionPreview.GetElementVisual(parent);
			Compositor compositor = parentVisual.Compositor;
			infoProvider = new RefreshInfoProviderImplementation(refreshPullDirection, visualizerSize, compositor);

			interactionTracker = InteractionTracker.CreateWithOwner(compositor, infoProvider);
			interactionTracker.MaxPosition = new Vector3(0.0f);
			interactionTracker.MinPosition = new Vector3(0.0f);
			interactionTracker.MaxScale = 1.0f;
			interactionTracker.MinScale = 1.0f;

			visualInteractionSource = VisualInteractionSource.Create(parentVisual);
			visualInteractionSource.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
			visualInteractionSource.ScaleSourceMode = InteractionSourceMode.Disabled;
			visualInteractionSource.PositionXSourceMode = IsOrientationVertical() ? InteractionSourceMode.Disabled : InteractionSourceMode.EnabledWithInertia;
			visualInteractionSource.PositionXChainingMode = IsOrientationVertical() ? InteractionChainingMode.Auto : InteractionChainingMode.Never;
			visualInteractionSource.PositionYSourceMode = IsOrientationVertical() ? InteractionSourceMode.EnabledWithInertia : InteractionSourceMode.Disabled;
			visualInteractionSource.PositionYChainingMode = IsOrientationVertical() ? InteractionChainingMode.Never : InteractionChainingMode.Auto;

			interactionTracker.InteractionSources.Add(visualInteractionSource);

			this.image.PointerPressed += OnImagePointerPressed;
			infoProvider.RefreshStarted += InfoProvider_RefreshStarted;
			infoProvider.RefreshCompleted += InfoProvider_RefreshCompleted;

			return infoProvider;
		}

		private void InfoProvider_RefreshCompleted(object sender, object e)
		{
			animationHandler.RefreshCompletedAnimation(null, image);
		}

		private void InfoProvider_RefreshStarted(object sender, object e)
		{
			image.CancelDirectManipulations();

			double executionRatio = 0.8f;
			if (infoProvider != null)
			{
				executionRatio = infoProvider.ExecutionRatio;
			}

			animationHandler.RefreshRequestedAnimation(null, image, executionRatio);
		}

		private void OnImagePointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
#if !HAS_UNO_WINUI
				visualInteractionSource.TryRedirectForManipulation(e.GetCurrentPoint(null));
#endif
				infoProvider.UpdateIsInteractingForRefresh(true);
			}
		}

		private Image AdaptFromTreeRecursiveHelper(DependencyObject root, int depth)
		{
			int numChildren = VisualTreeHelper.GetChildrenCount(root);
			if (depth == 0)
			{
				for (int i = 0; i < numChildren; i++)
				{
					DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
					if (childObject is Image)
					{
						return (Image)childObject;
					}
				}
				return null;
			}
			else
			{
				for (int i = 0; i < numChildren; i++)
				{
					DependencyObject childObject = VisualTreeHelper.GetChild(root, i);
					Image recursiveResult = AdaptFromTreeRecursiveHelper(childObject, depth - 1);
					if (recursiveResult != null)
					{
						return recursiveResult;
					}
				}
				return null;
			}
		}

		private bool IsOrientationVertical()
		{
			return (refreshPullDirection == RefreshPullDirection.TopToBottom || refreshPullDirection == RefreshPullDirection.BottomToTop);
		}

		public void Dispose() { }
	}


	internal class RefreshInfoProviderImplementation : IRefreshInfoProvider, IInteractionTrackerOwner
	{
		double executionRatio = 0.8;
		RefreshPullDirection refreshPullDirection;
		Size refreshVisualizerSize;
		CompositionPropertySet compositionProperties;
		String interactionRatioName = "InteractionRatio";
		int interactionRatioChangedCount = 0;
		bool isInteractingForRefresh = false;

		public RefreshInfoProviderImplementation(RefreshPullDirection refreshPullDirection, Size refreshVisualizerSize, Compositor compositor)
		{
			this.refreshPullDirection = refreshPullDirection;
			this.refreshVisualizerSize = refreshVisualizerSize;
			compositionProperties = compositor.CreatePropertySet();
		}

		public void UpdateIsInteractingForRefresh(bool value)
		{
			if (value != isInteractingForRefresh)
			{
				isInteractingForRefresh = value;
				if (IsInteractingForRefreshChanged != null)
				{
					IsInteractingForRefreshChanged.Invoke(this, null);
				}
			}
		}

		public void OnRefreshStarted()
		{
			if (RefreshStarted != null)
			{
				RefreshStarted.Invoke(this, null);
			}
		}

		public void OnRefreshCompleted()
		{
			if (RefreshCompleted != null)
			{
				RefreshCompleted.Invoke(this, null);
			}
		}

		public double ExecutionRatio
		{
			get
			{
				return executionRatio;
			}

			set
			{
				executionRatio = value;
			}
		}

		public CompositionPropertySet CompositionProperties
		{
			get
			{
				return compositionProperties;
			}
		}

		public string InteractionRatioCompositionProperty
		{
			get
			{
				return interactionRatioName;
			}
		}

		public bool IsInteractingForRefresh
		{
			get
			{
				return isInteractingForRefresh;
			}
		}

		public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

		public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
		{
		}

		public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
		{
		}

		public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
		{
			UpdateIsInteractingForRefresh(false);
		}

		public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
		{
		}

		public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
		{
		}

		public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
		{
			switch (refreshPullDirection)
			{
				case RefreshPullDirection.TopToBottom:
					RaiseInteractionRatioChanged(refreshVisualizerSize.Height == 0 ? 1.0 : Math.Min(1.0, (double)-args.Position.Y / refreshVisualizerSize.Height));
					break;
				case RefreshPullDirection.BottomToTop:
					RaiseInteractionRatioChanged(refreshVisualizerSize.Height == 0 ? 1.0f : Math.Min(1.0, (double)args.Position.Y / refreshVisualizerSize.Height));
					break;
				case RefreshPullDirection.LeftToRight:
					RaiseInteractionRatioChanged(refreshVisualizerSize.Width == 0 ? 1.0f : Math.Min(1.0, (double)-args.Position.X / refreshVisualizerSize.Width));
					break;
				case RefreshPullDirection.RightToLeft:
					RaiseInteractionRatioChanged(refreshVisualizerSize.Width == 0 ? 1.0f : Math.Min(1.0, (double)args.Position.X / refreshVisualizerSize.Width));
					break;
			}
		}

		private void RaiseInteractionRatioChanged(double interactionRatio)
		{
			compositionProperties.InsertScalar(interactionRatioName, (float)(interactionRatio));

			if (interactionRatioChangedCount == 0 || interactionRatio == 0.0)
			{
				if (InteractionRatioChanged != null)
				{
					var e = PullToRefreshHelperTestApi.CreateRefreshInteractionRatioChangedEventArgsInstance(interactionRatio);
					InteractionRatioChanged.Invoke(this, e);
				}
				interactionRatioChangedCount++;
			}
			else if (interactionRatioChangedCount >= 5)
			{
				interactionRatioChangedCount = 0;
			}
			else
			{
				interactionRatioChangedCount++;
			}
		}
	}
}
#endif
