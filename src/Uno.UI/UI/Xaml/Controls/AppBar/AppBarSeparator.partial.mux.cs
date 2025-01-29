// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\AppBarSeparator_Partial.cpp, tag winui3/release/1.6.4, commit 262a901e09

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

public partial class AppBarSeparator : Control, ICommandBarElement, ICommandBarElement2, ICommandBarElement3, ICommandBarOverflowElement
{
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == IsCompactProperty ||
			args.Property == UseOverflowStyleProperty)
		{
			UpdateVisualState();
		}
	}

	// After template is applied, set the initial view state
	// (FullSize or Compact) based on the value of our
	// IsCompact property
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		UpdateVisualState();
	}

	// Sets the visual state to "Compact" or "FullSize" based on the value
	// of our IsCompact property
	private protected override void ChangeVisualState(bool useTransitions)
	{
		base.ChangeVisualState(useTransitions);

		var useOverflowStyle = UseOverflowStyle;
		var isCompact = IsCompact;

		if (useOverflowStyle)
		{
			GoToState(useTransitions, "Overflow");
		}
		else if (isCompact)
		{
			GoToState(useTransitions, "Compact");
		}
		else
		{
			GoToState(useTransitions, "FullSize");
		}
	}

	private protected override void OnVisibilityChanged() =>
		CommandBar.OnCommandBarElementVisibilityChanged(this);
}
