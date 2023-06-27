using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Binding
	{
		[TestMethod]
		public async Task When_DoublyNested_TemplateBinding_XamlReader()
		{
			var border = (Border)XamlReader.Load("""
				<Border x:Name="TestRoot"
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data">
				
					<local:LeftRightControl x:Name="SUT" Style="{StaticResource CustomLeftRightStyle}">
						<local:LeftRightControl.Left>
							<TextBlock x:Name="LeftRightControl_Left" Text="Left" />
						</local:LeftRightControl.Left>
					</local:LeftRightControl>
				
					<Border.Resources>
						<Style x:Key="CustomWestEastStyle" TargetType="local:WestEastControl">
							<Setter Property="Template">
								<Setter.Value>
									<ControlTemplate TargetType="local:WestEastControl">
										<StackPanel x:Name="WestEastControl_Template_RootPanel">
											<ContentControl x:Name="WestEastControl_Template_WestContent" Content="{TemplateBinding West}" />
										</StackPanel>
									</ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
						<Style x:Key="CustomLeftRightStyle" TargetType="local:LeftRightControl">
							<Setter Property="Template">
								<Setter.Value>
									<ControlTemplate TargetType="local:LeftRightControl">
										<local:WestEastControl x:Name="LeftRightControl_Template_Root" Style="{StaticResource CustomWestEastStyle}">
											<local:WestEastControl.West>
												<ContentControl x:Name="LeftRightControl_Template_LeftContent" Content="{TemplateBinding Left}" />
											</local:WestEastControl.West>
										</local:WestEastControl>
									</ControlTemplate>
								</Setter.Value>
							</Setter>
						</Style>
					</Border.Resources>
				</Border>
				""");
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitFor(() => border.IsLoaded);

			var lrTemplateParent = border.FindFirstDescendant<LeftRightControl>(x => x.Name == "SUT");
			var lrContentControl = border.FindFirstDescendant<ContentControl>(x => x.Name == "LeftRightControl_Template_LeftContent");
			var lrContentBinding = lrContentControl?.GetBindingExpression(ContentControl.ContentProperty);

			Assert.IsNotNull(lrContentBinding, "nested 1");
			Assert.AreEqual(lrTemplateParent, lrContentBinding.DataContext, "nested 1");

			var weTemplateParent = border.FindFirstDescendant<WestEastControl>(x => x.Name == "LeftRightControl_Template_Root");
			var weContentControl = border.FindFirstDescendant<ContentControl>(x => x.Name == "WestEastControl_Template_WestContent");
			var weContentBinding = weContentControl?.GetBindingExpression(ContentControl.ContentProperty);

			Assert.IsNotNull(weContentBinding, "nested 2");
			Assert.AreEqual(weTemplateParent, weContentBinding.DataContext, "nested 2");
		}

		[TestMethod]
		public async Task When_TemplatedParent_PropagatedCrossPopup()
		{
			var combo = new ComboBox() { ItemsSource = Enumerable.Range(0, 100).ToArray() };
			WindowHelper.WindowContent = combo;
			await WindowHelper.WaitForLoaded(combo);

			combo.IsDropDownOpen = true;
			await WindowHelper.WaitForIdle();

			if (!combo.GetItemsPanelChildren().Any())
			{
				Assert.Fail("ComboBox Panel didnt not contain any child. Likely, the ItemsControl-ItemsPresenter-Panel association failed somewhere between due to Popup not being considered during visual-tree traversal.");
			}
		}

		[TestMethod] public Task When_ContentControl_Content_AsdAsd31() => When_ContentControl_Content_AsdAsd3("ContentControl", null, null);
		[TestMethod] public Task When_ContentControl_Content_AsdAsd32() => When_ContentControl_Content_AsdAsd3("ContentControl", "ContentPresenter", "ImplicitContent");
		[TestMethod] public Task When_ContentControl_Content_AsdAsd33() => When_ContentControl_Content_AsdAsd3("ContentControl", "ContentPresenter", "TemplateBinding");
		[TestMethod] public Task When_ContentControl_Content_AsdAsd34() => When_ContentControl_Content_AsdAsd3("ContentControl", "ContentPresenter", "RelativeTemplateBinding");
		[TestMethod] public Task When_ContentControl_Content_AsdAsd35() => When_ContentControl_Content_AsdAsd3("Button", "ContentPresenter", "ImplicitContent");

		[TestMethod]
		[DataRow("ContentControl", null, null)]
		[DataRow("ContentControl", "ContentPresenter", "ImplicitContent")]
		[DataRow("ContentControl", "ContentPresenter", "TemplateBinding")]
		[DataRow("ContentControl", "ContentPresenter", "RelativeTemplateBinding")]
		[DataRow("Button", "ContentPresenter", "ImplicitContent")]
		public async Task When_ContentControl_Content_AsdAsd3(string control, string templateRoot, string content)
		{
			string GetTemplate() => templateRoot switch
			{
				null => null,
				"ContentPresenter" => $"""
					<{control}.Template>
						<ControlTemplate TargetType="{control}">
							<{templateRoot} x:Name="DebugMarker" {GetContent()} />
						</ControlTemplate>
					</{control}.Template>
				""",

				_ => throw new ArgumentOutOfRangeException(nameof(templateRoot), templateRoot),
			};
			string GetContent() => content switch
			{
				"ImplicitContent" => null,
				"TemplateBinding" => "Content=\"{TemplateBinding Content}\"",
				"RelativeTemplateBinding" => "Content=\"{Binding Content, RelativeSource={RelativeSource TemplatedParent}}\"",

				_ => throw new ArgumentOutOfRangeException(nameof(content), content),
			};
			var xaml = GetTemplate() is not string template
				? $$"""<{{control}} Content="{Binding}" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" />"""
				: $$"""
					<{{control}} Content="{Binding}" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					{{template}}
					</{{control}}>
					""";
			var setup = (ContentControl)XamlReader.Load(xaml);
			setup.DataContext = "asd";
			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitFor(() => setup.IsLoaded, message: $"Timeout waiting for {control} to be loaded");

			var sut = setup.FindFirstDescendant<ContentPresenter>();
			var cpContentBinding = setup.GetBindingExpression(ContentPresenter.ContentProperty);

			var itb = sut.FindFirstDescendant<ImplicitTextBlock>();
			var tree = setup.TreeGraph();
		}

		// todo@xy: add test case against uno#7497
	}


	public partial class LeftRightControl : Control
	{
		#region DependencyProperty: Left

		public static DependencyProperty LeftProperty { get; } = DependencyProperty.Register(
			nameof(Left),
			typeof(UIElement),
			typeof(LeftRightControl),
			new PropertyMetadata(default(UIElement)));

		public UIElement Left
		{
			get => (UIElement)GetValue(LeftProperty);
			set => SetValue(LeftProperty, value);
		}

		#endregion
	}
	public partial class WestEastControl : Control
	{
		#region DependencyProperty: West

		public static DependencyProperty WestProperty { get; } = DependencyProperty.Register(
			nameof(West),
			typeof(UIElement),
			typeof(WestEastControl),
			new PropertyMetadata(default(UIElement)));

		public UIElement West
		{
			get => (UIElement)GetValue(WestProperty);
			set => SetValue(WestProperty, value);
		}

		#endregion
	}
}
