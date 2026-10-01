#nullable enable

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extras.Input;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.InputValidation;

[TestClass]
public partial class Given_Validation_Controls
{
	[TestInitialize]
	public void Initialize() => FeatureConfiguration.InputValidation.IsEnabled = true;

	[TestCleanup]
	public void Cleanup() => FeatureConfiguration.InputValidation.IsEnabled = false;

	[TestMethod]
	public void When_Participating_Controls()
	{
		// The four controls WinUI's CControl type-index switches cover. ComboBox validates what apps bind on a
		// non-editable ComboBox rather than its WinUI [InputProperty], Text.
		Assert.AreSame(TextBox.TextProperty, Resolve<TextBox>());
		Assert.AreSame(PasswordBox.PasswordProperty, Resolve<PasswordBox>());
		Assert.AreSame(AutoSuggestBox.TextProperty, Resolve<AutoSuggestBox>());
		Assert.AreSame(Selector.SelectedItemProperty, Resolve<Microsoft.UI.Xaml.Controls.ComboBox>());
	}

	[TestMethod]
	public void When_Not_A_WinUI_Participant()
	{
		Assert.IsNull(Resolve<NumberBox>());
		Assert.IsNull(Resolve<Slider>());
		Assert.IsNull(Resolve<ProgressBar>());
		Assert.IsNull(Resolve<ToggleSwitch>());
		Assert.IsNull(Resolve<ToggleButton>());
		Assert.IsNull(Resolve<CheckBox>());
		Assert.IsNull(Resolve<RadioButton>());
		Assert.IsNull(Resolve<FlipView>());
		Assert.IsNull(Resolve<ListBox>());
		Assert.IsNull(Resolve<ListView>());
	}

	[TestMethod]
	public void When_Derived_From_A_Participating_Control()
	{
		// A third-party control derived from a participating one takes part without redeclaring anything.
		Assert.AreSame(TextBox.TextProperty, Resolve<CustomTextBox>());
	}

	[TestMethod]
	public void When_TextBox_Bound_To_An_Error_Source()
	{
		var source = new Model();
		var textBox = new TextBox { DataContext = source };
		Validation.SetMode(textBox, InputValidationMode.Auto);
		textBox.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(Model.Name)) });

		source.SetError(nameof(Model.Name), "required");

		Assert.IsTrue(Validation.GetHasErrors(textBox));
		CollectionAssert.AreEqual(
			new object[] { "required" },
			Validation.GetErrors(textBox)!.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_ComboBox_SelectedItem_Bound_To_An_Error_Source()
	{
		var (comboBox, source) = BindComboBox(new Microsoft.UI.Xaml.Controls.ComboBox(), Selector.SelectedItemProperty, nameof(Model.Name));

		source.SetError(nameof(Model.Name), "required");

		Assert.IsTrue(Validation.GetHasErrors(comboBox));
		CollectionAssert.AreEqual(
			new object[] { "required" },
			Validation.GetErrors(comboBox)!.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_ComboBox_Text_Bound_Then_Not_Validated()
	{
		var (comboBox, source) = BindComboBox(new Microsoft.UI.Xaml.Controls.ComboBox(), Microsoft.UI.Xaml.Controls.ComboBox.TextProperty, nameof(Model.Name));

		source.SetError(nameof(Model.Name), "required");

		Assert.IsFalse(Validation.GetHasErrors(comboBox));
	}

	[TestMethod]
	public void When_ComboBox_Remapped_To_SelectedIndex()
	{
		// The app-wide alternative for a ComboBox bound on another property: it replaces SelectedItem.
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		try
		{
			properties[typeof(Microsoft.UI.Xaml.Controls.ComboBox)] = Selector.SelectedIndexProperty;

			var (byIndex, indexSource) = BindComboBox(new Microsoft.UI.Xaml.Controls.ComboBox(), Selector.SelectedIndexProperty, nameof(Model.Index));
			indexSource.SetError(nameof(Model.Index), "required");
			Assert.IsTrue(Validation.GetHasErrors(byIndex));

			var (byItem, itemSource) = BindComboBox(new Microsoft.UI.Xaml.Controls.ComboBox(), Selector.SelectedItemProperty, nameof(Model.Name));
			itemSource.SetError(nameof(Model.Name), "required");
			Assert.IsFalse(Validation.GetHasErrors(byItem));
		}
		finally
		{
			properties.Remove(typeof(Microsoft.UI.Xaml.Controls.ComboBox));
		}
	}

	[TestMethod]
	public void When_ComboBox_Subclass_Declares_SelectedIndex()
	{
		// The per-type alternative: a subclass's own attribute shadows the one ComboBox declares.
		Assert.AreSame(Selector.SelectedIndexProperty, Resolve<SelectedIndexComboBox>());

		var (comboBox, source) = BindComboBox(new SelectedIndexComboBox(), Selector.SelectedIndexProperty, nameof(Model.Index));

		source.SetError(nameof(Model.Index), "required");

		Assert.IsTrue(Validation.GetHasErrors(comboBox));
	}

	private static (T ComboBox, Model Source) BindComboBox<T>(T comboBox, DependencyProperty property, string path)
		where T : Microsoft.UI.Xaml.Controls.ComboBox
	{
		// The bound item is in ItemsSource, so Selector has no reason to coerce it and push null back.
		var source = new Model { Name = "first", Index = 0 };
		comboBox.ItemsSource = new[] { "first", "second" };
		comboBox.DataContext = source;
		Validation.SetMode(comboBox, InputValidationMode.Auto);
		comboBox.SetBinding(property, new Binding { Path = new PropertyPath(path), Mode = BindingMode.TwoWay });

		return (comboBox, source);
	}

	private static DependencyProperty? Resolve<T>() => FeatureConfiguration.InputValidation.ValidationProperties[typeof(T)];

	private partial class CustomTextBox : TextBox;

	[InputValidationProperty(nameof(SelectedIndex))]
	private partial class SelectedIndexComboBox : Microsoft.UI.Xaml.Controls.ComboBox;

	private class Model : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		private object[] _errors = Array.Empty<object>();
		private string _name = string.Empty;
		private int _index;

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
			}
		}

		public int Index
		{
			get => _index;
			set
			{
				_index = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Index)));
			}
		}

		public bool HasErrors => _errors.Length != 0;

		public IEnumerable GetErrors(string? propertyName) => _errors;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public void SetError(string propertyName, params object[] errors)
		{
			_errors = errors;
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		}
	}
}
