#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
#if HAS_UNO
	private static void OnRichEditBoxPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((RichEditBox)sender).SetValueCore(args);

	private void SetAcceptsReturn(bool value)
	{
		if (value != AcceptsReturn)
		{
			InvalidatePendingInteractiveLineFeed();
			_textChangingInvalidatedLineFeed |= _isInvokingTextChanging;
		}
		SetValue(AcceptsReturnProperty, value);
	}

	private static void OnHorizontalTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		var value = (TextAlignment)args.NewValue;
		if (owner.TextAlignment != value)
		{
			owner.SetValue(TextAlignmentProperty, value);
		}

		owner._textBoxView?.SetTextAlignment();
		owner.DispatchUpdateScrolling();
	}

	private static void OnTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		var value = (TextAlignment)args.NewValue;
		if (owner.HorizontalTextAlignment != value)
		{
			owner.SetValue(HorizontalTextAlignmentProperty, value);
		}

		owner._textBoxView?.SetTextAlignment();
		owner.DispatchUpdateScrolling();
	}

	private static void OnIsSpellCheckEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		if (owner._textBoxView is { } view)
		{
			view.DisplayBlock.IsSpellCheckEnabled = (bool)args.NewValue;
			view.UpdateProperties();
		}
		Uno.Helpers.UIElementAccessibilityHelper.NotifyTextControlStateChanged(owner);
		if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)
			&& owner.GetOrCreateAutomationPeer() is RichEditBoxAutomationPeer peer)
		{
			peer.RaiseIsSpellCheckEnabledPropertyChangedEvent((bool)args.OldValue, (bool)args.NewValue);
		}
		if (!owner.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				(FrameworkElementAutomationPeer.FromElement(owner) as RichEditBoxAutomationPeer)?.OnDocumentAccessibilityChanged();
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
			{
				if (typeof(RichEditBox).Log().IsEnabled(LogLevel.Error))
				{
					typeof(RichEditBox).Log().Error("Failed to refresh RichEditBox accessibility state.", error);
				}
			}
		}) && typeof(RichEditBox).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(RichEditBox).Log().Warn("Failed to enqueue a RichEditBox accessibility refresh.");
		}
		ImeSessionCoordinator.UpdateSession(owner, ImeSessionUpdate.SpellCheck);
	}

	private static void OnAcceptsReturnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner.InvalidatePendingInteractiveLineFeed();
		owner._textBoxView?.UpdateProperties();
		ImeSessionCoordinator.UpdateSession(owner, ImeSessionUpdate.AcceptsReturn);
	}

	private static void OnIsColorFontEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner._textBoxView?.SetColorFontEnabled();
		owner.RenderDocument();
		owner.DispatchUpdateScrolling();
	}

	private static void OnDesiredCandidateWindowAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ImeSessionCoordinator.UpdateSession((RichEditBox)sender, ImeSessionUpdate.CandidateWindowAlignment);

	private static void OnIsTextPredictionEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner._textBoxView?.UpdateProperties();
		ImeSessionCoordinator.UpdateSession(owner, ImeSessionUpdate.TextPrediction);
	}

	private static void OnInputScopeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner._textBoxView?.UpdateProperties();
		ImeSessionCoordinator.UpdateSession(owner, ImeSessionUpdate.InputScope);
	}

	private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		var oldValue = (bool)args.OldValue;
		var newValue = (bool)args.NewValue;
		if (newValue)
		{
			owner.EndImeSession();
			owner.StopCaret();
		}
		else if (owner.FocusState != FocusState.Unfocused)
		{
			owner.ResumeCaret();
			owner.StartImeSession();
		}
		else
		{
			owner.UpdateDisplaySelection();
		}

		owner._textBoxView?.UpdateProperties();
		owner.UpdateVisualState();
		Uno.Helpers.UIElementAccessibilityHelper.NotifyTextControlStateChanged(owner);
		if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)
			&& owner.GetOrCreateAutomationPeer() is RichEditBoxAutomationPeer peer)
		{
			peer.RaiseIsReadOnlyPropertyChangedEvent(oldValue, newValue);
		}
	}

	private static void OnMaxLengthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner.InvalidatePendingInteractiveLineFeed();
		owner._textBoxView?.UpdateMaxLength();
	}

	private static object CoerceMaxLength(DependencyObject sender, object baseValue, DependencyPropertyValuePrecedences precedence)
	{
		ValidateSetValueArguments(MaxLengthProperty, baseValue);
		return baseValue;
	}

	private static void OnSelectionHighlightColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((RichEditBox)sender).UpdateSelectionHighlightColor();

	private static void OnTextReadingOrderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner._textBoxView?.SetReadingOrder();
		owner.RenderDocument();
		owner.DispatchUpdateScrolling();
	}

	private static void OnTextWrappingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RichEditBox)sender;
		owner._textBoxView?.SetWrapping();
		owner.UpdateTextWrappingScrollMode();
		owner.DispatchUpdateScrolling();
	}
#endif
}
