#nullable enable
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.ExpanderTests;

[Sample("Expander", "MUX",
	Name = "Expander_Bindings_TemplatedControl",
	IsManualTest = true,
	Description =
	"Validates that the Expander control can be templated and that bindings work correctly. \r\n" +
	"When the Content property is bound to a templated control, the control should be displayed correctly. \r\n" +
	"When expanded the control should display two TextBlocks with the text 'Hello From Header in Custom Control' and 'On the Content'."
	)]
public sealed partial class Expander_Bindings_TemplatedControl : UserControl
{
	public Expander_Bindings_TemplatedControl()
	{
		this.InitializeComponent();
	}
}

public partial class SettingsExpander : Control
{
	public SettingsExpander() { }

	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public bool IsExpanded
	{
		get => (bool)GetValue(IsExpandedProperty);
		set => SetValue(IsExpandedProperty, value);
	}
	protected virtual void OnIsExpandedPropertyChanged(bool oldValue, bool newValue)
	{
	}

	public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
		nameof(Header),
		typeof(object),
		typeof(SettingsExpander),
		new PropertyMetadata(defaultValue: null));

	public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
		 nameof(IsExpanded),
		 typeof(bool),
		 typeof(SettingsExpander),
		 new PropertyMetadata(defaultValue: false, (d, e) => ((SettingsExpander)d).OnIsExpandedPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));
}
