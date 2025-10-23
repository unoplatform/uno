// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedIcon.cpp, commit 893116c

using System.Collections.Generic;
using Uno.Disposables;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIcon
	{
		private IAnimatedVisual m_animatedVisual;
		private Panel m_rootPanel;
		private ScaleTransform m_scaleTransform;

		private string m_currentState = "";
		private string m_previousState = "";
		private Queue<string> m_queuedStates = new Queue<string>();
		private int m_queueLength = 2;
		private string m_pendingState = "";
		private string m_lastAnimationSegment = "";
		private string m_lastAnimationSegmentStart = "";
		private string m_lastAnimationSegmentEnd = "";
		private bool m_isPlaying = false;
		private bool m_canDisplayPrimaryContent = true;
		private float m_previousSegmentLength = 1.0f;
		private float m_durationMultiplier = 1.0f;
		private float m_speedUpMultiplier = 7.0f;
		private bool m_isSpeedUp = false;

		private CompositionPropertySet m_progressPropertySet = null;
		private CompositionScopedBatch m_batch = null;
		private SerialDisposable m_batchCompletedRevoker = new SerialDisposable();
		private SerialDisposable m_ancestorStatePropertyChangedRevoker = new SerialDisposable();
		private SerialDisposable m_layoutUpdatedRevoker = new SerialDisposable();
		private SerialDisposable m_foregroundColorPropertyChangedRevoker = new SerialDisposable();

		private AnimatedIconAnimationQueueBehavior m_queueBehavior = AnimatedIconAnimationQueueBehavior.QueueOne;
	}
}
