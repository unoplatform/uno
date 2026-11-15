// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DatePickerFlyoutItem_Partial.cpp, commit d505d1ddf

using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class DatePickerFlyoutItem : DependencyObject
	{
		internal DatePickerFlyoutItem()
		{
		}

		//void InitializeImpl()
		//{
		//	//wrl.ComPtr<xaml.IDependencyObjectFactory> spInnerFactory;
		//	//wrl.ComPtr<xaml.IDependencyObject> spInnerInstance;
		//	//wrl.ComPtr<DependencyObject> spInnerInspectable;

		//	//DatePickerFlyoutItemGenerated.InitializeImpl();
		//	(wf.GetActivationFactory(
		//		wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_DependencyObject),
		//		&spInnerFactory));

		//	(spInnerFactory.CreateInstance(
		//		(IDatePickerFlyoutItem)(this),
		//		&spInnerInspectable,
		//		&spInnerInstance));

		//	(SetComposableBasePointers(
		//		spInnerInspectable,
		//		spInnerFactory));
		//}

		public ICustomProperty GetCustomProperty(string name)
		{
			//UNREFERENCED_PARAMETER(name);
			//ARG_VALIDRETURNPOINTER(returnValue);
			return null;
		}

		public ICustomProperty GetIndexedProperty(string name, Type type)
		{
			//UNREFERENCED_PARAMETER(name);
			//UNREFERENCED_PARAMETER(type);
			//ARG_VALIDRETURNPOINTER(returnValue);
			return null;
		}

		public string GetStringRepresentation()
		{
			string primaryText;
			string secondaryText;
			string outputText;

			primaryText = PrimaryText;
			secondaryText = SecondaryText;
			if (primaryText != null && secondaryText != null)
			{
				outputText = string.Concat(primaryText, " ", secondaryText);
			}
			else if (primaryText != null)
			{
				outputText = primaryText;
			}
			else if (secondaryText != null)
			{
				outputText = secondaryText;
			}
			else
			{
				outputText = "";
			}

			return outputText;
		}

		// WinUI reports TypeKind_Primitive with the runtime class name.
		public Type Type => typeof(DatePickerFlyoutItem);

		public string SecondaryText
		{
			get => (string)GetValue(SecondaryTextProperty);
			set => SetValue(SecondaryTextProperty, value);
		}

		public string PrimaryText
		{
			get => (string)GetValue(PrimaryTextProperty);
			set => SetValue(PrimaryTextProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty PrimaryTextProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(PrimaryText), typeof(string),
				typeof(global::Microsoft.UI.Xaml.Controls.DatePickerFlyoutItem),
				new FrameworkPropertyMetadata(default(string)));

		public static global::Microsoft.UI.Xaml.DependencyProperty SecondaryTextProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
				nameof(SecondaryText), typeof(string),
				typeof(global::Microsoft.UI.Xaml.Controls.DatePickerFlyoutItem),
				new FrameworkPropertyMetadata(default(string)));

		// Uno-specific: also surfaces the item text where only ToString() is consulted.
		public override string ToString() => GetStringRepresentation();
	}
}
