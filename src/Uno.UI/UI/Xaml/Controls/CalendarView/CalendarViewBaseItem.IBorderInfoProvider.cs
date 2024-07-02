using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class CalendarViewBaseItem : IBorderInfoProvider
{
	// DrawBackground			=> General background for all items
	// DrawControlBackground	=> Control.Background customized by the apps (can be customized in the element changing event)
	// DrawDensityBar			=> Not supported yet
	// DrawFocusBorder			=> Not supported yet
	// OR DrawBorder			=> Draws the border ...
	// DrawInnerBorder			=> The today / selected state

	Brush IBorderInfoProvider.Background
	{
		get
		{
			var background = Background;

			if (IsClear(background))
			{
				if (FindTodaySelectedBackgroundBrush() is { } todaySelectedBackground
					&& !IsClear(todaySelectedBackground))
				{
					background = todaySelectedBackground;
				}
				else if (FindSelectedBackgroundBrush() is { } selectedBackground
					&& !IsClear(selectedBackground))
				{
					background = selectedBackground;
				}
				else
				{
					background = GetItemBackgroundBrush();
				}
			}
			return background;
		}
	}

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush
	{
		get
		{
			var borderBrush = GetItemBorderBrush(forFocus: false);
			if (m_isToday && m_isSelected && GetItemInnerBorderBrush() is { } selectedBrush)
			{
				// We don't support inner border yet, so even if not optimal we just use it as border.
				borderBrush = selectedBrush;
			}
			return borderBrush;
		}
	}


	Thickness IBorderInfoProvider.BorderThickness => GetItemBorderThickness();

	CornerRadius IBorderInfoProvider.CornerRadius => GetItemCornerRadius();

#if UNO_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");

	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
