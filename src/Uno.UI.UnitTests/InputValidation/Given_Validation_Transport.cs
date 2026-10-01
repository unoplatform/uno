#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extras.Input;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Uno.UI.Tests.InputValidation;

[TestClass]
public partial class Given_Validation_Transport
{
	[TestInitialize]
	public void Initialize() => FeatureConfiguration.InputValidation.IsEnabled = true;

	[TestCleanup]
	public void Cleanup() => FeatureConfiguration.InputValidation.IsEnabled = false;

	[TestMethod]
	public void When_Global_Switch_Off()
	{
		FeatureConfiguration.InputValidation.IsEnabled = false;

		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(control.HasErrors);
	}

	[TestMethod]
	public void When_Mode_Disabled()
	{
		var (control, source) = Bind(enable: false);
		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(control.HasErrors);
		CollectionAssert.AreEqual(Array.Empty<object>(), control.Errors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Errors_Arrive_And_Clear()
	{
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required", "too short");

		Assert.IsTrue(control.HasErrors);
		CollectionAssert.AreEqual(
			new object[] { "required", "too short" },
			control.Errors.Select(e => e.ErrorMessage).ToArray());

		source.SetErrors(nameof(Person.Name));

		Assert.IsFalse(control.HasErrors);
		Assert.AreEqual(0, control.Errors.Count);
	}

	[TestMethod]
	public void When_Errors_Are_Not_Strings()
	{
		// INotifyDataErrorInfo yields an untyped IEnumerable, and the ObservableValidator of the MVVM Toolkit
		// fills it with ValidationResult. Only its ToString() reaches InputValidationError.
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), new ValidationResult("The Name field is required."));

		Assert.IsTrue(control.HasErrors);
		CollectionAssert.AreEqual(
			new object[] { "The Name field is required." },
			control.Errors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Source_Is_Leaf_Not_Root()
	{
		// For {Binding Customer.Name} the INotifyDataErrorInfo is Customer, not the page view model.
		var root = new Wrapper { Customer = new Person() };
		var control = new ValidatingControl { DataContext = root };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Customer.Name") });

		root.Customer.SetErrors(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasErrors);
	}

	[TestMethod]
	public void When_Leaf_Is_Replaced()
	{
		var root = new Wrapper { Customer = new Person() };
		var control = new ValidatingControl { DataContext = root };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Customer.Name") });

		root.Customer.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		var replacement = new Person();
		root.Customer = replacement;
		Assert.IsFalse(control.HasErrors, "the replacement reports no errors");

		replacement.SetErrors(nameof(Person.Name), "still required");
		Assert.IsTrue(control.HasErrors, "the subscription followed the new leaf");
	}

	[TestMethod]
	public void When_Another_Property_Reports_Errors()
	{
		var (control, source) = Bind();

		source.SetErrors("SomeOtherProperty", "required");

		Assert.IsFalse(control.HasErrors);
	}

	[TestMethod]
	public void When_PropertyName_Is_Empty_Then_All_Properties()
	{
		var (control, source) = Bind();

		source.SetErrorsWithoutName(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasErrors);
	}

	[TestMethod]
	public void When_Synchronized_Then_Errors_Keeps_Its_Instance()
	{
		// The collection is mutated in place rather than replaced, so a binding to it is notified by
		// VectorChanged and never has to be re-resolved.
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required");
		var first = control.Errors;

		source.RaiseErrorsChanged(nameof(Person.Name));
		var second = control.Errors;

		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void When_Mode_Set_After_The_Binding()
	{
		// Generated XAML applies the binding before the attached property.
		var source = new Person();
		var control = new ValidatingControl { DataContext = source };
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsFalse(control.HasErrors, "not opted in yet");

		control.Mode = InputValidationMode.Auto;

		Assert.IsTrue(control.HasErrors);
	}

	[TestMethod]
	public void When_Mode_Turned_Off()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		control.Mode = InputValidationMode.Disabled;

		Assert.IsFalse(control.HasErrors);
	}

	[TestMethod]
	public void When_Initial_Value_Equals_The_Property_Default()
	{
		// Text defaults to string.Empty and the source already holds string.Empty, so no value change ever
		// reaches the target. A design polling for the expression from a changed-callback would never
		// subscribe at all.
		var source = new Person { Name = string.Empty };
		source.SetErrors(nameof(Person.Name), "required");

		var control = new ValidatingControl { DataContext = source };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		Assert.AreEqual(string.Empty, control.Text);
		Assert.IsTrue(control.HasErrors);
	}

	[TestMethod]
	public void When_Rebound_To_Another_Source()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		var replacement = new Person();
		control.DataContext = replacement;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		Assert.IsFalse(control.HasErrors, "the replaced expression must not keep reporting");

		replacement.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		source.SetErrors(nameof(Person.Name), "stale");
		Assert.IsTrue(control.HasErrors, "the old source must no longer be observed");
		CollectionAssert.AreEqual(
			new object[] { "required" },
			control.Errors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Binding_Is_Cleared()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		control.ClearValue(ValidatingControl.TextProperty);

		Assert.IsFalse(control.HasErrors);

		source.SetErrors(nameof(Person.Name), "ignored");
		Assert.IsFalse(control.HasErrors, "the subscription is gone");
	}

	[TestMethod]
	public void When_Control_Has_No_Validation_Property()
	{
		var source = new Person();
		var control = new NonValidatingControl { DataContext = source };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(NonValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(control.HasErrors);
	}

	[TestMethod]
	public void When_Validation_Property_Is_Registered_Then_Errors_Flow()
	{
		// The same control taking part through the map instead of the attribute, which is the whole point of
		// the map being public.
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		try
		{
			properties[typeof(NonValidatingControl)] = NonValidatingControl.TextProperty;

			var source = new Person();
			var control = new NonValidatingControl { DataContext = source };
			control.Mode = InputValidationMode.Auto;
			control.SetBinding(NonValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

			source.SetErrors(nameof(Person.Name), "required");

			Assert.IsTrue(control.HasErrors);
			CollectionAssert.AreEqual(
				new object[] { "required" },
				control.Errors.Select(e => e.ErrorMessage).ToArray());
		}
		finally
		{
			properties.Remove(typeof(NonValidatingControl));
		}
	}

	[TestMethod]
	public void When_Source_Does_Not_Implement_The_Interface()
	{
		var control = new ValidatingControl { DataContext = new PlainSource() };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Name") });

		Assert.IsFalse(control.HasErrors);
	}

	[TestMethod]
	public void When_Compiled_Binding()
	{
		// A compiled binding resolves through its update sources and leaves the binding path empty, so leaf
		// resolution has to read the former. Reading the path alone would have reproduced
		// microsoft-ui-xaml#4642 in reverse, validating {Binding} but silently never an x:Bind.
		var page = new Page { ViewModel = new Person() };
		var control = new ValidatingControl();
		control.Mode = InputValidationMode.Auto;

		var binding = new Binding { Mode = BindingMode.OneWay, CompiledSource = page };
		binding.SetBindingXBindProvider(
			page,
			o => (true, ((Page)o).ViewModel.Name),
			null,
			new[] { "ViewModel.Name" });

		control.SetBinding(ValidatingControl.TextProperty, binding);
		control.ApplyXBind();

		page.ViewModel.SetErrors(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasErrors);
		CollectionAssert.AreEqual(
			new object[] { "required" },
			control.Errors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Control_Is_Collected()
	{
		// A strong ErrorsChanged handler would let a long-lived view model root every control bound to it.
		var source = new Person();
		var reference = BindAndForget(source);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(reference.IsAlive, "the source must not root the control");

		// And the orphaned handler must not throw when the source raises after the collection.
		source.SetErrors(nameof(Person.Name), "required");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference BindAndForget(Person source)
	{
		var control = new ValidatingControl { DataContext = source };
		control.Mode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasErrors);

		return new WeakReference(control);
	}

	private static (ValidatingControl Control, Person Source) Bind(bool enable = true)
	{
		var source = new Person();
		var control = new ValidatingControl { DataContext = source };

		if (enable)
		{
			control.Mode = InputValidationMode.Auto;
		}

		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		return (control, source);
	}

	/// <summary>
	/// Reads the attached properties through instance members, for brevity. It sits in this assembly, which
	/// sees Uno.UI's internals, so it is not evidence about the third-party contract —
	/// <see cref="Given_Validation_ThirdParty"/> is.
	/// </summary>
	private partial class ValidationControlBase : Control
	{
		public InputValidationMode Mode
		{
			get => Validation.GetMode(this);
			set => Validation.SetMode(this, value);
		}

		public bool HasErrors => Validation.GetHasErrors(this);

		public IObservableVector<InputValidationError> Errors => Validation.GetErrors(this)!;
	}

	[InputValidationProperty("Text")]
	private partial class ValidatingControl : ValidationControlBase
	{
		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(ValidatingControl),
				new FrameworkPropertyMetadata(string.Empty));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}
	}

	private partial class NonValidatingControl : ValidationControlBase
	{
		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(NonValidatingControl),
				new FrameworkPropertyMetadata(string.Empty));
	}

	private class PlainSource
	{
		public string Name { get; set; } = string.Empty;
	}

	/// <summary>Stands in for the page a compiled binding uses as its compiled source.</summary>
	private class Page
	{
		public Person ViewModel { get; set; } = new();
	}

	private class Wrapper : INotifyPropertyChanged
	{
		private Person _customer = new();

		public Person Customer
		{
			get => _customer;
			set
			{
				_customer = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Customer)));
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
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
			Store(propertyName, errors);
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		}

		/// <summary>Raises with no property name, which means "all properties".</summary>
		public void SetErrorsWithoutName(string propertyName, params object[] errors)
		{
			Store(propertyName, errors);
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
		}

		public void RaiseErrorsChanged(string propertyName)
			=> ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

		private void Store(string propertyName, object[] errors)
		{
			if (errors.Length == 0)
			{
				_errors.Remove(propertyName);
			}
			else
			{
				_errors[propertyName] = errors.ToList();
			}
		}
	}
}
