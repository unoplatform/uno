// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.cpp

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class FontIcon
{
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		if (@params.IsLive)
		{
			ApplyScaleTransformForFlowDirection();
		}
	}

	private protected override void ApplyTemplate(out bool fAddedVisuals)
	{
		fAddedVisuals = false;

		if (GetChildElementNoRef() is null)
		{
			// Create a new TextBlock
			var pTextBlock = new TextBlock();
			AddChildElement(pTextBlock);

			// Set the TextBlock's style to null so an implicit style doesn't affect it.
			pTextBlock.Style = null;

			// Set the TextBlock properties that aren't linked to our properties
			pTextBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
			pTextBlock.VerticalAlignment = VerticalAlignment.Center;
			pTextBlock.TextAlignment = TextAlignment.Center;
			pTextBlock.IsTextScaleFactorEnabled = false;

			// Set our glyph string as the text of the TextBlock
			UpdateLinkedChildProperty(GlyphProperty, TextBlock.TextProperty);

#if HAS_UNO
			// TODO Uno specific: WinUI's TextBlock pulls these from the icon's text formatting, Uno pushes them.
			UpdateLinkedChildProperty(FontFamilyProperty, TextBlock.FontFamilyProperty);
			UpdateLinkedChildProperty(FontSizeProperty, TextBlock.FontSizeProperty);
			UpdateLinkedChildProperty(FontStyleProperty, TextBlock.FontStyleProperty);
			UpdateLinkedChildProperty(FontWeightProperty, TextBlock.FontWeightProperty);
			UpdateLinkedChildProperty(ForegroundProperty, TextBlock.ForegroundProperty);
#endif

			// Set the TextBlock's attached AccessibilityView property to "Raw" so that its text
			// won't be picked up by automation/accessibility tools
			AutomationProperties.SetAccessibilityView(pTextBlock, AccessibilityView.Raw);

			fAddedVisuals = true;

			// Apply a scale transform in x axis for flipping the visuals if flowdirection is set to RTL.
			ApplyScaleTransformForFlowDirection();
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// The FontIcon_Glyph and FontIcon_MirroredWhenRightToLeft cases live in their property-changed callbacks.
		if (args.Property == FlowDirectionProperty)
		{
			ApplyScaleTransformForFlowDirection();
		}
	}

	private void ApplyScaleTransformForFlowDirection()
	{
		var mirroredWhenRightToLeft = MirroredWhenRightToLeft;

		if (m_pScaleTransform is null && this.IsRightToLeft() && mirroredWhenRightToLeft)
		{
			// Initialize the scale transform that will be used for mirroring and the
			// render transform origin as center in order to have the icon mirrored in place.
			m_pScaleTransform = new ScaleTransform();
			RenderTransformOrigin = new Point(0.5, 0.5);
			RenderTransform = m_pScaleTransform;
		}

		if (m_pScaleTransform is not null)
		{
			// Scale to 1 or -1 on X axis only if the RenderTransfrom is created before.
			// Otherwise it means we have never switched to RightToLeft at any time, so do nothing.
			m_pScaleTransform.ScaleX = (this.IsRightToLeft() && mirroredWhenRightToLeft) ? -1.0 : 1.0;
		}
	}
}
