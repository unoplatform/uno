using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_SelectedIndex")]
	public sealed partial class ComboBox_SelectedIndex : Page
	{
		public ComboBox_SelectedIndex()
		{
			this.InitializeComponent();
			DataContext = this;
		}

		public List<string> Items { get; } = Enumerable.Range(0, 5).Select(i => $"item #{i}").ToList();

		public static DependencyProperty SelectedValueProperty { get; } = DependencyProperty.Register(
			"SelectedValue", typeof(object), typeof(ComboBox_SelectedIndex), new PropertyMetadata(default));

		public object SelectedValue
		{
			get => GetValue(SelectedValueProperty);
			set => SetValue(SelectedValueProperty, value);
		}

		private void BtnNullClick(object sender, RoutedEventArgs e)
		{
			SelectedValue = null;
		}

		private void BtnInvalidClick(object sender, RoutedEventArgs e)
		{
			SelectedValue = "this is an invalid combobox value";
		}

		private void BtnIndex2Click(object sender, RoutedEventArgs e)
		{
			cmb.SelectedIndex = 2;
		}
	}
}
