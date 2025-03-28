// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Common;
using Windows.UI.Composition.Interactions;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;

namespace MUXControlsTestApp.Utilities;

public class ExpressionAnimationStatusChange
{
	public ExpressionAnimationStatusChange(bool isExpressionAnimationStarted, string propertyName)
	{
		IsExpressionAnimationStarted = isExpressionAnimationStarted;
		PropertyName = propertyName;
	}

	public bool IsExpressionAnimationStarted { get; set; }

	public string PropertyName { get; set; }
}

// Utility class used to turn on ScrollPresenter test hooks and automatically turn them off when the instance gets disposed.
public class ScrollPresenterTestHooksHelper : IDisposable
{
	Dictionary<ScrollPresenter, CompositionInteractionSourceCollection> m_interactionSources = null;
	Dictionary<ScrollPresenter, List<ExpressionAnimationStatusChange>> m_expressionAnimationStatusChanges = null;

	public ScrollPresenterTestHooksHelper(
		bool enableAnchorNotifications = true,
		bool enableInteractionSourcesNotifications = true,
		bool enableExpressionAnimationStatusNotifications = true,
		bool? isAnimationsEnabledOverride = null)
	{
		RunOnUIThread.Execute(() =>
		{
			if (enableAnchorNotifications)
			{
				TurnOnAnchorNotifications();
			}

			if (enableInteractionSourcesNotifications)
			{
				TurnOnInteractionSourcesNotifications();
			}

			if (enableExpressionAnimationStatusNotifications)
			{
				TurnOnExpressionAnimationStatusNotifications();
			}

			if (isAnimationsEnabledOverride.HasValue)
			{
				SetIsAnimationsEnabledOverride(isAnimationsEnabledOverride.Value);
			}

			m_interactionSources = new Dictionary<ScrollPresenter, CompositionInteractionSourceCollection>();
			m_expressionAnimationStatusChanges = new Dictionary<ScrollPresenter, List<ExpressionAnimationStatusChange>>();
		});
	}

	public void TurnOnAnchorNotifications()
	{
		if (!ScrollPresenterTestHooks.AreAnchorNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning on anchor notifications.");
			ScrollPresenterTestHooks.AreAnchorNotificationsRaised = true;
			ScrollPresenterTestHooks.AnchorEvaluated += ScrollPresenterTestHooks_AnchorEvaluated;
		}
	}

	public void TurnOffAnchorNotifications()
	{
		if (ScrollPresenterTestHooks.AreAnchorNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning off anchor notifications.");
			ScrollPresenterTestHooks.AreAnchorNotificationsRaised = false;
			ScrollPresenterTestHooks.AnchorEvaluated -= ScrollPresenterTestHooks_AnchorEvaluated;
		}
	}

	public void TurnOnInteractionSourcesNotifications()
	{
		if (!ScrollPresenterTestHooks.AreInteractionSourcesNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning on InteractionSources notifications.");
			ScrollPresenterTestHooks.AreInteractionSourcesNotificationsRaised = true;
			ScrollPresenterTestHooks.InteractionSourcesChanged += ScrollPresenterTestHooks_InteractionSourcesChanged;
		}
	}

	public void TurnOffInteractionSourcesNotifications()
	{
		if (ScrollPresenterTestHooks.AreInteractionSourcesNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning off InteractionSources notifications.");
			ScrollPresenterTestHooks.AreInteractionSourcesNotificationsRaised = false;
			ScrollPresenterTestHooks.InteractionSourcesChanged -= ScrollPresenterTestHooks_InteractionSourcesChanged;
		}
	}

	public void TurnOnExpressionAnimationStatusNotifications()
	{
		if (!ScrollPresenterTestHooks.AreExpressionAnimationStatusNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning on ExpressionAnimation status notifications.");
			ScrollPresenterTestHooks.AreExpressionAnimationStatusNotificationsRaised = true;
			ScrollPresenterTestHooks.ExpressionAnimationStatusChanged += ScrollPresenterTestHooks_ExpressionAnimationStatusChanged;
		}
	}

	public void TurnOffExpressionAnimationStatusNotifications()
	{
		if (ScrollPresenterTestHooks.AreExpressionAnimationStatusNotificationsRaised)
		{
			Log.Comment("ScrollPresenterTestHooksHelper: Turning off ExpressionAnimation status notifications.");
			ScrollPresenterTestHooks.AreExpressionAnimationStatusNotificationsRaised = false;
			ScrollPresenterTestHooks.ExpressionAnimationStatusChanged -= ScrollPresenterTestHooks_ExpressionAnimationStatusChanged;
		}
	}

	public void SetIsAnimationsEnabledOverride(bool isAnimationsEnabledOverride)
	{
		if (!ScrollPresenterTestHooks.IsAnimationsEnabledOverride.HasValue || ScrollPresenterTestHooks.IsAnimationsEnabledOverride.Value != isAnimationsEnabledOverride)
		{
			Log.Comment($"ScrollPresenterTestHooksHelper: Setting IsAnimationsEnabledOverride to {isAnimationsEnabledOverride}.");
			ScrollPresenterTestHooks.IsAnimationsEnabledOverride = isAnimationsEnabledOverride;
		}
	}

	public void ResetIsAnimationsEnabledOverride()
	{
		if (ScrollPresenterTestHooks.IsAnimationsEnabledOverride.HasValue)
		{
			Log.Comment($"ScrollPresenterTestHooksHelper: Resetting IsAnimationsEnabledOverride from {ScrollPresenterTestHooks.IsAnimationsEnabledOverride.Value}.");
			ScrollPresenterTestHooks.IsAnimationsEnabledOverride = null;
		}
	}

	public void Dispose()
	{
		RunOnUIThread.Execute(() =>
		{
			TurnOffAnchorNotifications();
			TurnOffInteractionSourcesNotifications();
			TurnOffExpressionAnimationStatusNotifications();
			ResetIsAnimationsEnabledOverride();

			m_interactionSources.Clear();
			m_expressionAnimationStatusChanges.Clear();
		});
	}

	public CompositionInteractionSourceCollection GetInteractionSources(ScrollPresenter scrollPresenter)
	{
		if (m_interactionSources.ContainsKey(scrollPresenter))
		{
			return m_interactionSources[scrollPresenter];
		}
		else
		{
			return null;
		}
	}

	public List<ExpressionAnimationStatusChange> GetExpressionAnimationStatusChanges(ScrollPresenter scrollPresenter)
	{
		if (m_expressionAnimationStatusChanges.ContainsKey(scrollPresenter))
		{
			return m_expressionAnimationStatusChanges[scrollPresenter];
		}
		else
		{
			return null;
		}
	}

	public void ResetExpressionAnimationStatusChanges(ScrollPresenter scrollPresenter)
	{
		if (m_expressionAnimationStatusChanges.ContainsKey(scrollPresenter))
		{
			m_expressionAnimationStatusChanges.Remove(scrollPresenter);
		}
	}

	public static void LogInteractionSources(CompositionInteractionSourceCollection interactionSources)
	{
		if (interactionSources == null)
		{
			Log.Warning("LogInteractionSources: parameter interactionSources is null");
			throw new ArgumentNullException("interactionSources");
		}

		Log.Comment("    Interaction sources count: " + interactionSources.Count);

		foreach (ICompositionInteractionSource interactionSource in interactionSources)
		{
			VisualInteractionSource visualInteractionSource = interactionSource as VisualInteractionSource;
			if (visualInteractionSource != null)
			{
				Log.Comment("    VisualInteractionSource: ManipulationRedirectionMode: " + visualInteractionSource.ManipulationRedirectionMode);
				//Log.Comment("    VisualInteractionSource: IsPositionXRailsEnabled: " + visualInteractionSource.IsPositionXRailsEnabled +
				//	", IsPositionYRailsEnabled:" + visualInteractionSource.IsPositionYRailsEnabled);
				//Log.Comment("    VisualInteractionSource: PositionXChainingMode: " + visualInteractionSource.PositionXChainingMode +
				//	", PositionYChainingMode:" + visualInteractionSource.PositionYChainingMode +
				//	", ScaleChainingMode:" + visualInteractionSource.ScaleChainingMode);
				Log.Comment("    VisualInteractionSource: PositionXSourceMode: " + visualInteractionSource.PositionXSourceMode +
					", PositionYSourceMode:" + visualInteractionSource.PositionYSourceMode +
					", ScaleSourceMode:" + visualInteractionSource.ScaleSourceMode);
				//Log.Comment("    VisualInteractionSource: PointerWheelConfig: (" + visualInteractionSource.PointerWheelConfig.PositionXSourceMode +
				//	", " + visualInteractionSource.PointerWheelConfig.PositionYSourceMode +
				//	", " + visualInteractionSource.PointerWheelConfig.ScaleSourceMode + ")");
			}
		}
	}

	public static void LogExpressionAnimationStatusChanges(List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges)
	{
		if (expressionAnimationStatusChanges == null)
		{
			Log.Comment("expressionAnimationStatusChanges is null");
			return;
		}

		Log.Comment("expressionAnimationStatusChanges:");
		foreach (ExpressionAnimationStatusChange expressionAnimationStatusChange in expressionAnimationStatusChanges)
		{
			Log.Comment($"  IsExpressionAnimationStarted: {expressionAnimationStatusChange.IsExpressionAnimationStarted}, PropertyName: {expressionAnimationStatusChange.PropertyName}");
		}
	}

	private void ScrollPresenterTestHooks_AnchorEvaluated(ScrollPresenter sender, ScrollPresenterTestHooksAnchorEvaluatedEventArgs args)
	{
		string anchorName = (args.AnchorElement is FrameworkElement) ? (args.AnchorElement as FrameworkElement).Name : string.Empty;

		Log.Comment("  AnchorEvaluated: s:" + sender.Name + ", a:" + anchorName + ", ap:(" + args.ViewportAnchorPointHorizontalOffset + "," + args.ViewportAnchorPointVerticalOffset + ")");
	}

	private void ScrollPresenterTestHooks_InteractionSourcesChanged(ScrollPresenter sender, ScrollPresenterTestHooksInteractionSourcesChangedEventArgs args)
	{
		Log.Comment("  InteractionSourcesChanged: s: " + sender.Name);
		if (!m_interactionSources.ContainsKey(sender))
		{
			m_interactionSources.Add(sender, args.InteractionSources);
		}
		else
		{
			m_interactionSources[sender] = args.InteractionSources;
		}
		LogInteractionSources(args.InteractionSources);
	}

	private void ScrollPresenterTestHooks_ExpressionAnimationStatusChanged(ScrollPresenter sender, ScrollPresenterTestHooksExpressionAnimationStatusChangedEventArgs args)
	{
		Log.Comment($"  ExpressionAnimationStatusChanged: s: {sender.Name}, IsExpressionAnimationStarted: {args.IsExpressionAnimationStarted}, PropertyName: {args.PropertyName}");
		List<ExpressionAnimationStatusChange> expressionAnimationStatusChanges = null;

		if (!m_expressionAnimationStatusChanges.ContainsKey(sender))
		{
			expressionAnimationStatusChanges = new List<ExpressionAnimationStatusChange>();
			m_expressionAnimationStatusChanges.Add(sender, expressionAnimationStatusChanges);
		}
		else
		{
			expressionAnimationStatusChanges = m_expressionAnimationStatusChanges[sender];
		}

		expressionAnimationStatusChanges.Add(new ExpressionAnimationStatusChange(args.IsExpressionAnimationStarted, args.PropertyName));
	}
}
