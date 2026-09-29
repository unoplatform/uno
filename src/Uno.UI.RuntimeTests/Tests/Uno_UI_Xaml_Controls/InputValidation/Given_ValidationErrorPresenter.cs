#nullable enable

#if HAS_UNO

using System;
using System.Collections;
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
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Controls.InputValidation;

/// <summary>
/// Covers the port of <c>CControl::EnsureErrors</c> / <c>CControl::DeferErrors</c>: how the ErrorTemplate reaches
/// a template's ErrorPresenter.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ValidationErrorPresenter
{
	private static readonly ValidationErrorPresenterResources _resources = new();

	private static ControlTemplate PresenterTemplate => (ControlTemplate)_resources["PresenterTemplate"];

	private static DataTemplate ErrorTextTemplate => (DataTemplate)_resources["ErrorTextTemplate"];

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
	public async Task When_No_Errors_Then_Presenter_Stays_Deferred()
	{
		var (sut, source) = await Bind();

		Assert.IsInstanceOfType<ElementStub>(FindErrorPresenterPart(sut));

		// Clearing errors that were never there must not realize it either.
		source.SetErrors();
		await WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType<ElementStub>(FindErrorPresenterPart(sut));
	}

	[TestMethod]
	public async Task When_Errors_Arrive_Inline_Then_Template_Content_Is_Presented()
	{
		var (sut, source) = await Bind(kind: InputValidationKind.Inline);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		var presenter = GetErrorPresenter(sut);
		var content = presenter.Content as TextBlock;
		Assert.IsNotNull(content, "inline errors present the loaded ErrorTemplate directly");
		Assert.AreSame(sut, content.DataContext, "the ErrorTemplate binds against the control");
	}

	[TestMethod]
	public async Task When_Errors_Arrive_Compact_Then_Content_Is_In_The_Icon_ToolTip()
	{
		var (sut, source) = await Bind(kind: InputValidationKind.Compact);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		var presenter = GetErrorPresenter(sut);
		var icon = presenter.Content as FontIcon;
		Assert.IsNotNull(icon, "compact errors present DefaultCompactErrorIconTemplate");

		var toolTip = ToolTipService.GetToolTip(icon) as ToolTip;
		Assert.IsNotNull(toolTip);
		var content = toolTip.Content as TextBlock;
		Assert.IsNotNull(content, "the loaded ErrorTemplate is the icon's tooltip");
		Assert.AreSame(sut, content.DataContext);
	}

	[TestMethod]
	public async Task When_No_ErrorTemplate_Then_Presenter_Realized_Without_Content()
	{
		var (sut, source) = await Bind(hasErrorTemplate: false);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.IsNull(GetErrorPresenter(sut).Content);
	}

	[TestMethod]
	public async Task When_ErrorTemplate_Changes_While_In_Error_Then_Content_Replaced()
	{
		var (sut, source) = await Bind(kind: InputValidationKind.Inline);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();
		var before = GetErrorPresenter(sut).Content;

		sut.ErrorTemplate = (DataTemplate)_resources["ErrorBorderTemplate"];
		await WindowHelper.WaitForIdle();

		Assert.AreNotSame(before, GetErrorPresenter(sut).Content);
		Assert.IsInstanceOfType<Border>(GetErrorPresenter(sut).Content);
	}

	[TestMethod]
	public async Task When_Mode_Enabled_With_Existing_Errors_Then_Content_Presented()
	{
		var (sut, source) = await Bind(kind: InputValidationKind.Inline, mode: InputValidationMode.Disabled);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();
		Assert.IsInstanceOfType<ElementStub>(FindErrorPresenterPart(sut), "a control that does not participate presents nothing");

		sut.InputValidationMode = InputValidationMode.Auto;
		await WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType<TextBlock>(GetErrorPresenter(sut).Content);
	}

	[TestMethod]
	public async Task When_Template_Applied_After_Errors_Then_Content_Presented()
	{
		// Uno deviation: WinUI does not call EnsureErrors on template application, so this is what the
		// InvokeApplyTemplate anchor adds.
		var source = new ErrorSource();
		var sut = new TextBox
		{
			DataContext = source,
			InputValidationKind = InputValidationKind.Inline,
			ErrorTemplate = ErrorTextTemplate,
		};
		sut.InputValidationMode = InputValidationMode.Auto;
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		source.SetErrors("required");
		Assert.IsTrue(sut.HasValidationErrors, "the error should reach the control before any template exists");

		sut.Template = PresenterTemplate;
		await UITestHelper.Load(sut);

		Assert.IsInstanceOfType<TextBlock>(GetErrorPresenter(sut).Content);
	}

	[TestMethod]
	public async Task When_Template_Parsed_At_Runtime_Then_Content_Presented()
	{
		// Without x:Load: XamlReader realizes the presenter eagerly, so this covers a presenter that was never a stub.
		var template = (ControlTemplate)XamlReader.Load("""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Grid Width="100" Height="32">
					<ContentPresenter x:Name="ErrorPresenter" />
				</Grid>
			</ControlTemplate>
			""");
		var (sut, source) = await Bind(kind: InputValidationKind.Inline, template: template);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.IsInstanceOfType<TextBlock>(GetErrorPresenter(sut).Content);
	}

	[TestMethod]
	public async Task When_Template_Has_No_Presenter_Then_Nothing_Happens()
	{
		var (sut, source) = await Bind(template: (ControlTemplate)_resources["NoPresenterTemplate"]);

		source.SetErrors("required");
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(sut.HasValidationErrors);
	}

	private static async Task<(TextBox Sut, ErrorSource Source)> Bind(
		InputValidationKind kind = InputValidationKind.Auto,
		InputValidationMode mode = InputValidationMode.Auto,
		bool hasErrorTemplate = true,
		ControlTemplate? template = null)
	{
		var source = new ErrorSource();
		var sut = new TextBox
		{
			DataContext = source,
			Template = template ?? PresenterTemplate,
			InputValidationKind = kind,
			ErrorTemplate = hasErrorTemplate ? ErrorTextTemplate : null,
		};

		sut.InputValidationMode = mode;
		sut.SetBinding(TextBox.TextProperty, new Binding { Path = new PropertyPath(nameof(ErrorSource.Value)) });

		await UITestHelper.Load(sut);

		return (sut, source);
	}

	private static object? FindErrorPresenterPart(Control control)
		=> VisualTreeHelper.GetChild(control, 0) is Panel root
			? root.Children.OfType<FrameworkElement>().SingleOrDefault(child => child is ElementStub or ContentPresenter)
			: null;

	private static ContentPresenter GetErrorPresenter(Control control)
	{
		var part = FindErrorPresenterPart(control);
		Assert.IsInstanceOfType<ContentPresenter>(part, "the ErrorPresenter should have been realized");
		return (ContentPresenter)part;
	}

	private sealed class ErrorSource : INotifyDataErrorInfo
	{
		private string[] _errors = Array.Empty<string>();

		public string? Value { get; set; }

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
