// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SliderAutomationPeer_Partial.cpp

using System;
using DirectUI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes Slider types to Microsoft UI Automation.
	/// </summary>
	public partial class SliderAutomationPeer : RangeBaseAutomationPeer
	{
		private readonly Slider _owner;

		/// <summary>
		/// Initializes a new instance of the SliderAutomationPeer class.
		/// </summary>
		/// <param name="owner">The Slider to create a peer for.</param>
		public SliderAutomationPeer(Slider owner) : base(owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}

		protected override string GetClassNameCore() => nameof(Slider);

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

		protected override Point GetClickablePointCore() => new Point(DoubleUtil.NaN, DoubleUtil.NaN);

		protected override AutomationOrientation GetOrientationCore()
		{
			var orientation = _owner.Orientation;
			if (orientation == Orientation.Horizontal)
			{
				return AutomationOrientation.Horizontal;
			}
			else
			{
				return AutomationOrientation.Vertical;
			}
		}

		private protected override bool ChildIsAcceptable(UIElement element)
		{
			var childIsAcceptable = base.ChildIsAcceptable(element);

			if (childIsAcceptable)
			{
				var elementHorizontalTemplate = _owner.ElementHorizontalTemplate;
				var elementVerticalTemplate = _owner.ElementVerticalTemplate;

				if (element == elementHorizontalTemplate || element == elementVerticalTemplate)
				{
					var visibility = element.Visibility;
					childIsAcceptable = visibility == Visibility.Visible;
				}
			}

			return childIsAcceptable;
		}
	}
}
