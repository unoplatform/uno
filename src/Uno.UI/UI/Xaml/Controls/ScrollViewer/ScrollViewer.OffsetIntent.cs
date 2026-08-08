#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollViewer
{
	private double? _verticalOffsetIntent;
	private double? _horizontalOffsetIntent;
	private bool _isInternalOffsetAdjustment;
	private double _lastTrimViewportHeight;
	private double _lastTrimViewportWidth;

	internal void ClearOffsetIntents()
	{
		_verticalOffsetIntent = null;
		_horizontalOffsetIntent = null;
	}

	internal void SetVerticalOffsetIntent(double offset) => _verticalOffsetIntent = offset;

	internal void SetHorizontalOffsetIntent(double offset) => _horizontalOffsetIntent = offset;

	private bool RecomputeOffsetsFromIntent()
	{
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
		if ((_presenter as ScrollContentPresenter)?.IsScrollAnimationInProgress == true)
		{
			return false;
		}
#endif

		var changed = false;

		if (_verticalOffsetIntent is double verticalIntent)
		{
			var clamped = Math.Max(0, Math.Min(verticalIntent, ScrollableHeight));
			if (Math.Abs(VerticalOffset - clamped) > 0.5)
			{
				ChangeViewInternal(null, clamped);
				changed = true;
			}
		}

		if (_horizontalOffsetIntent is double horizontalIntent)
		{
			var clamped = Math.Max(0, Math.Min(horizontalIntent, ScrollableWidth));
			if (Math.Abs(HorizontalOffset - clamped) > 0.5)
			{
				ChangeViewInternal(clamped, null);
				changed = true;
			}
		}

		return changed;
	}

	private void ChangeViewInternal(double? horizontalOffset, double? verticalOffset)
	{
		var wasInternal = _isInternalOffsetAdjustment;
		_isInternalOffsetAdjustment = true;
		try
		{
			ChangeView(horizontalOffset, verticalOffset, null, disableAnimation: true);
		}
		finally
		{
			_isInternalOffsetAdjustment = wasInternal;
		}
	}
}
