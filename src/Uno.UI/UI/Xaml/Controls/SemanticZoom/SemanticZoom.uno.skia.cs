// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	private readonly SerialDisposable m_sizeChangedToken = new();
	private readonly SerialDisposable m_zoomedInViewSizeChangedToken = new();
	private readonly SerialDisposable m_zoomedOutViewSizeChangedToken = new();
	private readonly SerialDisposable m_elementZoomOutButtonClickToken = new();
	private readonly SerialDisposable m_templateSubscriptions = new();
	private readonly SerialDisposable m_lifecycleSubscriptions = new();

	private void InitializeManagedLifecycle()
	{
		var subscriptions = new CompositeDisposable();

		Loaded += OnSemanticZoomLoaded;
		subscriptions.Add(Disposable.Create(() => Loaded -= OnSemanticZoomLoaded));

		Unloaded += OnSemanticZoomUnloaded;
		subscriptions.Add(Disposable.Create(() => Unloaded -= OnSemanticZoomUnloaded));

		m_lifecycleSubscriptions.Disposable = subscriptions;
	}

	private void OnSemanticZoomLoaded(object sender, RoutedEventArgs e)
	{
		if (m_tpScrollViewer is { } scrollViewer)
		{
			scrollViewer.SetDirectManipulationStateChangeHandler(this);
			ToggleBackKeyListener(!IsZoomedInViewActive);
		}

		EnterImpl(isLive: true, skipNameRegistration: false, coercedIsEnabled: IsEnabled, useLayoutRounding: UseLayoutRounding);
	}

	private void OnSemanticZoomUnloaded(object sender, RoutedEventArgs e)
	{
		ToggleBackKeyListener(false);

		if (m_tpScrollViewer is { } scrollViewer)
		{
			scrollViewer.SetDirectManipulationStateChangeHandler(null);
		}

		m_tpAlternateViewTimer?.Stop();
		ClearCompletedEventArgs();
	}

	private void CleanupTemplateSubscriptions(bool clearParts)
	{
		m_templateSubscriptions.Disposable = null;
		m_sizeChangedToken.Disposable = null;
		m_zoomedInViewSizeChangedToken.Disposable = null;
		m_zoomedOutViewSizeChangedToken.Disposable = null;
		m_elementZoomOutButtonClickToken.Disposable = null;

		if (m_tpScrollViewer is { } scrollViewer)
		{
			scrollViewer.ArePointerWheelEventsIgnored = false;
			scrollViewer.SetDirectManipulationStateChangeHandler(null);
		}

		m_tpAlternateViewTimer?.Stop();
		m_tpAlternateViewTimer = null;
		ClearCompletedEventArgs();

		if (clearParts)
		{
			m_tpScrollViewer = null;
			m_tpZoomOutButton = null;
			m_tpZoomedInPresenterPart = null;
			m_tpZoomedOutPresenterPart = null;
			m_tpZoomedInTransform = null;
			m_tpZoomedOutTransform = null;
			m_tpManipulatedElementTransform = null;
		}
	}
}
