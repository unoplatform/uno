using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.Disposables;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	internal partial class AnimationPlay : TaskCompletionSource
	{
		private AnimatedVisualPlayer m_owner;
		private readonly float m_fromProgress;
		private readonly float m_toProgress;
		private readonly bool m_looped;
		private TimeSpan m_playDuration;

		private AnimationController m_controller;
		private bool m_isPaused;
		private bool m_isPausedBecauseHidden;
		private long m_batchCompletedToken;
		CompositionScopedBatch m_batch;
	}

	//
	// Initialized by the constructor.
	//
	// A Visual used for clipping and for parenting of m_animatedVisualRoot.
	private SpriteVisual m_rootVisual;
	// The property set that contains the Progress property that will be used to
	// set the progress of the animated visual.
	private CompositionPropertySet m_progressPropertySet;
	// Revokers for events that we are subscribed to.
	private SerialDisposable m_suspendingRevoker = new();
	private SerialDisposable m_resumingRevoker = new();
	private SerialDisposable m_xamlRootChangedRevoker = new();
	private SerialDisposable m_loadedRevoker = new();
	private SerialDisposable m_unloadedRevoker = new();

	//
	// Player mutable state state.
	//
	private IAnimatedVisual m_animatedVisual;
	// The native size of the current animated visual. Only valid if m_animatedVisual is not nullptr.
	private Vector2 m_animatedVisualSize;
	private Visual m_animatedVisualRoot;
	private int m_playAsyncVersion;
	private double m_currentPlayFromProgress;
	// The play that will be stopped when Stop() is called.
	private AnimationPlay m_nowPlaying;
	private SerialDisposable m_dynamicAnimatedVisualInvalidatedRevoker;

	// Set true if an animated visual has failed to load and set false the next time an animated
	// visual loads with non-null content. When this is true the fallback content (if any) will
	// be displayed.
	private bool m_isFallenBack;

	// Set true when FrameworkElement::Unloaded is fired, then set false when FrameworkElement::Loaded is fired.
	// This is used to differentiate the first Loaded event (when the element has never been
	// unloaded) from later Loaded events.
	private bool m_isUnloaded;

	private bool m_isAnimationsCreated;
	private uint m_createAnimationsCounter = 0;

	private bool m_isHostVisible;
}
