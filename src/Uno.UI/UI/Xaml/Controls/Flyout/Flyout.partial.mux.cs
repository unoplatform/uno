// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Flyout_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

partial class Flyout
{
	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == ContentProperty)
		{
			if (GetPresenter() is ContentControl spPresenter)
			{
				// Apply the Content property change with new value if the content is really changed.
				if (!Equals(args.OldValue, args.NewValue))
				{
					spPresenter.Content = args.NewValue;
				}
			}

#if HAS_UNO
			SynchronizeNamescope();

			if (args.OldValue is DependencyObject oldDO)
			{
				oldDO.ClearValue(NameScope.NameScopeProperty);
			}
#endif
		}
		else if (args.Property == FlyoutPresenterStyleProperty)
		{
			if (GetPresenter() is { } presenter)
			{
				// Apply the PresenterStyle property change with new value if the PresenterStyle is really changed.
				if (!Equals(args.OldValue, args.NewValue))
				{
					SetPresenterStyle(presenter, args.NewValue as Style);
				}
			}
		}
	}

	protected override Control CreatePresenter()
	{
		FlyoutPresenter spPresenter = new();

		spPresenter.Content = Content;

		SetPresenterStyle(spPresenter, FlyoutPresenterStyle);

#if HAS_UNO
		SynchronizeNamescope();
#endif

		return spPresenter;
	}

	protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
		if (Content is not null)
		{
			Content.TryInvokeKeyboardAccelerator(args);
		}
	}

#if HAS_UNO
	// Uno resolves x:Name through the namescope of the content, whereas WinUI's flyout enters the namescope of its owner.
	private void SynchronizeNamescope()
	{
		if (NameScope.GetNameScope(this) is { } scope && Content is DependencyObject content)
		{
			NameScope.SetNameScope(content, scope);
		}
	}
#endif
}
