using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_Binding // rename to Given_TemplatedParent and move under: ..\Windows_UI_Xaml\
	{
		// todo@xy: add test case against uno#7497

		[TestMethod]
		public async Task When_DoublyNested_TemplateBinding_XamlReader_AsdAsd11()
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
		public async Task When_Interweaved_Setup_XamlReader_AsdAsd12()
		{
			// test case against uno#7497 (simplified)
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl Content="asd">
					<ContentControl.Template>
						<ControlTemplate TargetType="ContentControl">
							<Pivot>
								<PivotItem Header="P1">
									<TextBlock Text="{TemplateBinding Content}" />
								</PivotItem>
								<Pivot.Template>
									<ControlTemplate TargetType="Pivot">
										<ItemsPresenter x:Name="PivotItemPresenter" />
									</ControlTemplate>
								</Pivot.Template>
							</Pivot>
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");
			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitFor(() => setup.IsLoaded);

			//winui expectations:
			//	ContentControl // TemplatedParent=null
			//		Pivot // TemplatedParent=ContentControl
			//			ItemsPresenter#PivotItemPresenter // TemplatedParent=Pivot
			//				! ContentControl // TemplatedParent=null		(#10745: missing ItemsControl::Header+Footer impl)
			//				Grid // TemplatedParent=ItemsPresenter
			//					PivotItem // TemplatedParent=ContentControl
			//						Grid // TemplatedParent=PivotItem
			//							ContentPresenter // TemplatedParent=PivotItem
			//								TextBlock // TemplatedParent=ContentControl
			//				! ContentControl // TemplatedParent=null		(#10745: missing ItemsControl::Header+Footer impl)
			setup.ValidateVisualSubtree(
				new AssertTemplatedParent<ContentControl>(children:
					new AssertTemplatedParent<Pivot>(tp: (typeof(ContentControl), 1), children:
						new AssertTemplatedParent<ItemsPresenter>("PivotItemPresenter", tp: (typeof(Pivot), 1), children:
							new AssertTemplatedParent<Grid>(tp: (typeof(ItemsPresenter), 1), children:
								new AssertTemplatedParent<PivotItem>(tp: (typeof(ContentControl), 4), children:
									new AssertTemplatedParent<Grid>(tp: (typeof(PivotItem), 1), children:
										new AssertTemplatedParent<ContentPresenter>(tp: (typeof(PivotItem), 2), children:
											new AssertTemplatedParent<TextBlock>(tp: (typeof(ContentControl), 7))
			))))))));
		}

		[TestMethod]
		public async Task When_TemplatedParent_PropagatedCrossPopup_AsdAsd2()
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

		[TestMethod]
		[DataRow("ContentControl", null, null)]
		[DataRow("ContentControl", "ContentPresenter", "ImplicitContent")]
		[DataRow("ContentControl", "ContentPresenter", "TemplateBinding")]
		[DataRow("ContentControl", "ContentPresenter", "RelativeTemplateBinding")]
		[DataRow("Button", "ContentPresenter", "ImplicitContent")]
		public async Task When_ContentControl_Content_AsdAsd31(string control, string templateRoot, string content)
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
				? $$"""<{{control}} Content="{Binding}"/>"""
				: $$"""
					<{{control}} Content="{Binding}">
					{{template}}
					</{{control}}>
					""";
			var setup = XamlHelper.LoadXaml<ContentControl>(xaml);
			setup.DataContext = "asd";
			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitFor(() => setup.IsLoaded, message: $"Timeout waiting for {control} to be loaded");

			var sut = setup.FindFirstDescendant<ContentPresenter>();
			var itb = sut?.FindFirstDescendant<ImplicitTextBlock>();

			Assert.IsNotNull(itb, "ImplicitTextBlock not found");
			Assert.AreEqual(itb?.Text, "asd");
		}

		[TestMethod]
		public async Task When_ContentControl_ContentTemplate_AsdAsd32()
		{
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl Content="asd">
					<ContentControl.ContentTemplate>
						<DataTemplate>
							<StackPanel x:Name="T1Panel">
								<Border x:Name="T2BorderA" Width="1" Height="1" />
								<Border x:Name="T2BorderB" Width="1" Height="1" />
							</StackPanel>
						</DataTemplate>
					</ContentControl.ContentTemplate>
					<ContentControl.Template>
						<ControlTemplate>
							<ContentPresenter x:Name="TemplateRoot" ContentTemplate="{TemplateBinding ContentTemplate}" />
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");

			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);

			// winui expectation: // this should hold true for both XamlCodeGeneration and XamlReader.Load
			//	ContentControl // TemplatedParent=null
			//		ContentPresenter#TemplateRoot // TemplatedParent=ContentControl
			//			StackPanel#T1Panel // TemplatedParent=ContentPresenter
			//				Border#T2BorderA // TemplatedParent=ContentPresenter
			//				Border#T2BorderB // TemplatedParent=ContentPresenter
			ValidateSubtreeTemplatedParents(setup, new Dictionary<string, (Type Type, Func<FrameworkElement, DependencyObject> ValueGetter)?>
			{
				["TemplateRoot"] = (typeof(ContentControl), VisualTreeHelper.GetParent),
				["T1Panel"] = (typeof(ContentPresenter), VisualTreeHelper.GetParent),
				["T2BorderA"] = (typeof(ContentPresenter), x => VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(x))),
				["T2BorderB"] = (typeof(ContentPresenter), x => VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(x))),
			});
		}

		[TestMethod]
		public async Task When_ContentControl_ContentTemplate_LateClear_AsdAsd331()
		{
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl Content="asd">
					<ContentControl.ContentTemplate>
						<DataTemplate>
							<TextBlock x:Name="ContentRoot" Text="{Binding}" />
						</DataTemplate>
					</ContentControl.ContentTemplate>
					<ContentControl.Template>
						<ControlTemplate>
							<ContentPresenter x:Name="TemplateRoot" ContentTemplate="{TemplateBinding ContentTemplate}" />
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");
			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);
			setup.ValidateVisualSubtree(
				new AssertTemplatedParent<ContentControl>(children:
					new AssertTemplatedParent<ContentPresenter>("TemplateRoot", (typeof(ContentControl), 1), children:
						new AssertTemplatedParent<TextBlock>("ContentRoot", (typeof(ContentPresenter), 1))
			)));

			setup.ContentTemplate = null;
			await WindowHelper.WaitForIdle();
			setup.ValidateVisualSubtree(
				new AssertTemplatedParent<ContentControl>(children:
					new AssertTemplatedParent<ContentPresenter>("TemplateRoot", (typeof(ContentControl), 1), children:
						new AssertTemplatedParent<ImplicitTextBlock>(null, (typeof(ContentPresenter), 1))
			)));

			setup.ContentTemplate = XamlHelper.LoadXaml<DataTemplate>("""
				<DataTemplate>
					<TextBlock x:Name="ContentRoot" Text="{Binding}" />
				</DataTemplate>
			""");
			await WindowHelper.WaitForIdle();
			setup.ValidateVisualSubtree(
				new AssertTemplatedParent<ContentControl>(children:
					new AssertTemplatedParent<ContentPresenter>("TemplateRoot", (typeof(ContentControl), 1), children:
						new AssertTemplatedParent<TextBlock>("ContentRoot", (typeof(ContentPresenter), 1))
			)));
		}

		[TestMethod]
		public async Task When_ItemsControl_ItemPanel_And_ItemTemplate_AsdAsd41()
		{
			var setup = XamlHelper.LoadXaml<ItemsControl>("""
				<ItemsControl>
					<ItemsControl.ItemTemplate>
						<DataTemplate>
							<StackPanel x:Name="T1Panel">
								<Border x:Name="T2BorderA" Width="1" Height="1" />
								<Border x:Name="T2BorderB" Width="1" Height="1" />
							</StackPanel>
						</DataTemplate>
					</ItemsControl.ItemTemplate>
					<ItemsControl.ItemsPanel>
						<ItemsPanelTemplate>
							<ItemsStackPanel x:Name="ItemPanelRoot" />
						</ItemsPanelTemplate>
					</ItemsControl.ItemsPanel>
					<ItemsControl.Template>
						<ControlTemplate>
							<ItemsPresenter x:Name="TemplateRoot" />
						</ControlTemplate>
					</ItemsControl.Template>
				</ItemsControl>
			""");
			setup.ItemsSource = new[] { 0 };

			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);

			/* winui expectation: // this should hold true for both XamlCodeGeneration and XamlReader.Load
			 *	ItemsControl // TemplatedParent=null
			 *		ItemsPresenter#TemplateRoot // TemplatedParent=ItemsControl
			 *			ContentControl // TemplatedParent=null
			 *			ItemsStackPanel#ItemPanelRoot // TemplatedParent=ItemsPresenter
			 *				ContentPresenter // TemplatedParent=null
			 *					StackPanel#T1Panel // TemplatedParent=ContentPresenter
			 *						Border#T2BorderA // TemplatedParent=ContentPresenter
			 *						Border#T2BorderB // TemplatedParent=ContentPresenter
			 *			ContentControl // TemplatedParent=null
			 */
			var expectations = new Dictionary<string, (Type Type, Func<FrameworkElement, DependencyObject> ValueGetter)?>()
			{
				["TemplateRoot"] = (typeof(ItemsControl), _ => setup),
				["ItemPanelRoot"] = (typeof(ItemsPresenter), x => VisualTreeHelper.GetParent(x)),
				["T1Panel"] = (typeof(ContentPresenter), x => VisualTreeHelper.GetParent(x)),
				["T2BorderA"] = (typeof(ContentPresenter), x => VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(x))),
				["T2BorderB"] = (typeof(ContentPresenter), x => VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(x))),
			};
			foreach (var node in setup.EnumerateAllChildren().OfType<FrameworkElement>())
			{
				var templatedParent = node.GetTemplatedParent();
				var expectation = (node.Name is { } xname && expectations.TryGetValue(xname, out var result)) ? result : default;
				var expected = expectation?.ValueGetter(node);

				var msg = $"Unexpected templated-parent ({GetDebugLabel(templatedParent)} vs [expected]{GetDebugLabel(expected)}) on node at path: {GetXPath(node, fromInclusive: setup)}";
				if (expectation?.Type is { } type)
				{
					Assert.IsInstanceOfType(templatedParent, type, msg);
				}
				Assert.AreEqual(expected, templatedParent, msg);
			}
		}

		[TestMethod]
		public async Task When_ItemsRepeater_ItemTemplate_AsdAsd42()
		{
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl>
					<ContentControl.Template>
						<ControlTemplate>
							<muxc:ItemsRepeater ItemsSource="0">
								<muxc:ItemsRepeater.ItemTemplate>
									<DataTemplate>
										<TextBlock Text="asd" />
									</DataTemplate>
								</muxc:ItemsRepeater.ItemTemplate>
							</muxc:ItemsRepeater>
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");

			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);

			var ir = setup.FindFirstDescendant<ItemsRepeater>();
			Assert.IsNotNull(ir, "ItemsRepeater not found");
			Assert.AreEqual(setup, ir.GetTemplatedParent(), "ItemsRepeater's templated-parent should be the ContentControl");

			var iroot0 = ir.TryGetElement(0);
			Assert.IsInstanceOfType(iroot0, typeof(TextBlock), "root element #0 should be a TextBlock");
			Assert.IsNull((iroot0 as FrameworkElement).GetTemplatedParent(), "child of ItemsRepeater should not have a templated-parent.");
		}

		[TestMethod]
		public async Task When_XamlMember_Nesting_AsdAsd51()
		{
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl>
					<ContentControl.Template>
						<ControlTemplate>
							<Border x:Name="T1Border">
								<Border x:Name="T2Border" Width="1" Height="1" />
							</Border>
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");

			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);

			var t1 = setup.FindFirstDescendant<Border>(x => x.Name == "T1Border");
			Assert.IsNotNull(t1, "T1Border not found");
			Assert.AreEqual(setup, t1.GetTemplatedParent(), "T1Border templated-parent should be the ContentControl");

			var t2 = setup.FindFirstDescendant<Border>(x => x.Name == "T2Border");
			Assert.IsNotNull(t2, "T2Border not found");
			Assert.AreEqual(setup, t2.GetTemplatedParent(), "T2Border templated-parent should be the ContentControl");
		}

		[TestMethod]
		public async Task When_XamlCollectionItem_Nesting_AsdAsd52()
		{
			// XamlReader specific test: AddCollectionItems
			var setup = XamlHelper.LoadXaml<ContentControl>("""
				<ContentControl>
					<ContentControl.Template>
						<ControlTemplate>
							<StackPanel x:Name="T1Panel">
								<Border x:Name="T2BorderA" Width="1" Height="1" />
								<Border x:Name="T2BorderB" Width="1" Height="1" />
							</StackPanel>
						</ControlTemplate>
					</ContentControl.Template>
				</ContentControl>
			""");

			WindowHelper.WindowContent = setup;
			await WindowHelper.WaitForLoaded(setup);

			foreach (var xname in "T1Panel,T2BorderA,T2BorderB".Split(','))
			{
				var target = setup.FindFirstDescendant<FrameworkElement>(x => x.Name == xname);
				Assert.IsNotNull(target, $"{xname} not found");
				Assert.AreEqual(setup, target.GetTemplatedParent(), $"{xname} templated-parent should be the ContentControl");
			}
		}
	}

	public partial class Given_Binding
	{
		private static void ValidateSubtreeTemplatedParents(UIElement branch, Dictionary<string, (Type Type, Func<FrameworkElement, DependencyObject> ValueGetter)?> expectations)
		{
			foreach (var node in branch.EnumerateAllChildren().OfType<FrameworkElement>())
			{
				var templatedParent = node.GetTemplatedParent();
				var expectation = (node.Name is { } xname && expectations.TryGetValue(xname, out var result)) ? result : default;
				var expected = expectation?.ValueGetter(node);

				var msg = $"Unexpected templated-parent ({GetDebugLabel(templatedParent)} vs [expected]{GetDebugLabel(expected)}) on node at path: {GetXPath(node, fromInclusive: branch)}";
				if (expectation?.Type is { } type)
				{
					Assert.IsInstanceOfType(templatedParent, type, msg);
				}
				Assert.AreEqual(expected, templatedParent, msg);
			}
		}

		private static string GetXPath(UIElement uie, UIElement fromInclusive = null)
		{
			return string.Join(@"\", GetNodes()
				.Reverse()
				.Select(GetDebugLabel)
			);

			IEnumerable<DependencyObject> GetNodes()
			{
				yield return uie;
				foreach (var node in VisualTreeHelper.EnumerateAncestors(uie))
				{
					yield return node;
					if (fromInclusive != null && node == fromInclusive)
					{
						break;
					}
				}
			}
		}

		private static string GetDebugLabel(DependencyObject x) =>
			x is FrameworkElement { Name: { Length: > 0 } xname }
				? $"{x.GetType().Name}#{xname}"
				: (x?.GetType().Name ?? "<null>");

		internal class AssertTemplatedParent<T> : VisualNodeAssert<T> where T : DependencyObject
		{
			private readonly (Type Type, int RelativeLevel)? _templatedParentInfo;

			public AssertTemplatedParent(string xname = null, (Type Type, int RelativeLevel)? tp = null, params IVisualNodePredicate[] children) : base(xname, children)
			{
				_templatedParentInfo = tp;
			}

			protected override void ValidateCore(T node)
			{
				var templatedParent = (node as FrameworkElement)?.GetTemplatedParent();
				if (_templatedParentInfo is { } info)
				{
					var expected = Enumerable.Range(0, info.RelativeLevel)
						.Aggregate((DependencyObject)node, (acc, _) => VisualTreeHelper.GetParent(acc));

					Assert.IsInstanceOfType(templatedParent, info.Type);
					Assert.AreEqual(expected, templatedParent);
				}
				else
				{
					Assert.IsNull(templatedParent);
				}
			}
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
