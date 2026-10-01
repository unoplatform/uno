using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.Extras.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Uno_UI_Xaml_Controls.Validation;

[Sample("Validation", Name = "InputValidation", Description = "INotifyDataErrorInfo errors surfaced on the Uno.Extras.Input.Validation.HasErrors / .Errors attached properties")]
public sealed partial class InputValidation : Page
{
	public InputValidation()
	{
		// Before InitializeComponent: the switch is read as each binding is registered.
		Uno.UI.FeatureConfiguration.InputValidation.IsEnabled = true;

		InitializeComponent();

		DataContext = new SignUpViewModel();
	}
}

/// <summary>
/// One way to satisfy <see cref="INotifyDataErrorInfo"/>. The framework knows nothing about this shape: a
/// source generator, DataAnnotations or CommunityToolkit's ObservableValidator would do just as well.
/// </summary>
public class SignUpViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
	private const string NoCountry = "(none)";

	private readonly Dictionary<string, List<object>> _errors = new();

	private string _userName = "";
	private string _password = "";
	private string _country = NoCountry;

	public string UserName
	{
		get => _userName;
		set
		{
			_userName = value;
			OnPropertyChanged();
			Validate(value, nameof(UserName));
		}
	}

	public string Password
	{
		get => _password;
		set
		{
			_password = value;
			OnPropertyChanged();
			Validate(value, nameof(Password));
		}
	}

	public string Country
	{
		get => _country;
		set
		{
			_country = value;
			OnPropertyChanged();
			Validate(value, nameof(Country));
		}
	}

	public string[] Countries { get; } = { NoCountry, "Canada", "France", "Japan" };

	public bool HasErrors => _errors.Count != 0;

	public IEnumerable GetErrors(string propertyName)
		=> string.IsNullOrEmpty(propertyName)
			? _errors.SelectMany(kvp => kvp.Value).ToList()
			: _errors.TryGetValue(propertyName, out var errors) ? errors : null;

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	private void Validate(object value, string propertyName)
	{
		var errors = new List<object>();

		switch (propertyName)
		{
			case nameof(UserName):
				var userName = (string)value;
				if (string.IsNullOrWhiteSpace(userName))
				{
					errors.Add("A user name is required.");
				}
				else if (userName.Length < 5)
				{
					errors.Add("At least 5 characters, please.");
				}
				break;

			case nameof(Password):
				if (value is not string { Length: >= 8 })
				{
					errors.Add("At least 8 characters, please.");
				}
				break;

			case nameof(Country):
				if (value is null or NoCountry)
				{
					errors.Add("Please choose a country.");
				}
				break;
		}

		if (errors.Count == 0)
		{
			_errors.Remove(propertyName);
		}
		else
		{
			_errors[propertyName] = errors;
		}

		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Supplied by the application: this slice ships no converters, so joining the errors into a single line is
/// the cost of it rendering nothing on its own.
/// </summary>
public class ErrorsToStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is IEnumerable<InputValidationError> errors
			? string.Join(" ", errors.Select(error => error.ErrorMessage))
			: "";

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}

/// <summary>Also the application's, for the same reason.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> value is true ? Visibility.Visible : Visibility.Collapsed;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}
