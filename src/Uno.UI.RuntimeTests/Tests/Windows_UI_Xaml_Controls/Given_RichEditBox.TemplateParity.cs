#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow("Border")]
	[DataRow("Grid")]
	[DataRow("ContentPresenter")]
	[DataRow("ScrollViewer")]
	[DataRow("UserControl")]
	[DataRow("Viewbox")]
	public async Task When_TemplateParity_ContentElement_Attaches_And_Detaches_View(string hostType)
	{
		var editor = new RichEditBox
		{
			Width = 300,
			Height = 100,
			Template = CreateTemplateParityTemplate(hostType),
		};
		try
		{
			editor.Document.SetText(TextSetOptions.None, "First\rSecond");
			await UITestHelper.Load(editor);
			await WindowHelper.WaitForIdle();

			var host = FindGeometryDescendant<FrameworkElement>(editor, element => element.Name == "ContentElement");
			Assert.IsNotNull(host);
			var view = GetTemplateParityContent(host);
			Assert.IsNotNull(view, $"{hostType} must host the editing view through its content property.");
			Assert.IsTrue(view.ActualWidth > 0 && view.ActualHeight > 0, "The attached view must be laid out.");
			editor.Document.GetRange(6, 12).GetRect(PointOptions.ClientCoordinates, out var rect, out _);
			Assert.IsTrue(rect.Width > 0 && rect.Height > 0, "The second paragraph must have rendered geometry.");

			editor.Document.Selection.SetRange(6, 8);
			editor.Template = CreateTemplateParityTemplate(hostType);
			editor.ApplyTemplate();
			await WindowHelper.WaitForIdle();

			Assert.IsNull(GetTemplateParityContent(host), "Retemplating must detach the old editing view.");
			if (host is Panel panel)
			{
				Assert.AreEqual(1, panel.Children.Count, "Detaching must preserve the template's other children.");
				Assert.AreEqual("TemplateDecoration", ((FrameworkElement)panel.Children[0]).Name);
			}
			var replacementHost = FindGeometryDescendant<FrameworkElement>(editor, element => element.Name == "ContentElement");
			Assert.IsNotNull(replacementHost);
			Assert.AreNotSame(host, replacementHost);
			Assert.IsNotNull(GetTemplateParityContent(replacementHost));
			GetTextWithoutFinalEop(editor.Document, out var text);
			Assert.AreEqual("First\rSecond", text);
			Assert.AreEqual(6, editor.Document.Selection.StartPosition);
			Assert.AreEqual(8, editor.Document.Selection.EndPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static ControlTemplate CreateTemplateParityTemplate(string hostType)
	{
		var content = hostType == "Grid"
			? """<Grid x:Name="ContentElement"><Border x:Name="TemplateDecoration" /></Grid>"""
			: $"""<{hostType} x:Name="ContentElement" />""";
		return (ControlTemplate)XamlReader.Load($$"""
			<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="RichEditBox">
				<Grid>{{content}}</Grid>
			</ControlTemplate>
			""");
	}

	private static FrameworkElement? GetTemplateParityContent(FrameworkElement host)
		=> host switch
		{
			Border border => border.Child as FrameworkElement,
			Panel panel => panel.Children.OfType<FrameworkElement>().FirstOrDefault(child => child.Name != "TemplateDecoration"),
			ContentControl contentControl => contentControl.Content as FrameworkElement,
			ContentPresenter presenter => presenter.Content as FrameworkElement,
			UserControl userControl => userControl.Content as FrameworkElement,
			Viewbox viewbox => viewbox.Child as FrameworkElement,
			_ => throw new InvalidOperationException($"Unexpected template host: {host.GetType().Name}."),
		};
}
