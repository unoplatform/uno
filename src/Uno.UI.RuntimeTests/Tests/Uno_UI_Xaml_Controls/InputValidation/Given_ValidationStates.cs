#nullable enable

#if HAS_UNO

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using UnoValidation = Uno.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Controls.InputValidation;

[TestClass]
[RunsOnUIThread]
public class Given_ValidationStates
{
	private const string EnabledStates = "InputValidationEnabledStates";
	private const string ErrorStates = "InputValidationErrorStates";

	// Every state is empty: this asserts state selection only, and so keeps well clear of the shared
	// animated-value slot a setter would land in.
	private const string TemplateXaml = """
		<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
						 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			<!-- Sized so that WaitForLoaded, which polls for a non-zero size, actually settles. -->
			<Grid Width="100" Height="32">
				<VisualStateManager.VisualStateGroups>
					<VisualStateGroup x:Name="InputValidationEnabledStates">
						<VisualState x:Name="CompactValidationEnabled" />
						<VisualState x:Name="InlineValidationEnabled" />
						<VisualState x:Name="ValidationDisabled" />
					</VisualStateGroup>
					<VisualStateGroup x:Name="InputValidationErrorStates">
						<VisualState x:Name="CompactErrors" />
						<VisualState x:Name="InlineErrors" />
						<VisualState x:Name="ErrorsCleared" />
					</VisualStateGroup>
				</VisualStateManager.VisualStateGroups>
			</Grid>
		</ControlTemplate>
		""";

	private bool _switch;

	[TestInitialize]
	public void Initialize()
	{
		_switch = FeatureConfiguration.InputValidation.IsEnabled;
		FeatureConfiguration.InputValidation.IsEnabled = true;
	}

	[TestCleanup]
	public void Cleanup() => FeatureConfiguration.InputValidation.IsEnabled = _switch;

	[TestMethod]
	public async Task When_Enabled_And_No_Errors_Then_Compact()
	{
		var (sut, _) = await Bind();

		Assert.AreEqual("CompactValidationEnabled", StateOf(sut, EnabledStates));
		Assert.AreEqual("ErrorsCleared", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Kind_Is_Inline_Then_Inline()
	{
		var (sut, _) = await Bind(kind: InputValidationKind.Inline);

		Assert.AreEqual("InlineValidationEnabled", StateOf(sut, EnabledStates));
		Assert.AreEqual("ErrorsCleared", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Kind_Is_Auto_Then_Behaves_As_Compact()
	{
		// WinUI never resolves Auto to anything: ShowErrorsInline is kind == Inline, so Auto falls into the
		// compact branch. Asserted so the asymmetry is not tidied up later.
		var (sut, _) = await Bind(kind: InputValidationKind.Auto);

		Assert.AreEqual("CompactValidationEnabled", StateOf(sut, EnabledStates));
	}

	[TestMethod]
	public async Task When_Errors_Arrive_Then_Errors_State()
	{
		var (sut, source) = await Bind();

		// No pointer, focus or enabled change: the error itself has to drive the state.
		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));
		Assert.AreEqual("CompactValidationEnabled", StateOf(sut, EnabledStates));
	}

	[TestMethod]
	public async Task When_Errors_Arrive_And_Kind_Is_Inline_Then_Inline_Errors()
	{
		var (sut, source) = await Bind(kind: InputValidationKind.Inline);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("InlineErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Errors_Clear_Then_Errors_Cleared()
	{
		var (sut, source) = await Bind();

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));

		source.SetErrors();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ErrorsCleared", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Mode_Turned_Off_Then_Disabled_And_Error_State_Kept()
	{
		var (sut, source) = await Bind();

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));

		sut.InputValidationMode = InputValidationMode.Disabled;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("ValidationDisabled", StateOf(sut, EnabledStates));

		// The disabled branch of EnsureValidationVisuals does not touch the error group. A regression
		// anchor: nothing should be helpfully added that resets it.
		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Template_Applied_After_Errors_Then_Errors_State()
	{
		// The case that makes the template-realization trigger load-bearing rather than defensive.
		var source = new ErrorSource();
		var sut = new TextBox { DataContext = source };
		sut.InputValidationMode = InputValidationMode.Auto;
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		source.SetErrors("required");
		Assert.IsTrue(
			sut.HasValidationErrors,
			"the error should reach the control before any template exists");

		sut.Template = (ControlTemplate)XamlReader.Load(TemplateXaml);
		await UITestHelper.Load(sut);

		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Control_Has_No_Visual_State_Method_Then_Template_Realization_Applies()
	{
		// AutoSuggestBox participates but has no ChangeVisualState override, so no per-control call site
		// re-applies the states for it. With errors already reported before the template exists, the
		// InvokeApplyTemplate anchor is the only thing that can put it in one.
		var source = new ErrorSource();
		var sut = new AutoSuggestBox { DataContext = source };
		sut.InputValidationMode = InputValidationMode.Auto;
		sut.SetBinding(AutoSuggestBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		source.SetErrors("required");
		Assert.IsTrue(
			sut.HasValidationErrors,
			"the error should reach the control before any template exists");

		sut.Template = (ControlTemplate)XamlReader.Load(TemplateXaml);
		await UITestHelper.Load(sut);

		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Mode_Set_Through_A_Style_Then_Participates()
	{
		// The reason InputValidationMode is a dependency property on the control rather than an attached one:
		// a Setter can target it. It could not target a plain property forwarding to an attached value.
		var style = (Style)XamlReader.Load("""
			<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				   TargetType="TextBox">
				<Setter Property="InputValidationMode" Value="Auto" />
				<Setter Property="InputValidationKind" Value="Inline" />
			</Style>
			""");

		var source = new ErrorSource();
		var sut = new TextBox
		{
			DataContext = source,
			Style = style,
			Template = (ControlTemplate)XamlReader.Load(TemplateXaml),
		};
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		await UITestHelper.Load(sut);

		Assert.AreEqual(InputValidationMode.Auto, sut.InputValidationMode);
		Assert.AreEqual("InlineValidationEnabled", StateOf(sut, EnabledStates));

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("InlineErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Mode_Is_Bound_Then_Participation_Follows()
	{
		// Also only possible against a dependency property.
		var source = new ErrorSource();
		var sut = new TextBox
		{
			DataContext = source,
			Template = (ControlTemplate)XamlReader.Load(TemplateXaml),
		};
		sut.SetBinding(TextBox.InputValidationModeProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.ValidationMode)) });
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		source.SetErrors("required");
		await UITestHelper.Load(sut);

		Assert.AreEqual(InputValidationMode.Auto, sut.InputValidationMode);
		Assert.AreEqual("CompactErrors", StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Control_Does_Not_Participate_Then_Disabled_And_No_Error_State()
	{
		var sut = new TextBox { Template = (ControlTemplate)XamlReader.Load(TemplateXaml) };
		await UITestHelper.Load(sut);

		// Template realization applies the disabled branch of EnsureValidationVisuals, which leaves the error
		// group untouched.
		Assert.AreEqual("ValidationDisabled", StateOf(sut, EnabledStates));
		Assert.IsNull(StateOf(sut, ErrorStates));
	}

	[TestMethod]
	public async Task When_Errors_Change_Then_Events_Are_Raised()
	{
		var (sut, source) = await Bind();
		var control = (IInputValidationControl)sut;

		var hasErrorsChanges = new List<bool>();
		var errorEvents = new List<InputValidationErrorEventAction>();
		control.HasValidationErrorsChanged += (_, args) => hasErrorsChanges.Add(args.NewValue);
		control.ValidationError += (_, args) => errorEvents.Add(args.Action);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { true }, hasErrorsChanges);
		CollectionAssert.AreEqual(new[] { InputValidationErrorEventAction.Added }, errorEvents);
		CollectionAssert.AreEqual(
			new[] { "required" },
			control.ValidationErrors.Select(error => error.ErrorMessage).ToArray());

		source.SetErrors();
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { true, false }, hasErrorsChanges);
		CollectionAssert.AreEqual(
			new[] { InputValidationErrorEventAction.Added, InputValidationErrorEventAction.Removed },
			errorEvents);
	}

	private static async Task<(TextBox Sut, ErrorSource Source)> Bind(
		InputValidationKind kind = InputValidationKind.Auto)
	{
		var source = new ErrorSource();
		var sut = new TextBox
		{
			DataContext = source,
			Template = (ControlTemplate)XamlReader.Load(TemplateXaml),
		};

		sut.InputValidationKind = kind;
		sut.InputValidationMode = InputValidationMode.Auto;
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		await UITestHelper.Load(sut);

		return (sut, source);
	}

	private static string? StateOf(Control control, string groupName)
	{
		if (VisualTreeHelper.GetChildrenCount(control) == 0
			|| VisualTreeHelper.GetChild(control, 0) is not FrameworkElement root)
		{
			return null;
		}

		return VisualStateManager.GetVisualStateGroups(root)
			.FirstOrDefault(group => group.Name == groupName)
			?.CurrentState
			?.Name;
	}

	private sealed class ErrorSource : INotifyDataErrorInfo
	{
		private string[] _errors = Array.Empty<string>();

		public string? Value { get; set; }

		public InputValidationMode ValidationMode { get; set; } = InputValidationMode.Auto;

		public bool HasErrors => _errors.Length != 0;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public IEnumerable GetErrors(string? propertyName) => _errors;

		public void SetErrors(params string[] errors)
		{
			_errors = errors;
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
		}
	}
}

#endif
