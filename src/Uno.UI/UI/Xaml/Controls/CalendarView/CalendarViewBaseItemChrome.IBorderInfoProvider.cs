using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class CalendarViewBaseItem : IBorderInfoProvider
{
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
}
