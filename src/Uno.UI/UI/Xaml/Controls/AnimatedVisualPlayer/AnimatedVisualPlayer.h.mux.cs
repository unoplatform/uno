// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.h, commit 3cae15f0

#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	// using AnimatedVisualPlayerProperties::AnimationOptimization;
	// friend class AnimatedVisualPlayerProperties;

	//
	// An awaitable object that is completed when an animation play is completed.
	//
	private sealed partial class AnimationPlay
	{
		private AnimatedVisualPlayer? m_owner;
		private readonly float m_fromProgress;
		private readonly float m_toProgress;
		private readonly bool m_looped;

		private readonly TaskCompletionSource<object?> m_taskCompletionSource = new();
		private AnimationController? m_controller;
		private bool m_isPaused;
		private bool m_isPausedBecauseHidden;
		private TypedEventHandler<object, CompositionBatchCompletedEventArgs>? m_batchCompletedHandler;
		private CompositionScopedBatch? m_batch;
	}

	private readonly UIElementCollection m_fallbackContentChildren;
	UIElementCollection IPanel.Children => m_fallbackContentChildren;

	//
	// Initialized by the constructor.
	//
	// A Visual used for clipping and for parenting of m_animatedVisualRoot.
	private SpriteVisual m_rootVisual = null!;
	// The property set that contains the Progress property that will be used to
	// set the progress of the animated visual.
	private CompositionPropertySet m_progressPropertySet = null!;
	// Revokers for events that we are subscribed to.
	private readonly SerialDisposable m_suspendingRevoker = new();
	private readonly SerialDisposable m_resumingRevoker = new();
	private readonly SerialDisposable m_xamlRootChangedRevoker = new();
	private readonly SerialDisposable m_loadedRevoker = new();
	private readonly SerialDisposable m_unloadedRevoker = new();

	//
	// Player mutable state state.
	//
	private IAnimatedVisual? m_animatedVisual;
	// The native size of the current animated visual. Only valid if m_animatedVisual is not nullptr.
	private Vector2 m_animatedVisualSize;
	private Visual? m_animatedVisualRoot;
	private int m_playAsyncVersion;
	private double m_currentPlayFromProgress = 0;
	// The play that will be stopped when Stop() is called.
	private AnimationPlay? m_nowPlaying;
	private readonly SerialDisposable m_dynamicAnimatedVisualInvalidatedRevoker = new();

	// Set true if an animated visual has failed to load and set false the next time an animated
	// visual loads with non-null content. When this is true the fallback content (if any) will
	// be displayed.
	private bool m_isFallenBack;

	// Set true when FrameworkElement::Unloaded is fired, then set false when FrameworkElement::Loaded is fired.
	// This is used to differentiate the first Loaded event (when the element has never been
	// unloaded) from later Loaded events.
	private bool m_isUnloaded;
	private bool m_hasPendingContentUpdate;

	private bool m_isAnimationsCreated;
	private uint m_createAnimationsCounter;

	private bool m_isHostVisible;
}
