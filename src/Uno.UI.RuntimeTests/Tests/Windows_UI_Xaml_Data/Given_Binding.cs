using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
