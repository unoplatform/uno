#if false // TODO Should no longer be needed
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls
{
	public delegate void ElementAnimationCompleted(ElementAnimator sender, UIElement element);

	public partial class ElementAnimator
	{
		private enum AnimationTrigger
		{
			Show,
			Hide,
			BoundsChange
		}

		private struct ElementInfo
		{
			public ElementInfo(
				UIElement element,
				AnimationTrigger trigger,
				AnimationContext context)
			{
				Debug.Assert(trigger != AnimationTrigger.BoundsChange);

				Element = element;
				Trigger = trigger;
				Context = context;
				OldBounds = default;
				NewBounds = default;
			}

			public ElementInfo(
				UIElement element,
				AnimationTrigger trigger,
				AnimationContext context,
				Rect oldBounds,
				Rect newBounds)
			{
				Element = element;
				Trigger = trigger;
				Context = context;
				OldBounds = oldBounds;
				NewBounds = newBounds;
			}

			public UIElement Element { get; }
			public AnimationTrigger Trigger { get; }
			public AnimationContext Context { get; }
			public Rect OldBounds { get; }
			public Rect NewBounds { get; }
		}

		public event ElementAnimationCompleted BoundsChangeAnimationCompleted;
		public event ElementAnimationCompleted HideAnimationCompleted;
		public event ElementAnimationCompleted ShowAnimationCompleted;

		private readonly List<ElementInfo> m_animatingElements = new List<ElementInfo>();

		#region IElementAnimator
		public void OnElementShown(UIElement element, AnimationContext context)
		{
			if (HasShowAnimation(element, (AnimationContext)(context)))
			{
				HasShowAnimationsPending = true;
				SharedContext |= context;
				QueueElementForAnimation(new ElementInfo(
					element,
					AnimationTrigger.Show,
					context));
			}
		}

		public void OnElementHidden(UIElement element, AnimationContext context)
		{
			if (HasHideAnimation(element, (AnimationContext)(context)))
			{
				HasHideAnimationsPending = true;
				SharedContext |= context;
				QueueElementForAnimation(new ElementInfo(
					element,
					AnimationTrigger.Hide,
					context));
			}
		}

		public void OnElementBoundsChanged(UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds)
		{
			if (HasBoundsChangeAnimation(element, (AnimationContext)(context), oldBounds, newBounds))
			{
				HasBoundsChangeAnimationsPending = true;
				SharedContext |= context;
				QueueElementForAnimation(new ElementInfo(
					element,
					AnimationTrigger.BoundsChange,
					context,
					oldBounds,
					newBounds));
			}
		}

		public bool HasShowAnimation(UIElement element, AnimationContext context)
			=> HasShowAnimationCore(element, context);

		public bool HasHideAnimation(UIElement element, AnimationContext context)
			=> HasHideAnimationCore(element, context);

		public bool HasBoundsChangeAnimation(UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds)
			=> HasBoundsChangeAnimationCore(element, context, oldBounds, newBounds);
		#endregion

		#region IElementAnimatorOverrides

		protected virtual bool HasShowAnimationCore(UIElement element, AnimationContext context)
			=> throw new NotImplementedException();

		protected virtual bool HasHideAnimationCore(UIElement element, AnimationContext context)
			=> throw new NotImplementedException();

		protected virtual bool HasBoundsChangeAnimationCore(UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds)
			=> throw new NotImplementedException();

		protected virtual void StartShowAnimation(UIElement element, AnimationContext context)
			=> throw new NotImplementedException();

		protected virtual void StartHideAnimation(UIElement element, AnimationContext context)
			=> throw new NotImplementedException();

		protected virtual void StartBoundsChangeAnimation(UIElement element, AnimationContext context, Rect oldBounds, Rect newBounds)
			=> throw new NotImplementedException();

		#endregion

		#region IElementAnimatorProtected

		protected bool HasShowAnimationsPending { get; private set; }

		protected bool HasHideAnimationsPending { get; private set; }

		protected bool HasBoundsChangeAnimationsPending { get; private set; }

		protected AnimationContext SharedContext { get; private set; }

		protected void OnShowAnimationCompleted(UIElement element)
			=> ShowAnimationCompleted?.Invoke(this, element);

		protected void OnHideAnimationCompleted(UIElement element)
			=> HideAnimationCompleted?.Invoke(this, element);

		protected void OnBoundsChangeAnimationCompleted(UIElement element)
			=> BoundsChangeAnimationCompleted?.Invoke(this, element);
		#endregion

		private void QueueElementForAnimation(ElementInfo elementInfo)
		{
			m_animatingElements.Add(elementInfo);
			if (m_animatingElements.Count == 1)
			{
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
			}
		}

		private void OnRendering(object snd, object args)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering;

			using var resetState = Disposable.Create(ResetState);

			foreach (var elementInfo in m_animatingElements)
			{
				switch (elementInfo.Trigger)
				{
					case AnimationTrigger.Show:
						// Call into the derivied class's StartShowAnimation override
						StartShowAnimation(elementInfo.Element, elementInfo.Context);
						break;
					case AnimationTrigger.Hide:
						// Call into the derivied class's StartHideAnimation override
						StartHideAnimation(elementInfo.Element, elementInfo.Context);
						break;
					case AnimationTrigger.BoundsChange:
						// Call into the derivied class's StartBoundsChangeAnimation override
						StartBoundsChangeAnimation(
							elementInfo.Element,
							elementInfo.Context,
							elementInfo.OldBounds,
							elementInfo.NewBounds);
						break;
				}
			}
		}

		private void ResetState()
		{
			m_animatingElements.Clear();
			HasShowAnimationsPending = HasHideAnimationsPending = HasBoundsChangeAnimationsPending = false;
			SharedContext = AnimationContext.None;
		}
	}
}
#endif