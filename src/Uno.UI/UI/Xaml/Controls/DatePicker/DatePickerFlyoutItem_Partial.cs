namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerFlyoutItem : DependencyObject
	{
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

		////void GetCustomPropertyImpl(string name, out xaml.Data.ICustomProperty returnValue)
		////{
		////	//UNREFERENCED_PARAMETER(name);
		////	//ARG_VALIDRETURNPOINTER(returnValue);
		////	returnValue = null;
		////}

		////void GetIndexedPropertyImpl(string name, wxaml_interop.TypeName type, out xaml.Data.ICustomProperty returnValue)
		////{
		////	UNREFERENCED_PARAMETER(name);
		////	UNREFERENCED_PARAMETER(type);
		////	ARG_VALIDRETURNPOINTER(returnValue);
		////	returnValue = null;
		////}

		//void GetStringRepresentationImpl(out string plainText)
		//{

		//	string primaryText;
		//	string primaryTextWithSpace;
		//	string secondaryText;
		//	string outputText;

		//	primaryText = PrimaryText;
		//	secondaryText = SecondaryText;
		//	if (primaryText != null && secondaryText != null)
		//	{
		//		primaryText.Concat(wrl_wrappers.Hstring(" "), primaryTextWithSpace);
		//		primaryTextWithSpace.Concat(secondaryText, outputText);
		//	}
		//	else if (primaryText != null)
		//	{
		//		primaryText.CopyTo(outputText);
		//	}
		//	else if (secondaryText != null)
		//	{
		//		secondaryText.CopyTo(outputText);
		//	}
		//	else
		//	{
		//		wrl_wrappers.Hstring("").CopyTo(outputText);
		//	}

		//	outputText.CopyTo(plainText);
		//}

		//void get_TypeImpl(out wxaml_interop.TypeName type)
		//{

		//	type.Kind = wxaml_interop.TypeKind_Primitive;
		//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_DatePickerFlyoutItem).CopyTo(type.Name);
		//}


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

		public static global::Windows.UI.Xaml.DependencyProperty PrimaryTextProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(PrimaryText), typeof(string),
				typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyoutItem),
				new FrameworkPropertyMetadata(default(string)));

		public static global::Windows.UI.Xaml.DependencyProperty SecondaryTextProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(SecondaryText), typeof(string),
				typeof(global::Windows.UI.Xaml.Controls.DatePickerFlyoutItem),
				new FrameworkPropertyMetadata(default(string)));

		public override string ToString()
		{
			var primaryText = PrimaryText;
			var secondaryText = SecondaryText;

			if (primaryText != null && secondaryText != null)
			{
				return string.Concat(primaryText, " ", secondaryText);
			}
			else if (primaryText != null)
			{
				return primaryText;
			}
			else if (secondaryText != null)
			{
				return secondaryText;
			}
			else
			{
				return "";
			}
		}
	}
}
