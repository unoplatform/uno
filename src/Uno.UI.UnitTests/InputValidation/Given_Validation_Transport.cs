#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Windows.Foundation.Collections;
using Windows.Foundation;

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

		Assert.IsFalse(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Mode_Disabled()
	{
		var (control, source) = Bind(enable: false);
		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(control.HasValidationErrors);
		CollectionAssert.AreEqual(Array.Empty<object>(), control.ValidationErrors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Errors_Arrive_And_Clear()
	{
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required", "too short");

		Assert.IsTrue(control.HasValidationErrors);
		CollectionAssert.AreEqual(
			new object[] { "required", "too short" },
			control.ValidationErrors.Select(e => e.ErrorMessage).ToArray());

		source.SetErrors(nameof(Person.Name));

		Assert.IsFalse(control.HasValidationErrors);
		Assert.AreEqual(0, control.ValidationErrors.Count);
	}

	[TestMethod]
	public void When_Source_Is_Leaf_Not_Root()
	{
		// For {Binding Customer.Name} the INotifyDataErrorInfo is Customer, not the page view model.
		var root = new Wrapper { Customer = new Person() };
		var control = new ValidatingControl { DataContext = root };
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Customer.Name") });

		root.Customer.SetErrors(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Leaf_Is_Replaced()
	{
		var root = new Wrapper { Customer = new Person() };
		var control = new ValidatingControl { DataContext = root };
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Customer.Name") });

		root.Customer.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		var replacement = new Person();
		root.Customer = replacement;
		Assert.IsFalse(control.HasValidationErrors, "the replacement reports no errors");

		replacement.SetErrors(nameof(Person.Name), "still required");
		Assert.IsTrue(control.HasValidationErrors, "the subscription followed the new leaf");
	}

	[TestMethod]
	public void When_Another_Property_Reports_Errors()
	{
		var (control, source) = Bind();

		source.SetErrors("SomeOtherProperty", "required");

		Assert.IsFalse(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_PropertyName_Is_Empty_Then_All_Properties()
	{
		var (control, source) = Bind();

		source.SetErrorsWithoutName(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Synchronized_Then_Errors_Keeps_Its_Instance()
	{
		// The collection is mutated in place rather than replaced, so a binding to it is notified by
		// VectorChanged and never has to be re-resolved.
		var (control, source) = Bind();

		source.SetErrors(nameof(Person.Name), "required");
		var first = control.ValidationErrors;

		source.RaiseErrorsChanged(nameof(Person.Name));
		var second = control.ValidationErrors;

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
		Assert.IsFalse(control.HasValidationErrors, "not opted in yet");

		control.InputValidationMode = InputValidationMode.Auto;

		Assert.IsTrue(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Mode_Turned_Off()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		control.InputValidationMode = InputValidationMode.Disabled;

		Assert.IsFalse(control.HasValidationErrors);
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
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		Assert.AreEqual(string.Empty, control.Text);
		Assert.IsTrue(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Rebound_To_Another_Source()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		var replacement = new Person();
		control.DataContext = replacement;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		Assert.IsFalse(control.HasValidationErrors, "the replaced expression must not keep reporting");

		replacement.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		source.SetErrors(nameof(Person.Name), "stale");
		Assert.IsTrue(control.HasValidationErrors, "the old source must no longer be observed");
		CollectionAssert.AreEqual(
			new object[] { "required" },
			control.ValidationErrors.Select(e => e.ErrorMessage).ToArray());
	}

	[TestMethod]
	public void When_Binding_Is_Cleared()
	{
		var (control, source) = Bind();
		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		control.ClearValue(ValidatingControl.TextProperty);

		Assert.IsFalse(control.HasValidationErrors);

		source.SetErrors(nameof(Person.Name), "ignored");
		Assert.IsFalse(control.HasValidationErrors, "the subscription is gone");
	}

	[TestMethod]
	public void When_Control_Has_No_Validation_Property()
	{
		var source = new Person();
		var control = new NonValidatingControl { DataContext = source };
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(NonValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");

		Assert.IsFalse(control.HasValidationErrors);
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
			control.InputValidationMode = InputValidationMode.Auto;
			control.SetBinding(NonValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

			source.SetErrors(nameof(Person.Name), "required");

			Assert.IsTrue(control.HasValidationErrors);
			CollectionAssert.AreEqual(
				new object[] { "required" },
				control.ValidationErrors.Select(e => e.ErrorMessage).ToArray());
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
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath("Name") });

		Assert.IsFalse(control.HasValidationErrors);
	}

	[TestMethod]
	public void When_Compiled_Binding()
	{
		// A compiled binding resolves through its update sources and leaves the binding path empty, so leaf
		// resolution has to read the former. Reading the path alone would have reproduced
		// microsoft-ui-xaml#4642 in reverse, validating {Binding} but silently never an x:Bind.
		var page = new Page { ViewModel = new Person() };
		var control = new ValidatingControl();
		control.InputValidationMode = InputValidationMode.Auto;

		var binding = new Binding { Mode = BindingMode.OneWay, CompiledSource = page };
		binding.SetBindingXBindProvider(
			page,
			o => (true, ((Page)o).ViewModel.Name),
			null,
			new[] { "ViewModel.Name" });

		control.SetBinding(ValidatingControl.TextProperty, binding);
		control.ApplyXBind();

		page.ViewModel.SetErrors(nameof(Person.Name), "required");

		Assert.IsTrue(control.HasValidationErrors);
		CollectionAssert.AreEqual(
			new object[] { "required" },
			control.ValidationErrors.Select(e => e.ErrorMessage).ToArray());
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
		control.InputValidationMode = InputValidationMode.Auto;
		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		source.SetErrors(nameof(Person.Name), "required");
		Assert.IsTrue(control.HasValidationErrors);

		return new WeakReference(control);
	}

	private static (ValidatingControl Control, Person Source) Bind(bool enable = true)
	{
		var source = new Person();
		var control = new ValidatingControl { DataContext = source };

		if (enable)
		{
			control.InputValidationMode = InputValidationMode.Auto;
		}

		control.SetBinding(ValidatingControl.TextProperty, new Binding { Path = new PropertyPath(nameof(Person.Name)) });

		return (control, source);
	}

	/// <summary>
	/// Stands in for a third-party control: it implements the interface and registers all five dependency
	/// properties itself, exactly as a control outside Uno.UI would have to.
	/// </summary>
	private partial class ValidationControlBase : Control, IInputValidationControl
	{
		public static DependencyProperty InputValidationModeProperty { get; } =
			DependencyProperty.Register(
				nameof(InputValidationMode),
				typeof(InputValidationMode),
				typeof(ValidationControlBase),
				new FrameworkPropertyMetadata(
					InputValidationMode.Disabled,
					OnInputValidationModeChanged));

		public static DependencyProperty InputValidationKindProperty { get; } =
			DependencyProperty.Register(
				nameof(InputValidationKind),
				typeof(InputValidationKind),
				typeof(ValidationControlBase),
				new FrameworkPropertyMetadata(
					InputValidationKind.Auto,
					OnInputValidationKindChanged));

		public static DependencyProperty HasValidationErrorsProperty { get; } =
			DependencyProperty.Register(
				nameof(HasValidationErrors),
				typeof(bool),
				typeof(ValidationControlBase),
				new FrameworkPropertyMetadata(
					default(bool),
					OnHasValidationErrorsChanged));

		public static DependencyProperty ValidationErrorsProperty { get; } =
			DependencyProperty.Register(
				nameof(ValidationErrors),
				typeof(IObservableVector<InputValidationError>),
				typeof(ValidationControlBase),
				new FrameworkPropertyMetadata(default(IObservableVector<InputValidationError>)));

		public static DependencyProperty ErrorTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(ErrorTemplate),
				typeof(DataTemplate),
				typeof(ValidationControlBase),
				new FrameworkPropertyMetadata(default(DataTemplate)));

		public InputValidationMode InputValidationMode
		{
			get => (InputValidationMode)GetValue(InputValidationModeProperty);
			set => SetValue(InputValidationModeProperty, value);
		}

		public InputValidationKind InputValidationKind
		{
			get => (InputValidationKind)GetValue(InputValidationKindProperty);
			set => SetValue(InputValidationKindProperty, value);
		}

		public bool HasValidationErrors => (bool)GetValue(HasValidationErrorsProperty);

		public IObservableVector<InputValidationError> ValidationErrors
			=> GetOrCreateValidationErrors(ValidationErrorsProperty);

		public DataTemplate? ErrorTemplate
		{
			get => (DataTemplate?)GetValue(ErrorTemplateProperty);
			set => SetValue(ErrorTemplateProperty, value);
		}

		public event TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> HasValidationErrorsChanged
		{
			add => AddHasValidationErrorsChangedHandler(value);
			remove => RemoveHasValidationErrorsChangedHandler(value);
		}

		public event TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> ValidationError
		{
			add => AddValidationErrorHandler(value);
			remove => RemoveValidationErrorHandler(value);
		}

		public event EventHandler<DataErrorsChangedEventArgs> ErrorChanged
		{
			add => AddErrorChangedHandler(value);
			remove => RemoveErrorChangedHandler(value);
		}
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
