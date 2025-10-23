// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\NumberBox\NumberBox.cpp, tag winui3/release/1.7.1, commit 5f27a786ac9

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Globalization.NumberFormatting;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class NumberBox
{
	private const string c_numberBoxHeaderName = "HeaderContentPresenter";
	private const string c_numberBoxDownButtonName = "DownSpinButton";
	private const string c_numberBoxUpButtonName = "UpSpinButton";
	private const string c_numberBoxTextBoxName = "InputBox";
	private const string c_numberBoxPopupName = "UpDownPopup";
	private const string c_numberBoxPopupDownButtonName = "PopupDownSpinButton";
	private const string c_numberBoxPopupUpButtonName = "PopupUpSpinButton";
	private const string c_numberBoxPopupContentRootName = "PopupContentRoot";

	private const double c_popupShadowDepth = 16.0;
	private const string c_numberBoxPopupShadowDepthName = "NumberBoxPopupShadowDepth";

	// Shockingly, there is no standard function for trimming strings.
	private const string c_whitespace = " \n\r\t\f\v";

	private static string trim(string s)
	{
		IEnumerable<(char c, int i)> GetNonWhiteSpace()
			=> s.Select((c, i) => (c, i)).Where(p => !c_whitespace.Contains(p.c.ToString()));

		var start = GetNonWhiteSpace().FirstOrDefault();
		var end = GetNonWhiteSpace().LastOrDefault();
		return (start.c == '\0' || end.c == '\0') ? "" : s.Substring(start.i, end.i - start.i + 1);
	}

	public NumberBox()
	{
		NumberFormatter = GetRegionalSettingsAwareDecimalFormatter();

		PointerWheelChanged += OnNumberBoxScroll;

		GotFocus += OnNumberBoxGotFocus;
		LostFocus += OnNumberBoxLostFocus;

#if HAS_UNO // Uno specific.
		Loaded += (s, e) => ReApplyTemplate();
		Unloaded += (s, e) => DisposeRegistrations();
#endif

		this.SetDefaultStyleKey();
		SetDefaultInputScope();

		// We are not revoking this since the event and the listener reside on the same object and as such have the same lifecycle.
		// That means that as soon as the NumberBox gets removed so will the event and the listener.
		RegisterPropertyChangedCallback(AutomationProperties.NameProperty, OnAutomationPropertiesNamePropertyChanged);
		RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, OnAutomationPropertiesLabeledByPropertyChanged);
	}

	private void UnhookEvents()
	{
		_eventSubscriptions.Disposable = null;
	}

	private void SetDefaultInputScope()
	{
		// Sets the default value of the InputScope property.
		// Note that InputScope is a class that cannot be set to a default value within the IDL.

#if HAS_UNO // Uno specific: Use InputScopeNameValue.Default if AcceptsExpression is true.		
		var inputScopeName = new InputScopeName(AcceptsExpression ? InputScopeNameValue.Default : InputScopeNameValue.Number);
#endif
		var inputScope = new InputScope();
		inputScope.Names.Add(inputScopeName);
		InputScope = inputScope;
	}

	// This was largely copied from Calculator's GetRegionalSettingsAwareDecimalFormatter()
	private DecimalFormatter GetRegionalSettingsAwareDecimalFormatter()
	{
		return new DecimalFormatter
		{
			IntegerDigits = 1,
			FractionDigits = 0,
		};

#if HAS_UNO // UNO TODO: https://github.com/unoplatform/uno/issues/6908

		//WCHAR currentLocale[LOCALE_NAME_MAX_LENGTH] = { };
		//if (GetUserDefaultLocaleName(currentLocale, LOCALE_NAME_MAX_LENGTH) != 0)
		//{
		//	// GetUserDefaultLocaleName may return an invalid bcp47 language tag with trailing non-BCP47 friendly characters,
		//	// which if present would start with an underscore, for example sort order
		//	// (see https://msdn.microsoft.com/en-us/library/windows/desktop/dd373814(v=vs.85).aspx).
		//	// Therefore, if there is an underscore in the locale name, trim all characters from the underscore onwards.
		//	WCHAR* underscore = wcschr(currentLocale, L'_');
		//	if (underscore != nullptr)
		//	{
		//		*underscore = L'\0';
		//	}

		//	if (winrt::Language::IsWellFormed(currentLocale))
		//	{
		//		std::vector<winrt::hstring> languageList;
		//		languageList.push_back(winrt::hstring(currentLocale));
		//		formatter = winrt::DecimalFormatter(languageList, winrt::GlobalizationPreferences::HomeGeographicRegion());
		//	}
		//}

		//if (!formatter)
		//{
		//	formatter = winrt::DecimalFormatter();
		//}

		//formatter.IntegerDigits(1);
		//formatter.FractionDigits(0);

		//return formatter;
#endif
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new NumberBoxAutomationPeer(this);
	}

	protected override void OnApplyTemplate()
	{
		InitializeTemplate();
	}

	private void InitializeTemplate()
	{
		UnhookEvents();

		var registrations = new CompositeDisposable();

		var spinDownName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NumberBoxDownSpinButtonName);
		var spinUpName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NumberBoxUpSpinButtonName);

		if (this.GetTemplateChild(c_numberBoxDownButtonName) is RepeatButton spinDown)
		{
			spinDown.Click += OnSpinDownClick;
			registrations.Add(() => spinDown.Click -= OnSpinDownClick);

			// Do localization for the down button
			if (string.IsNullOrEmpty(AutomationProperties.GetName(spinDown)))
			{
				AutomationProperties.SetName(spinDown, spinDownName);
			}
		}

		if (GetTemplateChild(c_numberBoxUpButtonName) is RepeatButton spinUp)
		{
			spinUp.Click += OnSpinUpClick;
			registrations.Add(() => spinUp.Click -= OnSpinUpClick);

			// Do localization for the up button
			if (string.IsNullOrEmpty(AutomationProperties.GetName(spinUp)))
			{
				AutomationProperties.SetName(spinUp, spinUpName);
			}
		}

		UpdateHeaderPresenterState();

		if (GetTemplateChild(c_numberBoxTextBoxName) is TextBox textBox)
		{
#if !HAS_UNO // UNO Docs: PreviewKeyDown is not implemented on all targets. Use KeyDown for now.
			// Listen to PreviewKeyDown because textbox eats the down arrow key in some circumstances.
			textBox.PreviewKeyDown += OnNumberBoxKeyDown;
			registrations.Add(() => textBox.PreviewKeyDown -= OnNumberBoxKeyDown);
#else
			textBox.KeyDown += OnNumberBoxKeyDown;
			registrations.Add(() => textBox.KeyDown -= OnNumberBoxKeyDown);
#endif

			textBox.KeyUp += OnNumberBoxKeyUp;
			registrations.Add(() => textBox.KeyUp -= OnNumberBoxKeyUp);

			m_textBox = textBox;
		}

		if (m_textBox is { } tb)
		{
			tb.Loaded += OnTextBoxLoaded;
			registrations.Add(() => tb.Loaded -= OnTextBoxLoaded);
		}

		m_popup = GetTemplateChild(c_numberBoxPopupName) as Popup;

		// Uno specific: Prevent m_textBox from losing focus when m_popup is opened.
		if (m_popup != null)
		{
			m_popup.IsLightDismissEnabled = false;
		}

		if (GetTemplateChild(c_numberBoxPopupContentRootName) is UIElement popupRoot)
		{
			if (popupRoot.Shadow is null)
			{
				popupRoot.Shadow = new ThemeShadow();
				var translation = popupRoot.Translation;

				double shadowDepth = (double)SharedHelpers.FindResource(c_numberBoxPopupShadowDepthName, Application.Current.Resources, c_popupShadowDepth);

				popupRoot.Translation = new System.Numerics.Vector3(translation.X, translation.Y, (float)shadowDepth);
			}
		}

		if (GetTemplateChild(c_numberBoxPopupDownButtonName) is RepeatButton popupSpinDown)
		{
			popupSpinDown.Click += OnSpinDownClick;
			registrations.Add(() => popupSpinDown.Click -= OnSpinDownClick);
		}

		if (GetTemplateChild(c_numberBoxPopupUpButtonName) is RepeatButton popupSpinUp)
		{
			popupSpinUp.Click += OnSpinUpClick;
			registrations.Add(() => popupSpinUp.Click -= OnSpinUpClick);
		}

		IsEnabledChanged += OnIsEnabledChanged;
		registrations.Add(() => IsEnabledChanged -= OnIsEnabledChanged);

		m_displayRounder.SignificantDigits = 10;

		UpdateSpinButtonPlacement();
		UpdateSpinButtonEnabled();

		UpdateVisualStateForIsEnabledChange();

		ReevaluateForwardedUIAProperties();

		if (ReadLocalValue(ValueProperty) == DependencyProperty.UnsetValue
			&& ReadLocalValue(TextProperty) != DependencyProperty.UnsetValue)
		{
			// If Text has been set, but Value hasn't, update Value based on Text.
			UpdateValueToText();
		}
		else
		{
			UpdateTextToValue();
		}

		// Uno specific.
		_eventSubscriptions.Disposable = registrations;
	}

	private void OnTextBoxLoaded(object sender, RoutedEventArgs args)
	{
		// Updating again once TextBox is loaded so its visual states are set properly.
		UpdateSpinButtonPlacement();
	}

	private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// This handler may change Value; don't send extra events in that case.
		if (!m_valueUpdating)
		{
			var oldValue = (double)args.OldValue;

			try
			{
				m_valueUpdating = true;

				CoerceValue();

				var newValue = Value;
				if (newValue != oldValue && !(double.IsNaN(newValue) && double.IsNaN(oldValue)))
				{
					// Fire ValueChanged event
					var valueChangedArgs = new NumberBoxValueChangedEventArgs(oldValue, newValue);
					ValueChanged?.Invoke(this, valueChangedArgs);

					// Fire value property change for UIA
					if (FrameworkElementAutomationPeer.FromElement(this) is NumberBoxAutomationPeer peer)
					{
						peer.RaiseValueChangedEvent(oldValue, newValue);
					}
				}

				UpdateTextToValue();
				UpdateSpinButtonEnabled();
			}
			finally
			{
				m_valueUpdating = false;
			}
		}
	}

	private void OnMinimumPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		CoerceMaximum();
		CoerceValue();

		UpdateSpinButtonEnabled();
		ReevaluateForwardedUIAProperties();
	}

	private void OnMaximumPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		CoerceMinimum();
		CoerceValue();

		UpdateSpinButtonEnabled();
		ReevaluateForwardedUIAProperties();
	}

	private void OnSmallChangePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateSpinButtonEnabled();
	}

	private void OnIsWrapEnabledPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateSpinButtonEnabled();
	}

	private void OnNumberFormatterPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Update text with new formatting
		UpdateTextToValue();
	}

	// Uno specific
	private void OnAcceptsExpressionPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		SetDefaultInputScope();
	}

	private void ValidateNumberFormatter(INumberFormatter2 value)
	{
		// NumberFormatter also needs to be an INumberParser
		if (!(value is INumberParser))
		{
			throw new ArgumentException(nameof(value));
		}
	}

	private void OnSpinButtonPlacementModePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateSpinButtonPlacement();
	}

	private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (!m_textUpdating)
		{
			UpdateValueToText();
		}
	}

	private void UpdateValueToText()
	{
		if (m_textBox != null)
		{
			m_textBox.Text = Text;
			ValidateInput();
		}
	}

	private void OnHeaderPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateHeaderPresenterState();
	}

	private void OnHeaderTemplatePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		UpdateHeaderPresenterState();
	}

	private void OnValidationModePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		ValidateInput();
		UpdateSpinButtonEnabled();
	}

	private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualStateForIsEnabledChange();
	}

	private void OnAutomationPropertiesNamePropertyChanged(object sender, DependencyProperty dp)
	{
		ReevaluateForwardedUIAProperties();
	}

	private void OnAutomationPropertiesLabeledByPropertyChanged(object sender, DependencyProperty dp)
	{
		ReevaluateForwardedUIAProperties();
	}

	private void ReevaluateForwardedUIAProperties()
	{
		if (m_textBox is TextBox textBox)
		{
			// UIA Name
			var name = AutomationProperties.GetName(this);
			var minimum = Minimum == -double.MaxValue ? "" : " " + ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NumberBoxMinimumValueStatus) + Minimum.ToString(CultureInfo.InvariantCulture);
			var maximum = Maximum == double.MaxValue ? "" : " " + ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NumberBoxMaximumValueStatus) + Maximum.ToString(CultureInfo.InvariantCulture);
			if (!string.IsNullOrEmpty(name))
			{
				// AutomationProperties.Name is a non empty string, we will use that value.
				AutomationProperties.SetName(textBox, name + minimum + maximum);
			}
			else
			{
				if (Header is string headerAsString)
				{
					// Header is a string, we can use that as our UIA name.
					AutomationProperties.SetName(textBox, headerAsString + minimum + maximum);
				}
			}

			// UIA LabeledBy
			var labeledBy = AutomationProperties.GetLabeledBy(this);
			if (labeledBy is not null)
			{
				AutomationProperties.SetLabeledBy(textBox, labeledBy);
			}
		}
	}

	private void UpdateVisualStateForIsEnabledChange()
	{
		VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);
	}

	private void OnNumberBoxGotFocus(object sender, RoutedEventArgs args)
	{
		// When the control receives focus, select the text
		if (m_textBox != null)
		{
			m_textBox.SelectAll();
		}

		if (SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Compact)
		{
			if (m_popup != null)
			{
				m_popup.IsOpen = true;
			}
		}
	}

	private void OnNumberBoxLostFocus(object sender, RoutedEventArgs args)
	{
		ValidateInput();

		if (m_popup != null)
		{
			m_popup.IsOpen = false;
		}
	}

	private void CoerceMinimum()
	{
		var max = Maximum;
		if (Minimum > max)
		{
			Minimum = max;
		}
	}

	private void CoerceMaximum()
	{
		var min = Minimum;
		if (Maximum < min)
		{
			Maximum = min;
		}
	}

	private void CoerceValue()
	{
		// Validate that the value is in bounds
		var value = Value;
		if (!double.IsNaN(value) && !IsInBounds(value) && ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
		{
			// Coerce value to be within range
			var max = Maximum;
			if (value > max)
			{
				Value = max;
			}
			else
			{
				Value = Minimum;
			}
		}
	}

	private void ValidateInput()
	{
		// Validate the content of the inner textbox
		if (m_textBox != null)
		{
			var text = trim(m_textBox.Text);

			// Handles empty TextBox case, set text to current value
			if (string.IsNullOrEmpty(text))
			{
				Value = double.NaN;
			}
			else
			{
				// Setting NumberFormatter to something that isn't an INumberParser will throw an exception, so this should be safe
				var numberParser = NumberFormatter as INumberParser;

				var value = AcceptsExpression
					? NumberBoxParser.Compute(text, numberParser)
					: numberParser.ParseDouble(text);

				if (value == null)
				{
					if (ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
					{
						// Override text to current value
						UpdateTextToValue();
					}
				}
				else
				{
					if (value.Value == Value)
					{
						// Even if the value hasn't changed, we still want to update the text (e.g. Value is 3, user types 1 + 2, we want to replace the text with 3)
						UpdateTextToValue();
					}
					else
					{
						Value = value.Value;
					}
				}
			}
		}
	}

	private void OnSpinDownClick(object sender, RoutedEventArgs args)
	{
		StepValue(-SmallChange);
	}

	private void OnSpinUpClick(object sender, RoutedEventArgs args)
	{
		StepValue(SmallChange);
	}

	private void OnNumberBoxKeyDown(object sender, KeyRoutedEventArgs args)
	{
		// Handle these on key down so that we get repeat behavior.
		switch (args.OriginalKey)
		{
			case VirtualKey.Up:
				StepValue(SmallChange);
				args.Handled = true;
				break;

			case VirtualKey.Down:
				StepValue(-SmallChange);
				args.Handled = true;
				break;

			case VirtualKey.PageUp:
				StepValue(LargeChange);
				args.Handled = true;
				break;

			case VirtualKey.PageDown:
				StepValue(-LargeChange);
				args.Handled = true;
				break;
		}
	}

	private void OnNumberBoxKeyUp(object sender, KeyRoutedEventArgs args)
	{
		switch (args.OriginalKey)
		{
			case VirtualKey.Enter:
			case VirtualKey.GamepadA:
				ValidateInput();
				args.Handled = true;
				break;

			case VirtualKey.Escape:
			case VirtualKey.GamepadB:
				UpdateTextToValue();
				args.Handled = true;
				break;
		}
	}

	private void OnNumberBoxScroll(object sender, PointerRoutedEventArgs args)
	{
		if (m_textBox != null)
		{
			if (m_textBox.FocusState != FocusState.Unfocused)
			{
				var delta = args.GetCurrentPoint(this).Properties.MouseWheelDelta;
				if (delta > 0)
				{
					StepValue(SmallChange);
				}
				else if (delta < 0)
				{
					StepValue(-SmallChange);
				}
				// Only set as handled when we actually changed our state.
				args.Handled = true;
			}
		}
	}

	private void StepValue(double change)
	{
		// Before adjusting the value, validate the contents of the textbox so we don't override it.
		ValidateInput();

		var newVal = Value;
		if (!double.IsNaN(newVal))
		{
			newVal += change;

			if (IsWrapEnabled)
			{
				var max = Maximum;
				var min = Minimum;

				if (newVal > max)
				{
					newVal = min;
				}
				else if (newVal < min)
				{
					newVal = max;
				}
			}

			Value = newVal;

			// We don't want the caret to move to the front of the text for example when using the up/down arrows
			// to change the numberbox value.
			MoveCaretToTextEnd();

		}
	}

	// Updates TextBox.Text with the formatted Value
	private void UpdateTextToValue()
	{
		if (m_textBox != null)
		{
			string newText = "";

			var value = Value;
			if (!double.IsNaN(value))
			{
				// Rounding the value here will prevent displaying digits caused by floating point imprecision.
				var roundedValue = m_displayRounder.RoundDouble(value);
				newText = NumberFormatter.FormatDouble(roundedValue);
			}

			m_textBox.Text = newText;

			try
			{
				m_textUpdating = true;
				Text = newText;
			}
			finally
			{
				m_textUpdating = false;
			}
		}
	}

	private void UpdateSpinButtonPlacement()
	{
		var spinButtonMode = SpinButtonPlacementMode;
		var state = spinButtonMode switch
		{
			NumberBoxSpinButtonPlacementMode.Inline => "SpinButtonsVisible",
			NumberBoxSpinButtonPlacementMode.Compact => "SpinButtonsPopup",
			_ => "SpinButtonsCollapsed"
		};

		VisualStateManager.GoToState(this, state, false);
		if (m_textBox is TextBox textBox)
		{
			VisualStateManager.GoToState(textBox, state, false);
		}
	}

	private void UpdateSpinButtonEnabled()
	{
		var value = Value;
		bool isUpButtonEnabled = false;
		bool isDownButtonEnabled = false;

		if (!double.IsNaN(value))
		{
			if (IsWrapEnabled || ValidationMode != NumberBoxValidationMode.InvalidInputOverwritten)
			{
				// If wrapping is enabled, or invalid values are allowed, then the buttons should be enabled
				isUpButtonEnabled = true;
				isDownButtonEnabled = true;
			}
			else
			{
				if (value < Maximum)
				{
					isUpButtonEnabled = true;
				}
				if (value > Minimum)
				{
					isDownButtonEnabled = true;
				}
			}
		}

		VisualStateManager.GoToState(this, isUpButtonEnabled ? "UpSpinButtonEnabled" : "UpSpinButtonDisabled", false);
		VisualStateManager.GoToState(this, isDownButtonEnabled ? "DownSpinButtonEnabled" : "DownSpinButtonDisabled", false);
	}

	private bool IsInBounds(double value)
	{
		return (value >= Minimum && value <= Maximum);
	}

	private void UpdateHeaderPresenterState()
	{
		bool shouldShowHeader = false;

		// Load header presenter as late as possible

		// To enable lightweight styling, collapse header presenter if there is no header specified
		var header = Header;
		if (header != null)
		{
			// Check if header is string or not
			var headerAsString = header as string;
			if (headerAsString != null)
			{
				if (headerAsString != string.Empty)
				{
					// Header is not empty string
					shouldShowHeader = true;
				}
			}
			else
			{
				// Header is not a string, so let's show header presenter
				shouldShowHeader = true;
				// When our header isn't a string, we use the NumberBox's UIA name for the textbox's UIA name.
				if (m_textBox is TextBox textBox)
				{
					AutomationProperties.SetName(textBox, AutomationProperties.GetName(this));
				}
			}
		}
		var headerTemplate = HeaderTemplate;
		if (headerTemplate != null)
		{
			shouldShowHeader = true;
		}

		if (shouldShowHeader && m_headerPresenter == null)
		{
			var headerPresenter = GetTemplateChild<ContentPresenter>(c_numberBoxHeaderName);
			if (headerPresenter != null)
			{
				// Set presenter to enable lightweight styling of the headers margin
				m_headerPresenter = headerPresenter;
			}
		}

		if (m_headerPresenter != null)
		{
			m_headerPresenter.Visibility = shouldShowHeader ? Visibility.Visible : Visibility.Collapsed;
		}

		ReevaluateForwardedUIAProperties();
	}

	private void MoveCaretToTextEnd()
	{
		if (m_textBox is TextBox textBox)
		{
			textBox.Select(textBox.Text.Length, 0);
		}
	}
}
