#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Internal;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichEditBox
	{
		private MenuFlyout? _proofingMenu;
		private PointerDeviceType _lastFlyoutInputDeviceType;
		private Point _lastPointerPositionForFlyout;
		private bool _isSelectionFlyoutUpdateQueued;
		private bool _forceFocusedVisualState;

		public event ContextMenuOpeningEventHandler? ContextMenuOpening;

		private FlyoutBase GetProofingMenuFlyout(int? position = null)
		{
			_proofingMenu ??= new MenuFlyout();
			TextControlFlyoutHelper.AddProofingFlyout(_proofingMenu, this);
			if (IsSpellCheckEnabled && (FocusState != FocusState.Unfocused || _forceFocusedVisualState))
			{
				UpdateProofingMenu(position ?? _selection.start);
			}

			SetValue(ProofingMenuFlyoutProperty, _proofingMenu);
			return _proofingMenu;
		}

		private void UpdateProofingMenu(int position)
		{
			if (_proofingMenu is null)
			{
				return;
			}

			_proofingMenu.Items.Clear();
			if (_textBoxView?.DisplayBlock.ParsedText is not UnicodeText unicodeText
				|| unicodeText.GetCorrectionAtIndex(position) is not { } correction
				|| unicodeText.GetSpellCheckSuggestions(correction.correctionStart, correction.correctionEnd) is not { } result)
			{
				return;
			}

			var count = Math.Min(result.suggestions.Count, 3);
			for (var i = 0; i < count; i++)
			{
				var suggestion = result.suggestions[i];
				var item = new MenuFlyoutItem { Text = suggestion };
				item.Click += (_, _) => ReplaceWithProofingSuggestion(result.replaceIndexStart, result.replaceIndexEnd, suggestion);
				_proofingMenu.Items.Add(item);
			}
		}

		private void ReplaceWithProofingSuggestion(int start, int end, string suggestion)
		{
			if (IsReadOnly)
			{
				return;
			}
			if (Document.IsRangeProtected(start, end))
			{
				return;
			}

			var insertedLength = Document.ReplaceRange(start, end, suggestion);
			SetInteractiveSelection(start + insertedLength, 0);
		}

		private void QueueUpdateSelectionFlyoutVisibility(PointerDeviceType deviceType, Point position)
		{
			_lastFlyoutInputDeviceType = deviceType;
			_lastPointerPositionForFlyout = position;
			if (!_isSelectionFlyoutUpdateQueued)
			{
				_isSelectionFlyoutUpdateQueued = true;
				DispatcherQueue.TryEnqueue(UpdateSelectionFlyoutVisibility);
			}
		}

		private void UpdateSelectionFlyoutVisibility()
		{
			_isSelectionFlyoutUpdateQueued = false;
			if (SelectionFlyout is not { } selectionFlyout || TextControlFlyoutHelper.IsOpen(ContextFlyout))
			{
				return;
			}

			var shouldShow = _selection.length > 0 && _lastFlyoutInputDeviceType is
				PointerDeviceType.Mouse or PointerDeviceType.Pen or PointerDeviceType.Touch;
			if (!shouldShow)
			{
				TextControlFlyoutHelper.CloseIfOpen(selectionFlyout);
				_lastFlyoutInputDeviceType = default;
				return;
			}

			var showMode = _lastFlyoutInputDeviceType == PointerDeviceType.Mouse
				? FlyoutShowMode.TransientWithDismissOnPointerMoveAway
				: FlyoutShowMode.Transient;
			Document.GetRange(_selection.start, _selection.start + _selection.length)
				.GetRect(global::Microsoft.UI.Text.PointOptions.ClientCoordinates, out var selectionRect, out _);
			var position = new Point(_lastPointerPositionForFlyout.X, selectionRect.Y);
			TextControlFlyoutHelper.ShowAt(selectionFlyout, this, position, selectionRect, showMode);
			_lastFlyoutInputDeviceType = default;
		}

		internal void ShowAccessibilityContextMenu(int start, int end)
		{
			if (!IsLoaded || Visibility != Visibility.Visible)
			{
				return;
			}

			var length = Document.TextLength;
			start = Math.Clamp(start, 0, length);
			end = Math.Clamp(end, start, length);
			Document.GetRange(start, end).GetRect(
				global::Microsoft.UI.Text.PointOptions.ClientCoordinates,
				out var rangeRect,
				out _);
			if (rangeRect.Height <= 0)
			{
				return;
			}

			FlyoutBase? flyout = ContextFlyout;
			if (flyout is null && start != end)
			{
				flyout = SelectionFlyout;
			}

			if (flyout is null
				&& GetProofingMenuFlyout(start) is MenuFlyout { Items.Count: > 0 } proofingMenu)
			{
				flyout = proofingMenu;
			}

			flyout ??= SelectionFlyout;
			if (flyout is null)
			{
				return;
			}

			var position = new Point(
				rangeRect.X + rangeRect.Width / 2,
				rangeRect.Y + rangeRect.Height);
			TextControlFlyoutHelper.ShowAt(
				flyout,
				this,
				position,
				rangeRect,
				FlyoutShowMode.Standard);
		}

		private void DismissSelectionFlyoutForPointerPress()
			=> TextControlFlyoutHelper.CloseIfOpen(SelectionFlyout);

		internal void DismissAllFlyouts()
		{
			TextControlFlyoutHelper.CloseIfOpen(_proofingMenu);
			TextControlFlyoutHelper.CloseIfOpen(SelectionFlyout);
			TextControlFlyoutHelper.CloseIfOpen(ContextFlyout);
		}

		private bool ShouldForceFocusedVisualState()
			=> TextControlFlyoutHelper.IsGettingFocus(SelectionFlyout, this)
				|| TextControlFlyoutHelper.IsGettingFocus(ContextFlyout, this);

		private bool ShouldHideGrippersOnFlyoutOpening()
			=> TextControlFlyoutHelper.IsGettingFocus(ContextFlyout, this);

		internal void ForceFocusLoss()
		{
			_forceFocusedVisualState = false;
			_textBoxView?.OnFocusStateChanged(FocusState.Unfocused);
			EndImeSession();
			StopCaret();
			UpdateSelectionHighlightColor();
			UpdateVisualState();
		}

		internal bool FireContextMenuOpeningEventSynchronously(Point point)
		{
			var rootPoint = TransformToVisual(null).TransformPoint(point);
			var args = new ContextMenuEventArgs(rootPoint.X, rootPoint.Y);
			ContextMenuOpening?.Invoke(this, args);
			return args.Handled;
		}
	}
}