using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.CheckBoxTests;

[Sample("Buttons", Description = "A checked, disabled CheckBox should render identically whether standalone or inside a disabled ListView; the list item must not add extra dimming.", IsManualTest = true)]
public sealed partial class CheckBox_DisabledInDisabledListView : Page
{
	public CheckBox_DisabledInDisabledListView()
	{
		this.InitializeComponent();
		DataContext = this;
	}

	public IReadOnlyList<string> Items { get; } = new[]
	{
		"Disabled checkbox in list 1",
		"Disabled checkbox in list 2",
		"Disabled checkbox in list 3",
	};
}
