// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\Flyout_partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls;

partial class Flyout
{
	private protected override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		//TODO:MZ:Verify this is called.
		//base.OnPropertyChanged2(args);

		if (args.Property == ContentProperty)
		{
			var spPresenter = GetPresenter() as ContentControl;
			if (spPresenter is not null)
			{
				//TODO:MZ: Remove commented out if works ok
				//PropertyChangedParamsHelper.GetObjects(args, &spOldValue, &spNewValue));

				// Apply the Content property change with new value if the content is really changed.
				bool areEqual = false;
				//PropertyValue::AreEqual(spOldValue.Get(), spNewValue.Get(), &areEqual));
				areEqual = args.OldValue == args.NewValue;
				if (!areEqual)
				{
					spPresenter.Content = args.NewValue;
				}
			}
		}
		else if (args.Property == FlyoutPresenterStyleProperty)
		{
			if (GetPresenter() is not null)
			{
				//TODO:MZ: Remove commented out if works ok
				//IFC(PropertyChangedParamsHelper::GetObjects(args, &spOldValue, &spNewValue));

				// Apply the PresenterStyle property change with new value if the PresenterStyle is really changed.
				bool areEqual = false;
				//IFC(PropertyValue::AreEqual(spOldValue.Get(), spNewValue.Get(), &areEqual));
				areEqual = args.OldValue == args.NewValue;
				if (!areEqual)
				{
					Style? spNewStyle = (Style)args.NewValue;

					SetPresenterStyle(GetPresenter(), spNewStyle);
				}
			}
		}
	}

	protected override Control CreatePresenter()
	{
		var presenter = new FlyoutPresenter();

		var content = Content;
		presenter.Content = content;

		var style = FlyoutPresenterStyle;
		SetPresenterStyle(presenter, style);

		return presenter;
	}

	private void OnProcessKeyboardAcceleratorsImpl(ProcessKeyboardAcceleratorEventArgs pArgs)
	{
		var spContentInterface = Content;
		if (spContentInterface is not null)
		{
			//TODO:MZ: Implement keyboard accelerator handling
			spContentInterface.TryInvokeKeyboardAccelerator(pArgs);
		}
	}

}
