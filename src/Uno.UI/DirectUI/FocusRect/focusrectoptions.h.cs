// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DirectUI
{
	internal struct FocusRectangleOptions
	{
		// Used to set the colors of the two alternating brushes
		// When isContinuous==true, the second rect is drawn inside the first
		// When isContinuous==false, the first and second are drawn together as a dotted line
		public SolidColorBrush firstBrush;
		public SolidColorBrush secondBrush;

		// Used to disable/enable the drawing of the two different lines
		public bool drawFirst;
		public bool drawSecond;
		public bool drawReveal;

		// Used to indicate whether this is a continuous focus rectangle (not dashed)
		// When true, the second rect is drawn inside the first
		// When false, the rects are drawn as alternating dotted lines
		//
		// IMPORTANT UNO: MS recommends to not support dashed focus visual! Only "high visibility focus rects"
		// (solid lines that are thicker and have two colors to show up on any background/theme).
		//
		// public bool isContinuous;

		// Used to indicate if we are drawing a borderless reveal glow
		public bool isRevealBorderless;

		public uint revealColor;

		// Bounds of the focus rect.  If uninitialized, we'll just use element's bounds
		public Rect bounds;

		// Previous bounds, used for animating reveal focus visual
		public Rect previousBounds;

		// Determines the thickness of the focus rectangle
		// When !isContinuous, only fistThickness is honored
		public Thickness firstThickness;
		public Thickness secondThickness;

		//void UseElementBounds(CUIElement element);
	};

}
