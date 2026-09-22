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
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.InputValidation;

[TestClass]
public partial class Given_Validation_Controls
{
	[TestInitialize]
	public void Initialize() => FeatureConfiguration.Validation.IsEnabled = true;

	[TestCleanup]
	public void Cleanup() => FeatureConfiguration.Validation.IsEnabled = false;

	[TestMethod]
	public void When_Participating_Controls()
	{
		Assert.AreSame(TextBox.TextProperty, Resolve<TextBox>());
		Assert.AreSame(PasswordBox.PasswordProperty, Resolve<PasswordBox>());
		Assert.AreSame(NumberBox.ValueProperty, Resolve<NumberBox>());
		Assert.AreSame(AutoSuggestBox.TextProperty, Resolve<AutoSuggestBox>());
		Assert.AreSame(ToggleSwitch.IsOnProperty, Resolve<ToggleSwitch>());
		Assert.AreSame(ToggleButton.IsCheckedProperty, Resolve<ToggleButton>());
	}

	[TestMethod]
	public void When_Slider_But_Not_ProgressBar()
	{
		// Both derive from RangeBase, which is where Value is registered. A per-dependency-property flag
		// could not have told them apart, which is why the attribute sits on the type.
		Assert.AreSame(RangeBase.ValueProperty, Resolve<Slider>());
		Assert.IsNull(Resolve<ProgressBar>());
	}

	[TestMethod]
	public void When_ComboBox_But_Not_Other_Selectors()
	{
		Assert.AreSame(Selector.SelectedItemProperty, Resolve<Microsoft.UI.Xaml.Controls.ComboBox>());
		Assert.IsNull(Resolve<FlipView>());
		Assert.IsNull(Resolve<ListBox>());
		Assert.IsNull(Resolve<ListView>());
	}

	[TestMethod]
	public void When_Derived_From_A_Participating_Control()
	{
		// ToggleButton carries the attribute for all of its subclasses, and a third-party control derived
		// from a participating one takes part without redeclaring anything.
		Assert.AreSame(ToggleButton.IsCheckedProperty, Resolve<CheckBox>());
		Assert.AreSame(ToggleButton.IsCheckedProperty, Resolve<RadioButton>());
		Assert.AreSame(TextBox.TextProperty, Resolve<CustomTextBox>());
	}

	[TestMethod]
	public void When_TextBox_Bound_To_An_Error_Source()
	{
		var source = new Model();
		var textBox = new TextBox { DataContext = source };
		Validation.SetIsEnabled(textBox, true);
		textBox.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(Model.Name)) });

		source.SetError(nameof(Model.Name), "required");

		Assert.IsTrue(Validation.GetHasErrors(textBox));
		CollectionAssert.AreEqual(
			new object[] { "required" },
			Validation.GetErrors(textBox).Cast<object>().ToArray());
	}

	[TestMethod]
	public void When_ErrorChanged_Is_Raised()
	{
		var source = new Model();
		var textBox = new TextBox { DataContext = source };
		Validation.SetIsEnabled(textBox, true);
		textBox.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(Model.Name)) });

		object? sender = null;
		string? propertyName = null;
		var raised = 0;

		void OnErrorChanged(object? s, DataErrorsChangedEventArgs args)
		{
			raised++;
			sender = s;
			propertyName = args.PropertyName;
		}

		((IInputValidationControl)textBox).ErrorChanged += OnErrorChanged;

		source.SetError(nameof(Model.Name), "required");

		Assert.AreEqual(1, raised);
		Assert.AreSame(textBox, sender);
		Assert.AreEqual(nameof(Model.Name), propertyName);

		((IInputValidationControl)textBox).ErrorChanged -= OnErrorChanged;

		source.SetError(nameof(Model.Name), "still required");

		Assert.AreEqual(1, raised, "the handler was removed");
	}

	private static DependencyProperty? Resolve<T>() => FeatureConfiguration.Validation.GetValidationProperty(typeof(T));

	private partial class CustomTextBox : TextBox;

	private class Model : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		private object[] _errors = Array.Empty<object>();
		private string _name = string.Empty;

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
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
