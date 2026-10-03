#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extras.Input;
using Uno.UI.Tests.ViewLibrary;

namespace Uno.UI.Tests.InputValidation;

/// <summary>
/// Covers a control that participates in input validation from outside Uno.UI.
/// </summary>
/// <remarks>
/// The controls under test live in <c>Uno.UI.Tests.ViewLibrary</c>, which is deliberately absent from
/// Uno.UI's InternalsVisibleTo list and declares nothing but its input — these tests prove that is enough.
/// The doubles in <see cref="Given_Validation_Transport"/> cannot prove it: they are in this assembly, which
/// does see Uno.UI's internals.
/// </remarks>
[TestClass]
public partial class Given_Validation_ThirdParty
{
	[TestInitialize]
	public void Initialize() => FeatureConfiguration.InputValidation.IsEnabled = true;

	[TestCleanup]
	public void Cleanup()
	{
		FeatureConfiguration.InputValidation.IsEnabled = false;

		// The map is process-global, so an entry added by a test outlives it.
		FeatureConfiguration.InputValidation.ValidationProperties.Remove(typeof(MyMappedValidatingControl));
	}

	[TestMethod]
	public void When_Errors_Arrive_And_Clear()
	{
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required", "too short");

		Assert.IsTrue(Validation.GetHasErrors(control));
		CollectionAssert.AreEqual(
			new object[] { "required", "too short" },
			Validation.GetErrors(control)!.Select(e => e.ErrorMessage).ToArray());

		source.SetErrors(nameof(Person.Name));

		Assert.IsFalse(Validation.GetHasErrors(control));
		Assert.AreEqual(0, Validation.GetErrors(control)!.Count);
	}

	[TestMethod]
	public void When_Enabled_After_The_Source_Already_Has_Errors()
	{
		var (control, source) = Bind(enable: false);

		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(Validation.GetHasErrors(control));

		Validation.SetMode(control, InputValidationMode.Auto);

		Assert.IsTrue(Validation.GetHasErrors(control));
		CollectionAssert.AreEqual(
			new object[] { "required" },
			Validation.GetErrors(control)!.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Disabled_After_Reporting_Errors()
	{
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(Validation.GetHasErrors(control));

		Validation.SetMode(control, InputValidationMode.Disabled);

		Assert.IsFalse(Validation.GetHasErrors(control));
	}

	[TestMethod]
	public void When_Input_Registered_Through_The_Map()
	{
		// The escape hatch for a control that cannot carry the attribute.
		FeatureConfiguration.InputValidation.ValidationProperties[typeof(MyMappedValidatingControl)]
			= MyMappedValidatingControl.ValueProperty;

		var source = new Person();
		var control = new MyMappedValidatingControl { DataContext = source };
		Validation.SetMode(control, InputValidationMode.Auto);

		control.SetBinding(
			MyMappedValidatingControl.ValueProperty,
			new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsTrue(Validation.GetHasErrors(control));
		CollectionAssert.AreEqual(
			new object[] { "required" },
			Validation.GetErrors(control)!.Select(e => e.ErrorMessage).ToArray());
	}

	private static (MyValidatingControl Control, Person Source) Bind(bool enable = true)
	{
		var source = new Person();
		var control = new MyValidatingControl { DataContext = source };

		if (enable)
		{
			Validation.SetMode(control, InputValidationMode.Auto);
		}

		control.SetBinding(
			MyValidatingControl.TextProperty,
			new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		return (control, source);
	}

	private class Person : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		private readonly Dictionary<string, List<object>> _errors = new();
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

		public bool HasErrors => _errors.Count != 0;

		public IEnumerable GetErrors(string? propertyName)
			=> string.IsNullOrEmpty(propertyName)
				? _errors.SelectMany(kvp => kvp.Value).ToList()
				: _errors.TryGetValue(propertyName!, out var errors) ? errors : null!;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public void SetErrors(string propertyName, params object[] errors)
		{
			if (errors.Length == 0)
			{
				_errors.Remove(propertyName);
			}
			else
			{
				_errors[propertyName] = errors.ToList();
			}

			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		}
	}
}
