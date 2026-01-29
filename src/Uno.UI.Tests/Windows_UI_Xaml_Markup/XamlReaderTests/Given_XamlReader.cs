using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using SwipeItems = Microsoft.UI.Xaml.Controls.SwipeItems;
using SwipeControl = Microsoft.UI.Xaml.Controls.SwipeControl;
using SwipeMode = Microsoft.UI.Xaml.Controls.SwipeMode;
using Uno.UI.Tests.Helpers;
using System.Numerics;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	[TestClass]
	public class Given_XamlReader : Context
	{
		[TestInitialize]
		public void Initialize()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_BasicRoot()
		{
			var s = GetContent(nameof(When_BasicRoot));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

			Assert.IsNotNull(r);
			Assert.AreEqual("testPage", r.Name);
		}

		[TestMethod]
		public void When_BasicProperty()
		{
			var s = GetContent(nameof(When_BasicProperty));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);
			Assert.AreEqual("testPage", r.Name);
			Assert.AreEqual(42.0, r.Width);
		}

		[TestMethod]
		public void When_UserControl_With_Content()
		{
			var s = GetContent(nameof(When_UserControl_With_Content));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);
			Assert.AreEqual("testPage", r.Name);

			var stackPanel = r.Content as StackPanel;
			Assert.IsNotNull(stackPanel);
			Assert.AreEqual(Orientation.Horizontal, stackPanel.Orientation);
		}

		[TestMethod]
		public void When_UserControl_With_Grid()
		{
			var s = GetContent(nameof(When_UserControl_With_Grid));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);
			Assert.AreEqual("testPage", r.Name);

			var grid = r.Content as Grid;
			Assert.IsNotNull(grid);

			Assert.AreEqual(2, grid.Children.Count);

			var border1 = grid.Children.ElementAt(0) as Border;
			var border2 = grid.Children.ElementAt(1) as Border;

			Assert.AreEqual((border1.Background as SolidColorBrush).Color, Microsoft.UI.Colors.Red);
			Assert.AreEqual((border2.Background as SolidColorBrush).Color, Microsoft.UI.Colors.Blue);
		}

		[TestMethod]
		public void When_MultipleBindings()
		{
			var s = GetContent(nameof(When_MultipleBindings));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);
			Assert.AreEqual("rootPage", r.Name);

			var grid = r.FindName("rootGrid") as Grid;
			var page = r.FindName("rootPage") as Page;
			var listView = r.FindName("WrapPanelContainer") as ListView;

			Assert.IsNotNull(grid);
			Assert.IsNotNull(page);
			Assert.IsNotNull(listView);

			var itemsPanel = listView?.ItemsPanel;
			Assert.IsNotNull(itemsPanel);

			var content = ((IFrameworkTemplateInternal)itemsPanel).LoadContent(listView) as StackPanel;
			Assert.IsNotNull(content);
			Assert.AreEqual("InnerStackPanel", content.Name);

			var template = page.Resources["PhotoTemplate"] as DataTemplate;
			Assert.IsNotNull(template);

			var photoTemplateContent = template.LoadContent() as FrameworkElement;
			Assert.IsNotNull(photoTemplateContent);

			var border01 = photoTemplateContent.FindName("border01") as Border;
			Assert.IsNotNull(border01);
			var stops = (border01.Background as LinearGradientBrush).GradientStops;
			Assert.AreEqual(2, stops.Count);
			Assert.AreEqual(Microsoft.UI.Colors.Transparent, stops[0].Color);
			Assert.AreEqual(Microsoft.UI.ColorHelper.FromARGB(0x33, 0, 0, 0), stops[1].Color);
			Assert.AreEqual(0.0, stops[0].Offset);
			Assert.AreEqual(1.0, stops[1].Offset);

			var textBlock01 = photoTemplateContent.FindName("textBlock01") as TextBlock;
			Assert.IsNotNull(textBlock01);
			var textBlockExpression = textBlock01.GetBindingExpression(TextBlock.TextProperty);
			Assert.AreEqual("Category", textBlockExpression.ParentBinding.Path.Path);

			var photoTemplateRootGrid = photoTemplateContent.FindName("PhotoTemplateRootGrid") as Grid;
			Assert.IsNotNull(photoTemplateRootGrid);

			var widthExpression = photoTemplateRootGrid.GetBindingExpression(Grid.WidthProperty);
			Assert.IsNotNull(widthExpression);
			Assert.AreEqual("Width", widthExpression.ParentBinding.Path.Path);

			var heightExpression = photoTemplateRootGrid.GetBindingExpression(Grid.HeightProperty);
			Assert.IsNotNull(heightExpression);
			Assert.AreEqual("Height", heightExpression.ParentBinding.Path.Path);

			var photoTemplateImage = photoTemplateContent.FindName("PhotoTemplateImage") as Image;
			Assert.IsNotNull(photoTemplateImage);

			var uriSourceExpression = photoTemplateImage.Source.GetBindingExpression(Microsoft.UI.Xaml.Media.Imaging.BitmapImage.UriSourceProperty);
			Assert.IsNotNull(uriSourceExpression);
			Assert.AreEqual("Thumbnail", uriSourceExpression.ParentBinding.Path.Path);
		}

		[TestMethod]
		public void When_AttachedProperty_Different_Target()
		{
			var s = GetContent(nameof(When_AttachedProperty_Different_Target));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);
			Assert.AreEqual("rootPage", r.Name);

			var stackPanel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			Assert.AreEqual(42, Grid.GetRow(stackPanel));
		}

		[TestMethod]
		public void When_AttachedProperty_Same_Target()
		{
			var s = GetContent(nameof(When_AttachedProperty_Same_Target));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var grid = r.FindName("rootGrid") as Grid;
			Assert.IsNotNull(grid);

			Assert.AreEqual(42, Grid.GetRow(grid));
		}

		[TestMethod]
		public void When_AttachedProperty_Binding()
		{
			var s = GetContent(nameof(When_AttachedProperty_Binding));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var stackPanel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			var expression = stackPanel.GetBindingExpression(Grid.RowProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyRow", expression.ParentBinding.Path.Path);
		}

		[TestMethod]
		public void When_Binding_TwoWay()
		{
			var s = GetContent(nameof(When_Binding_TwoWay));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var stackPanel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			var expression = stackPanel.GetBindingExpression(StackPanel.OrientationProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyOrientation", expression.ParentBinding.Path.Path);
			Assert.AreEqual(Microsoft.UI.Xaml.Data.BindingMode.TwoWay, expression.ParentBinding.Mode);

			var expressionWidth = stackPanel.GetBindingExpression(StackPanel.WidthProperty);
			Assert.IsNotNull(expressionWidth);
			Assert.AreEqual("MyWidth", expressionWidth.ParentBinding.Path.Path);
			Assert.AreEqual(Microsoft.UI.Xaml.Data.BindingMode.OneTime, expressionWidth.ParentBinding.Mode);
		}

		[TestMethod]
		public void When_StaticResource()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			app.Resources["StaticRow"] = 42;
			app.Resources["StaticWidth"] = 42.0;
			app.Resources["StaticHeight"] = 44.0;

			var s = GetContent(nameof(When_StaticResource));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var panel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(panel);

			Assert.AreEqual(42, Grid.GetRow(panel));
			Assert.AreEqual(42.0, panel.Width);
			Assert.AreEqual(44.0, panel.Height);

			app.Resources.Remove("StaticRow");
			app.Resources.Remove("StaticWidth");
			app.Resources.Remove("StaticHeight");
		}

		[TestMethod]
		public void When_ThemeResource()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			if (app.Resources.ThemeDictionaries.TryGetValue("Light", out var themeDictionary)
				&& themeDictionary is ResourceDictionary dictionary)
			{
				dictionary["StaticRow"] = 42;
				dictionary["StaticWidth"] = 42.0;
				dictionary["StaticHeight"] = 44.0;
			}

			var s = GetContent(nameof(When_ThemeResource));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var panel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(panel);

			Assert.AreEqual(42, Grid.GetRow(panel));
			Assert.AreEqual(42.0, panel.Width);
			Assert.AreEqual(44.0, panel.Height);

			if (app.Resources.ThemeDictionaries.TryGetValue("Light", out var themeDictionary2)
				&& themeDictionary is ResourceDictionary dictionary2)
			{
				dictionary2.Remove("StaticRow");
				dictionary2.Remove("StaticWidth");
				dictionary2.Remove("StaticHeight");
			}
		}

		[TestMethod]
		public void When_ThemeResource_Lazy()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var s = GetContent(nameof(When_ThemeResource_Lazy));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var panel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(panel);

			r.ForceLoaded();

			Assert.AreEqual(42, Grid.GetRow(panel));
			Assert.AreEqual(42.0, panel.Width);
			Assert.AreEqual(44.0, panel.Height);
		}

		[TestMethod]
		public void When_CustomResource()
		{
			var savedState = Microsoft.UI.Xaml.Resources.CustomXamlResourceLoader.Current;
			try
			{
				var loader = new TestCustomXamlResourceLoader();
				var resourceValue = "This is a CustomResource.";
				var resourceKey = "myCustomResource";
				loader.TestCustomResources[resourceKey] = resourceValue;
				Microsoft.UI.Xaml.Resources.CustomXamlResourceLoader.Current = loader;

				var s = GetContent(nameof(When_CustomResource));
				var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

				Assert.IsNotNull(r);

				var tb = r.Content as TextBlock;

				Assert.IsNotNull(tb);
				Assert.AreEqual(resourceValue, tb.Text);

				var objectType = tb.GetType().FullName;
				var propertyName = nameof(tb.Text);
				var propertyType = tb.Text.GetType().FullName;

				Assert.AreEqual(loader.LastResourceId, resourceKey);
				Assert.AreEqual(loader.LastObjectType, objectType);
				Assert.AreEqual(loader.LastPropertyName, propertyName);
				Assert.AreEqual(loader.LastPropertyType, propertyType);
			}
			finally
			{
				Microsoft.UI.Xaml.Resources.CustomXamlResourceLoader.Current = savedState;
			}
		}

		[TestMethod]
		public void When_TextBlock_Basic()
		{
			var s = GetContent(nameof(When_TextBlock_Basic));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var tb01 = r.FindName("tb01") as TextBlock;
			Assert.IsNotNull(tb01);
			Assert.AreEqual("My Text 01", tb01.Text);

			var tb02 = r.FindName("tb02") as TextBlock;
			Assert.IsNotNull(tb02);
			var tb02_run = tb02.Inlines.FirstOrDefault() as Microsoft.UI.Xaml.Documents.Run;
			Assert.AreEqual("My Text 02", tb02_run.Text);

			var tb03 = r.FindName("tb03") as TextBlock;
			Assert.IsNotNull(tb03);
			var tb03_run = tb03.Inlines.FirstOrDefault() as Microsoft.UI.Xaml.Documents.Run;
			Assert.AreEqual("My Run Text", tb03_run.Text);
		}

		[TestMethod]
		public void When_ElementName()
		{
			var s = GetContent(nameof(When_ElementName));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var stackPanel = r.FindName("rootPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			var textBlock = r.FindName("innerTextBlock") as TextBlock;
			Assert.IsNotNull(textBlock);

			var expression = textBlock.GetBindingExpression(TextBlock.WidthProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("Width", expression.ParentBinding.Path.Path);
			Assert.IsInstanceOfType(expression.ParentBinding.ElementName, typeof(ElementNameSubject));
			Assert.AreEqual(stackPanel, ((ElementNameSubject)expression.ParentBinding.ElementName).ElementInstance);
			Assert.AreEqual(42.0, textBlock.Width);
		}

		[TestMethod]
		public void When_Empty_ResourceDictionary_As_Resources()
		{
			var s = GetContent(nameof(When_Empty_ResourceDictionary_As_Resources));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

			Assert.IsNotNull(r);
			Assert.IsNotNull(r.Resources);
			Assert.IsEmpty(r.Resources);
		}

		[TestMethod]
		public void When_Empty_ThemeDictionaries()
		{
			var s = GetContent(nameof(When_Empty_ThemeDictionaries));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

			Assert.IsNotNull(r);
			Assert.IsNotNull(r.Resources);
			Assert.IsEmpty(r.Resources);
			Assert.IsNotNull(r.Resources.ThemeDictionaries);
			Assert.AreEqual(2, r.Resources.ThemeDictionaries.Count);
		}

		[TestMethod]
		public void When_ContentControl_ControlTemplate()
		{
			var s = GetContent(nameof(When_ContentControl_ControlTemplate));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var innerContent = r.Content as ContentControl;
			innerContent.ApplyTemplate();

			var tb = innerContent.GetTemplateChild("PART_Root") as TextBlock;
			Assert.IsNotNull(tb);

			Assert.AreEqual("42", tb.Text);
		}

		[TestMethod]
		public void When_Style_ControlTemplate()
		{
			var s = GetContent(nameof(When_Style_ControlTemplate));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
			Assert.IsNotNull(r);

			r.ForceLoaded();

			var innerContent = r.Content as ContentControl;
			Assert.IsNotNull(innerContent);

			innerContent.ApplyTemplate();

			var tb = innerContent.GetTemplateChild("PART_root") as TextBlock;
			Assert.IsNotNull(tb);

			var textBinding = tb.GetBindingExpression(TextBlock.TextProperty);
			Assert.IsNotNull(textBinding);
			Assert.AreEqual("Text", textBinding.TargetName);
			Assert.AreEqual("Content", textBinding.ParentBinding.Path.Path);
			Assert.AreEqual("test", tb.Text);
			Assert.AreEqual("42", tb.Tag);
		}

		[TestMethod]
		public void When_VisualStateGroup_AttachedProperty()
		{
			var s = GetContent(nameof(When_VisualStateGroup_AttachedProperty));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var border1 = r.FindName("border1") as Border;
			r.ForceLoaded();
			Assert.AreEqual(0, Grid.GetRow(border1));

			Window.Current.SetWindowSize(new Windows.Foundation.Size(721, 100));

			Assert.AreEqual(1, Grid.GetRow(border1));
		}

		[TestMethod]
		public void When_VisualStateGroup()
		{
			var s = GetContent(nameof(When_VisualStateGroup));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Grid;
			r.ForceLoaded();

			var groups = VisualStateManager.GetVisualStateGroups(r);
			Assert.IsNotNull(groups);

			var g = groups.FirstOrDefault();
			Assert.IsNotNull(g);

			var state = g.States.FirstOrDefault();
			Assert.IsNotNull(state);

			var at = state.StateTriggers.FirstOrDefault() as AdaptiveTrigger;
			Assert.IsNotNull(at);
			Assert.AreEqual(720, at.MinWindowWidth);

			var setter = state.Setters.FirstOrDefault() as Setter;
			Assert.IsNotNull(setter);
			Assert.AreEqual("Orientation", setter.Target.Path.Path);

			Assert.AreEqual("Horizontal", setter.Value);
			// TODO: This assert is flaky.
			//Assert.IsNull(setter.Target.Target);

			// Force a size change, otherwise setter.Target.Target won't get evaluated
			Window.Current.SetWindowSize(new Windows.Foundation.Size(719, 100));
			Window.Current.SetWindowSize(new Windows.Foundation.Size(721, 100));

			Assert.IsNotNull(setter.Target.Target);

			var myPanel = setter.Target.Target as StackPanel;
			Assert.IsNotNull(myPanel);
			Assert.AreEqual("myPanel", myPanel.Name);
			Assert.AreEqual(Orientation.Horizontal, myPanel.Orientation);

			Window.Current.SetWindowSize(new Windows.Foundation.Size(719, 100));
			Assert.AreEqual(Orientation.Vertical, myPanel.Orientation);
		}

		[TestMethod]
		public void When_XNull()
		{
			var s = GetContent(nameof(When_XNull));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as Grid;
			var innerPanel = r.FindName("innerPanel") as StackPanel;

			Assert.AreEqual("42", root.DataContext);
			Assert.IsNull(innerPanel.DataContext);
		}

		[TestMethod]
		public void When_Binding_TwoWay_UpdateSourceTrigger()
		{
			var s = GetContent(nameof(When_Binding_TwoWay_UpdateSourceTrigger));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var innerPanel = r.FindName("innerPanel") as StackPanel;

			var expression = innerPanel.GetBindingExpression(StackPanel.OrientationProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyOrientation", expression.ParentBinding.Path.Path);
			Assert.AreEqual(Microsoft.UI.Xaml.Data.BindingMode.TwoWay, expression.ParentBinding.Mode);
			Assert.AreEqual(Microsoft.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged, expression.ParentBinding.UpdateSourceTrigger);

			Assert.IsNull(innerPanel.DataContext);
		}

		[TestMethod]
		public void When_Binding_TargetNull()
		{
			var s = GetContent(nameof(When_Binding_TargetNull));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var innerPanel = r.FindName("innerPanel") as StackPanel;

			var expression = innerPanel.GetBindingExpression(StackPanel.TagProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyOrientation", expression.ParentBinding.Path.Path);
			Assert.AreEqual("42", expression.ParentBinding.TargetNullValue);
			Assert.AreEqual("test", expression.ParentBinding.FallbackValue);

			Assert.IsNull(innerPanel.DataContext);
		}

		[TestMethod]
		public void When_TextBlock_ImplicitRun()
		{
			var s = GetContent(nameof(When_TextBlock_ImplicitRun));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tb1 = r.FindName("tb01") as TextBlock;
			var link = tb1.Inlines.OfType<Hyperlink>().Single();
			link.NavigateUri.ToString().Should().Be("http://www.site.com/");
			link.Inlines.Single().Should().BeOfType<Run>();
			((Run)link.Inlines.Single()).Text.Should().Be("Nav");

			var tb2 = r.FindName("tb02") as TextBlock;

			Assert.AreEqual(5, tb2.Inlines.Count);
			Assert.AreEqual("start ", (tb2.Inlines[0] as Run).Text);
			Assert.AreEqual(" ", (tb2.Inlines[1] as Run).Text);

			var bold = tb2.Inlines[2] as Bold;
			Assert.IsNotNull(bold);

			var boldRun = bold.Inlines.FirstOrDefault() as Run;
			Assert.IsNotNull(boldRun);
			Assert.AreEqual("test", boldRun.Text);

			Assert.AreEqual(" ", (tb2.Inlines[3] as Run).Text);
			Assert.AreEqual(" finish", (tb2.Inlines[4] as Run).Text);
		}

		[TestMethod]
		public void When_TextBlock_NestedSpan()
		{
			var s = GetContent(nameof(When_TextBlock_NestedSpan));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tb = r.FindName("tb01") as TextBlock;

			var bold = tb.Inlines[0] as Bold;
			Assert.IsNotNull(bold);

			var italic = bold.Inlines[0] as Italic;
			Assert.IsNotNull(italic);

			var hyperlink = italic.Inlines[0] as Hyperlink;
			Assert.IsNotNull(hyperlink);

			Assert.IsInstanceOfType(hyperlink.Inlines[0], typeof(Run));
			Assert.AreEqual("test", (hyperlink.Inlines[0] as Run).Text);

			Assert.IsInstanceOfType(hyperlink.Inlines[1], typeof(LineBreak));
			Assert.IsInstanceOfType(hyperlink.Inlines[1], typeof(LineBreak));

			Assert.IsInstanceOfType(hyperlink.Inlines[2], typeof(Run));
			Assert.AreEqual("line", (hyperlink.Inlines[2] as Run).Text);
		}

		[TestMethod]
		public void When_VisualStateGroup_AttachedProperty_Binding()
		{
			var s = GetContent(nameof(When_VisualStateGroup_AttachedProperty_Binding));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("rootGrid") as Grid;
			var myPanel = r.FindName("myPanel") as StackPanel;
			var groups = VisualStateManager.GetVisualStateGroups(root);

			var visualStateGroup = groups.FirstOrDefault();
			object visualStateGroupDataContext;
			visualStateGroup.DataContextChanged += (s2, e2) => visualStateGroupDataContext = e2.NewValue;

			var visualState = visualStateGroup.States.FirstOrDefault();
			object visualStateDataContext;
			visualStateGroup.DataContextChanged += (s2, e2) => visualStateDataContext = e2.NewValue;

			var trigger = visualState.StateTriggers.FirstOrDefault() as StateTrigger;
			object triggerDataContext;
			trigger.DataContextChanged += (s2, e2) => triggerDataContext = e2.NewValue;

			Assert.IsFalse(trigger.IsActive);
			Assert.AreEqual(1, myPanel.Opacity);

			r.DataContext = new { a = true };

			Assert.IsTrue(trigger.IsActive);
			Assert.IsNotNull(trigger.DataContext);
			Assert.AreEqual(.5, myPanel.Opacity);
		}

		[TestMethod]
		public void When_VisualStateGroup_Propagation()
		{
			var s = GetContent(nameof(When_VisualStateGroup_Propagation));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as ListViewItem;
			var test = r.FindName("test");

		}

		[TestMethod]
		public void When_TextBlock_FontFamily()
		{
			var s = GetContent(nameof(When_TextBlock_FontFamily));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tb01 = r.FindName("tb01") as TextBlock;
			Assert.AreEqual("My Text 01", tb01.Text);
			Assert.IsNotNull(tb01.FontFamily);
			Assert.AreEqual("Segoe UI", tb01.FontFamily.Source);

			var tb02 = r.FindName("tb02") as TextBlock;
			var r2 = tb02.Inlines[0] as Run;

			Assert.IsNotNull(r.FontFamily);
			Assert.AreEqual("inner text", r2.Text);
			Assert.AreEqual("Segoe UI", r2.FontFamily.Source);
		}

		[TestMethod]
		public void When_MultipleImplicitStyle()
		{
			var s = GetContent(nameof(When_MultipleImplicitStyle));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsTrue(r.Resources.ContainsKey(typeof(Grid)));
			Assert.IsTrue(r.Resources.ContainsKey(typeof(TextBlock)));
		}

		[TestMethod]
		public void When_Binding_Converter()
		{
			var s = GetContent(nameof(When_Binding_Converter));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var c = r.Content as ContentControl;

			Assert.AreEqual(Visibility.Visible, c.Visibility);

			c.DataContext = false;

			Assert.AreEqual(Visibility.Collapsed, c.Visibility);

			c.DataContext = true;

			Assert.AreEqual(Visibility.Visible, c.Visibility);
		}

		[TestMethod]
		public void When_Binding_ConverterParameter()
		{
			var s = GetContent(nameof(When_Binding_ConverterParameter));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var c = r.Content as ContentControl;

			Assert.AreEqual("42", c.GetBindingExpression(UIElement.VisibilityProperty).ParentBinding.ConverterParameter);
		}

		[TestMethod]
		public void When_StaticResource_Style_And_Binding()
		{
			var s = GetContent(nameof(When_StaticResource_Style_And_Binding));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			r.ForceLoaded();

			Assert.IsTrue(r.Resources.ContainsKey("test"));

			var tb1 = r.FindName("tb1") as ToggleButton;
			var tb2 = r.FindName("tb2") as ToggleButton;

			Assert.IsTrue((bool)tb1.IsChecked);
			Assert.IsTrue((bool)tb2.IsChecked);

			tb1.IsChecked = false;

			Assert.IsFalse((bool)tb1.IsChecked);
			Assert.IsFalse((bool)tb2.IsChecked);

			tb2.IsChecked = true;

			Assert.IsTrue((bool)tb1.IsChecked);
			Assert.IsTrue((bool)tb2.IsChecked);
		}

		[TestMethod]
		public void When_GridRowDefinitions()
		{
			var s = GetContent(nameof(When_GridRowDefinitions));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as Grid;

			Assert.AreEqual(3, root.RowDefinitions.Count);
			Assert.AreEqual(GridUnitType.Star, root.RowDefinitions[0].Height.GridUnitType);
			Assert.AreEqual(1.0, root.RowDefinitions[0].Height.Value);
			Assert.AreEqual(GridUnitType.Star, root.RowDefinitions[1].Height.GridUnitType);
			Assert.AreEqual(1.0, root.RowDefinitions[1].Height.Value);

			var panel = r.FindName("innerPanel") as EmptyTestControl;
			Assert.AreEqual(1, Grid.GetRow(panel));

			var panel2 = r.FindName("innerPanel2") as StackPanel;
			Assert.AreEqual(2, Grid.GetRow(panel2));

		}

		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_Common_Syntax()
		{
			using var _ = new AssertionScope();
			var xaml = GetContent(nameof(When_Grid_Uses_Common_Syntax));
			var userControl = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as UserControl;
			var grid = userControl.FindName("grid") as Grid;

			grid.Should().NotBeNull();
			grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}

		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_New_Succinct_Syntax()
		{
			using var _ = new AssertionScope();
			var xaml = GetContent(nameof(When_Grid_Uses_New_Succinct_Syntax));
			var userControl = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as UserControl;
			var grid = userControl.FindName("grid") as Grid;

			grid.Should().NotBeNull();
			grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}

		[TestMethod]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
		{
			using var _ = new AssertionScope();
			var xaml = GetContent(nameof(When_Grid_Uses_New_Assigned_ContentProperty_Syntax));
			var userControl = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as UserControl;
			var grid = userControl.FindName("grid") as Grid;

			grid.Should().NotBeNull();
			grid.RowDefinitions.Should().BeEquivalentTo(new[]
			{
				new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
				new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				new RowDefinition { Height = new GridLength(25, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(14, GridUnitType.Pixel) },
				new RowDefinition { Height = new GridLength(20, GridUnitType.Pixel) },
			});
			grid.ColumnDefinitions.Should().BeEquivalentTo(new[]
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = new GridLength(300, GridUnitType.Pixel) },
			});
		}

		[TestMethod]
		public void When_ImplicitStyle_WithoutKey()
		{
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() =>
			{
				var s = GetContent(nameof(When_ImplicitStyle_WithoutKey));
				var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
			});
		}

		[TestMethod]
		public void When_NonDependencyPropertyAssignable()
		{
			var s = GetContent(nameof(When_NonDependencyPropertyAssignable));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as Grid;
			var inner = root.Children.First() as NonDependencyPropertyAssignable;

			Assert.AreEqual("innerPanel", inner.Name);
			Assert.AreEqual("42", inner.Tag);
			Assert.AreEqual(43, inner.MyProperty);
		}

		[TestMethod]
		public void When_NonDependencyProperty_Binding()
		{
			var s = GetContent(nameof(When_NonDependencyProperty_Binding));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as Grid;
			var inner = root.Children.First() as NonDependencyPropertyAssignable;

			Assert.AreEqual("innerPanel", inner.Name);
			Assert.IsNotNull(inner.MyBinding);
			Assert.AreEqual("Text", inner.MyBinding.Path.Path);
		}

		[TestMethod]
		public void When_TypeConverters()
		{
			var s = GetContent(nameof(When_TypeConverters));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = (TypeConvertersControl)r.Content;

			Assert.AreEqual(typeof(TypeConvertersControl), root.TypeProperty);
			Assert.AreEqual(new Uri("https://platform.uno/"), root.UriProperty);
		}

		[TestMethod]
		public void When_SetLessProperty()
		{
			var s = GetContent(nameof(When_SetLessProperty));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = (SetLessPropertyControl)r.Content;
		}

		[TestMethod]
		public void When_TopLevel_ResourceDictionary()
		{
			var s = GetContent(nameof(When_TopLevel_ResourceDictionary));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as ResourceDictionary;

			Assert.IsTrue(r.ContainsKey("DefaultColumnStyle"));
			var style = r["DefaultColumnStyle"] as Style;
			Assert.IsNotNull(style);
			Assert.AreEqual(typeof(TextBlock), style.TargetType);
		}

		[TestMethod]
		public void When_StaticResource_SystemTypes()
		{
			var s = GetContent(nameof(When_StaticResource_SystemTypes));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsTrue(r.Resources.ContainsKey("myDouble"));
			Assert.IsTrue(r.Resources.ContainsKey("mySingle"));
			Assert.IsTrue(r.Resources.ContainsKey("myInt32"));
			Assert.IsTrue(r.Resources.ContainsKey("myString"));

			Assert.AreEqual(42.42, r.Resources["myDouble"]);
			Assert.AreEqual(42.42f, r.Resources["mySingle"]);
			Assert.AreEqual((int)42, r.Resources["myInt32"]);
			Assert.AreEqual("Result is 42", r.Resources["myString"]);
		}

		[TestMethod]
		public void When_IList_TabView()
		{
			var s = GetContent(nameof(When_IList_TabView));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tabView1 = r.FindName("tabView1") as Microsoft.UI.Xaml.Controls.TabView;

			Assert.AreEqual(2, tabView1.TabItems.Count);
		}

		[TestMethod]
		public void When_StateTrigger_PropertyPath()
		{
			var s = GetContent(nameof(When_StateTrigger_PropertyPath));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
		}

		[TestMethod]
		public void When_Event_Handler()
		{
			var s = GetContent(nameof(When_Event_Handler));
			var r = new When_Event_Handler();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, r);

			var button1 = r.FindName("Button1") as Button;
			button1.RaiseClick();
			Assert.AreEqual(1, r.Handler1Count);

			var button2 = r.FindName("Button2") as Button;
			button2.RaiseClick();
			Assert.AreEqual(1, r.Handler2Count);
		}

		[TestMethod]
		public void When_Event_Handler_xBind()
		{
			var s = GetContent(nameof(When_Event_Handler_xBind));
			var r = new When_Event_Handler_xBind();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, r);

			var button1 = r.FindName("Button1") as Button;
			button1.RaiseClick();
			Assert.AreEqual(1, r.Handler1Count);

			var button2 = r.FindName("Button2") as Button;
			button2.RaiseClick();
			Assert.AreEqual(1, r.Handler2Count);
		}

		[TestMethod]
		public void When_Color_Thickness_GridLength_As_String()
		{
			var s = GetContent(nameof(When_Color_Thickness_GridLength_As_String));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as ContentControl;

			Assert.AreEqual(Microsoft.UI.Colors.Red, r.Resources["Color01"]);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, (r.Resources["scb01"] as SolidColorBrush).Color);
			Assert.AreEqual(new Thickness(42), r.Resources["thickness"]);
			Assert.AreEqual(new CornerRadius(42), r.Resources["cornerRadius"]);
			Assert.AreEqual("TestFamily", (r.Resources["fontFamily"] as FontFamily).Source);
			Assert.AreEqual(GridLength.FromString("42"), r.Resources["gridLength"]);
			Assert.AreEqual(Microsoft.UI.Xaml.Media.Animation.KeyTime.FromTimeSpan(TimeSpan.Parse("1:2:3")), r.Resources["keyTime"]);
			Assert.AreEqual(new Duration(TimeSpan.Parse("1:2:3")), r.Resources["duration"]);
			Assert.AreEqual(Matrix.Identity, r.Resources["matrix"]);
			Assert.AreEqual(Microsoft.UI.Text.FontWeights.Bold, r.Resources["fontWeight"]);

			Assert.AreEqual(Microsoft.UI.Colors.Red, ((r.Content as Grid)?.Background as SolidColorBrush).Color);
		}

		[TestMethod]
		public void When_Resources_And_Empty()
		{
			var s = "<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' > <Grid.Resources ></Grid.Resources ></Grid > ";
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Grid;
			Assert.IsNotNull(r.Resources);
		}

		[TestMethod]
		public void When_StaticResource_And_NonDependencyProperty()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			app.Resources["MyIntResource"] = 77;
			try
			{
				var s = GetContent(nameof(When_StaticResource_And_NonDependencyProperty));
				var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

				var root = r.FindName("root") as Grid;
				var inner = root.Children.First() as NonDependencyPropertyAssignable;

				Assert.AreEqual(77, inner.MyProperty);
			}
			finally
			{
				app.Resources.Remove("MyDoubleResource");
			}

		}

		[TestMethod]
		public void When_ThemeResource_And_Setter_And_Theme_Changed()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			var themeDict = new ResourceDictionary
			{
				ThemeDictionaries =
				{
					{"Light", new ResourceDictionary
						{
							{"MyIntResourceThemed", 244 }
						}
					},
					{"Dark", new ResourceDictionary
						{
							{"MyIntResourceThemed", 9 }
						}
					},
				}
			};
			app.Resources.MergedDictionaries.Add(themeDict);
			try
			{
				var s = GetContent(nameof(When_ThemeResource_And_Setter_And_Theme_Changed));
				var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

				var root = r.FindName("root") as Grid;
				var inner = root.Children.First() as Button;

				app.HostView.Children.Add(r);

				Assert.AreEqual(ApplicationTheme.Light, app.RequestedTheme);
				Assert.AreEqual(244, inner.Tag);

				using var _ = ThemeHelper.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				Assert.AreEqual(ApplicationTheme.Dark, app.RequestedTheme);
				Assert.AreEqual(9, inner.Tag);
			}
			finally
			{
				app.Resources.MergedDictionaries.Remove(themeDict);
			}

		}

		[TestMethod]
		public void When_Xmlns_Non_Default()
		{
			var xaml = "<NonDefaultXamlNamespace Test=\"42\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			var builder = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
			if (builder is CreateFromStringFullyQualifiedMethodNameOwner owner)
			{
				Assert.AreEqual(42, owner.Test.Value);
			}
		}

		[TestMethod]
		public void When_CreateFromString_Invalid_MethodName()
		{
			var xaml = "<CreateFromStringInvalidMethodNameOwner Test=\"8\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() => Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml));
		}

		[TestMethod]
		public void When_CreateFromString_Non_Qualified_MethodName()
		{
			var xaml = "<CreateFromStringNonQualifiedMethodNameOwner Test=\"16\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			var builder = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
			if (builder is CreateFromStringFullyQualifiedMethodNameOwner owner)
			{
				Assert.AreEqual(32, owner.Test.Value);
			}
		}

		[TestMethod]
		public void When_CreateFromString_Non_Static_Method()
		{
			var xaml = "<CreateFromStringNonStaticMethodOwner Test=\"4\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() => Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml));
		}

		[TestMethod]
		public void When_CreateFromString_Private_Static_Method()
		{
			var xaml = "<CreateFromStringPrivateStaticMethodOwner Test=\"21\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() => Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml));
		}

		[TestMethod]
		public void When_CreateFromString_Internal_Static_Method()
		{
			var xaml = "<CreateFromStringInternalStaticMethodOwner Test=\"42\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			var builder = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
			if (builder is CreateFromStringFullyQualifiedMethodNameOwner owner)
			{
				Assert.AreEqual(84, owner.Test.Value);
			}
		}

		[TestMethod]
		public void When_CreateFromString_Invalid_Parameters()
		{
			var xaml = "<CreateFromStringInvalidParametersOwner Test=\"2\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() => Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml));
		}

		[TestMethod]
		public void When_CreateFromString_Invalid_Return_Type()
		{
			var xaml = "<CreateFromStringInvalidReturnTypeOwner Test=\"1\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			// TODO: This should throw XamlParseException too
			Assert.ThrowsExactly<Uno.Xaml.XamlParseException>(() => Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml));
		}

		[TestMethod]
		public void When_CreateFromString_Fully_Qualified_MethodName()
		{
			var xaml = "<CreateFromStringFullyQualifiedMethodNameOwner Test=\"12\" xmlns=\"using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests\" />";
			var builder = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
			if (builder is CreateFromStringFullyQualifiedMethodNameOwner owner)
			{
				Assert.AreEqual(24, owner.Test.Value);
			}
		}

		[TestMethod]
		public void When_xName_Reload()
		{
			var s = GetContent(nameof(When_xName_Reload));
			var SUT = new When_xName_Reload();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, SUT);

			var Button1_field_private = SUT.FindName("Button1_field_private") as Button;
			Assert.IsNotNull(Button1_field_private);
			Assert.AreEqual(Button1_field_private, SUT.Button1_field_private_Getter);

			var Button1_field_public = SUT.FindName("Button1_field_public") as Button;
			Assert.IsNotNull(Button1_field_public);
			Assert.AreEqual(Button1_field_public, SUT.Button1_field_public);

			var Button2_property_private = SUT.FindName("Button2_property_private") as Button;
			Assert.IsNotNull(Button2_property_private);
			Assert.AreEqual(Button2_property_private, SUT.Button2_property_private_Getter);

			var Button2_property_public = SUT.FindName("Button2_property_public") as Button;
			Assert.IsNotNull(Button2_property_public);
			Assert.AreEqual(Button2_property_public, SUT.Button2_property_public);
		}

		[TestMethod]
		public void When_ResourceDictionary_Colors()
		{
			var s = GetContent(nameof(When_ResourceDictionary_Colors));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as ResourceDictionary;

			var lightTheme = r.ThemeDictionaries["Light"] as ResourceDictionary;
			Assert.IsNotNull(lightTheme);

			Assert.AreEqual(Microsoft.UI.Colors.Red, lightTheme["MaterialPrimaryColor"]);

			var darkTheme = r.ThemeDictionaries["Dark"] as ResourceDictionary;
			Assert.IsNotNull(darkTheme);

			Assert.AreEqual(Microsoft.UI.Colors.White, darkTheme["MaterialOnPrimaryColor"]);
		}

		[TestMethod]
		public void When_xBind_Simple()
		{
			var s = GetContent(nameof(When_xBind_Simple));
			var r = new When_xBind_Simple();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, r);

			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(r);

			var SUT = r.FindFirstChild<TextBlock>();

			Assert.AreEqual("Sprong", SUT.Text);
		}

		[TestMethod]
		public void When_xBind_TwoWay()
		{
			var s = GetContent(nameof(When_xBind_TwoWay));
			var r = new When_xBind_TwoWay();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, r);

			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(r);

			var SUT = r.FindFirstChild<CheckBox>();

			Assert.IsFalse(SUT.IsChecked);

			r.MyVM.MyBool = true;
			Assert.IsTrue(SUT.IsChecked);
		}

		[TestMethod]
		public void When_xBind_TwoWay_Back()
		{
			var s = GetContent(nameof(When_xBind_TwoWay));
			var r = new When_xBind_TwoWay();
			Microsoft.UI.Xaml.Markup.XamlReader.LoadUsingComponent(s, r);

			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(r);

			var SUT = r.FindFirstChild<CheckBox>();

			Assert.IsFalse(SUT.IsChecked);

			SUT.IsChecked = true;
			Assert.IsTrue(r.MyVM.MyBool);
		}

		[TestMethod]
		public void When_Collection_Implicit_Add_Item()
		{
			var SUT = XamlHelper.LoadXaml<SwipeItems>("""
				<muxc:SwipeItems>
					<muxc:SwipeItem Text="asd" />
				</muxc:SwipeItems>
				""");

			Assert.AreEqual(1, SUT.Count);
			Assert.AreEqual("asd", SUT[0].Text);
		}

		[TestMethod]
		public void When_Collection_Property_Nest_Collection()
		{
			var SUT = XamlHelper.LoadXaml<SwipeControl>("""
				<muxc:SwipeControl>
					<muxc:SwipeControl.LeftItems>
						<muxc:SwipeItems Mode="Execute">
							<muxc:SwipeItem Text="asd" />
						</muxc:SwipeItems>
					</muxc:SwipeControl.LeftItems>
				</muxc:SwipeControl>
				""");

			Assert.IsNotNull(SUT.LeftItems);
			Assert.AreEqual(SwipeMode.Execute, SUT.LeftItems.Mode); // check we are using the very same collection in the xaml, and not a new instance
			Assert.AreEqual(1, SUT.LeftItems.Count);
			Assert.AreEqual("asd", SUT.LeftItems[0].Text);
		}

		[TestMethod]
		public void When_Collection_Property_Nest_Multiple_Collections()
		{
			var SUT = XamlHelper.LoadXaml<SwipeControl>("""
				<muxc:SwipeControl>
					<muxc:SwipeControl.LeftItems>
						<!-- This is actually allowed, however only the last will be kept -->
						<muxc:SwipeItems>
							<muxc:SwipeItem Text="asd" />
						</muxc:SwipeItems>
						<muxc:SwipeItems Mode="Execute">
							<muxc:SwipeItem Text="qwe" />
						</muxc:SwipeItems>
					</muxc:SwipeControl.LeftItems>
				</muxc:SwipeControl>
				""");

			Assert.IsNotNull(SUT.LeftItems);
			Assert.AreEqual(SwipeMode.Execute, SUT.LeftItems.Mode); // check we are using the very same collection in the xaml, and not a new instance
			Assert.AreEqual(1, SUT.LeftItems.Count);
			Assert.AreEqual("qwe", SUT.LeftItems[0].Text);
		}

		[TestMethod]
		public void When_StaticResource_In_ResourceDictionary()
		{
			var s = GetContent(nameof(When_StaticResource_In_ResourceDictionary));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var c2 = (Color)panel.Resources["c2"];
			var b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);
			Assert.AreEqual(Microsoft.UI.Colors.Green, b2.Color);

			r.ForceLoaded();

			c2 = (Color)panel.Resources["c2"];
			b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);
		}

		[TestMethod]
		public void When_StaticResource_In_Explicit_ResourceDictionary()
		{
			var s = GetContent(nameof(When_StaticResource_In_Explicit_ResourceDictionary));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var c2 = (Color)panel.Resources["c2"];
			var b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);

			r.ForceLoaded();

			c2 = (Color)panel.Resources["c2"];
			b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);
		}


		[TestMethod]
		public void When_StaticResource_In_Themed_ResourceDictionary()
		{
			var s = GetContent(nameof(When_StaticResource_In_Themed_ResourceDictionary));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var light = (ResourceDictionary)panel.Resources.ThemeDictionaries["Light"];
			var dark = (ResourceDictionary)panel.Resources.ThemeDictionaries["Dark"];

			Assert.AreEqual(1, light.Count);
			Assert.AreEqual(1, dark.Count);
		}

		[TestMethod]
		public void When_Run_In_Inlines_Span()
		{
			When_Run_In_Inlines_Helper("Span", p => ((Span)p).Inlines);
		}

		[TestMethod]
		public void When_Run_In_Inlines_Paragraph()
		{
			When_Run_In_Inlines_Helper("Paragraph", p => ((Paragraph)p).Inlines);
		}

		private void When_Run_In_Inlines_Helper(string rootName, Func<object, InlineCollection> getInlines)
		{
			var root = Microsoft.UI.Xaml.Markup.XamlReader.Load($@"<{rootName} xml:space=""preserve"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">regular text<Bold>bold text</Bold><Underline>underline text</Underline><Italic>italic text</Italic><Hyperlink Name=""name"">this is a hyperlink</Hyperlink>more regular text</{rootName}>");
			var inlines = getInlines(root).ToArray();
			Assert.AreEqual(6, inlines.Length);
			Assert.AreEqual("regular text", ((Run)inlines[0]).Text);
			Assert.AreEqual("bold text", ((Run)((Bold)inlines[1]).Inlines.Single()).Text);
			Assert.AreEqual("underline text", ((Run)((Underline)inlines[2]).Inlines.Single()).Text);
			Assert.AreEqual("italic text", ((Run)((Italic)inlines[3]).Inlines.Single()).Text);
			Assert.AreEqual("this is a hyperlink", ((Run)((Hyperlink)inlines[4]).Inlines.Single()).Text);
			Assert.AreEqual("more regular text", ((Run)inlines[5]).Text);
		}

		[TestMethod]
		public void When_StaticResource_In_Explicit_ResourceDictionary_And_ThemeResources()
		{
			var s = GetContent(nameof(When_StaticResource_In_Explicit_ResourceDictionary_And_ThemeResources));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var c2 = (Color)panel.Resources["c2"];
			var b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);

			r.ForceLoaded();

			c2 = (Color)panel.Resources["c2"];
			b2 = (SolidColorBrush)panel.Resources["b2"];

			var c3 = (Color)panel.Resources["c3"];
			var b3 = (SolidColorBrush)panel.Resources["b3"];

			Assert.AreEqual(b3.Color, c3);
		}

		[TestMethod]
		public void When_StaticResource_In_Explicit_ResourceDictionary_And_MergedDictionaries()
		{
			var s = GetContent(nameof(When_StaticResource_In_Explicit_ResourceDictionary_And_MergedDictionaries));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var c2 = (Color)panel.Resources["c2"];
			var b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);

			r.ForceLoaded();

			c2 = (Color)panel.Resources["c2"];
			b2 = (SolidColorBrush)panel.Resources["b2"];

			Assert.AreEqual(b2.Color, c2);

			var c3 = (Color)panel.Resources["c3"];
			var b3 = (SolidColorBrush)panel.Resources["b3"];

			Assert.AreEqual(b3.Color, c3);

			var c4 = (Color)panel.Resources["c4"];
			var b4 = (SolidColorBrush)panel.Resources["b4"];

			Assert.AreEqual(b4.Color, c4);
		}

		[TestMethod]
		public void When_StaticResource_MergedDictionaries_Fluent()
		{
			var s = GetContent(nameof(When_StaticResource_MergedDictionaries_Fluent));
			var r = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var panel = r.FindName("panel") as StackPanel;
			Assert.IsNotNull(panel);

			var t1 = (Color)panel.Resources["c1"];

			r.ForceLoaded();

			var b1 = (SolidColorBrush)panel.Resources["b1"];

			Assert.AreEqual(t1, Microsoft.UI.Colors.Red);
			Assert.AreEqual(t1, b1.Color);
		}

		[TestMethod]
		public void When_Setter_Override_From_Visual_Parent()
		{
			var s = GetContent(nameof(When_Setter_Override_From_Visual_Parent));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;
			SUT.ForceLoaded();

			var tb = (TextBlock)SUT.FindName("MarkTextBlock");
			Assert.IsNotNull(tb);
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)tb.Foreground).Color);
		}

		[TestMethod]
		public void When_Setter_Override_State_From_Visual_Parent()
		{
			var s = GetContent(nameof(When_Setter_Override_State_From_Visual_Parent));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;
			SUT.ForceLoaded();

			VisualStateManager.GoToState((Control)SUT.FindName("SubjectToggleButton"), "Checked", false);

			var tb = (TextBlock)SUT.FindName("MarkTextBlock");
			Assert.IsNotNull(tb);
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(Microsoft.UI.Colors.Orange, ((SolidColorBrush)tb.Foreground).Color);
		}

		[TestMethod]
		public void When_ThemeResource_Inherited_multiple()
		{
			var s = GetContent(nameof(When_ThemeResource_Inherited_multiple));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;
			SUT.ForceLoaded();


			var border1 = (Border)SUT.FindName("border1");
			var border2 = (Border)SUT.FindName("border2");

			Assert.IsNotNull(border1);
			Assert.IsNotNull(border2);

			Assert.IsInstanceOfType(border1.Background, typeof(SolidColorBrush));
			Assert.IsInstanceOfType(border1.BorderBrush, typeof(SolidColorBrush));
			Assert.IsInstanceOfType(border2.Background, typeof(SolidColorBrush));
			Assert.IsInstanceOfType(border2.BorderBrush, typeof(SolidColorBrush));
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border1.Background).Color);
			Assert.AreEqual(Microsoft.UI.Colors.Pink, ((SolidColorBrush)border1.BorderBrush).Color);
			Assert.AreEqual(Microsoft.UI.Colors.Blue, ((SolidColorBrush)border2.Background).Color);
			Assert.AreEqual(Microsoft.UI.Colors.Yellow, ((SolidColorBrush)border2.BorderBrush).Color);
		}

		[TestMethod]
		public void When_Geometry()
		{
			var path = "<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z</Geometry>";
			var geometry = Microsoft.UI.Xaml.Markup.XamlReader.Load(path) as Uno.Media.StreamGeometry;
			Assert.IsNotNull(geometry);
			Assert.AreEqual(FillRule.EvenOdd, geometry.FillRule);
		}

		[TestMethod]
		public void When_RectangleGeometry()
		{
			var sut = XamlHelper.LoadXaml<RectangleGeometry>("<RectangleGeometry Rect='0 1 2 3' />");

			Assert.AreEqual(new Windows.Foundation.Rect(0, 1, 2, 3), sut.Rect);
		}

		[TestMethod]
		public void When_ThemeResource_With_StaticResource()
		{
			var s = GetContent(nameof(When_ThemeResource_With_StaticResource));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as Page;

			Assert.IsNotNull(SUT.Resources["Color1"]);
			Assert.IsNotNull(SUT.Resources["Color2"]);
		}

		[TestMethod]
		public void When_ResourceDictionary_With_Theme_And_Static()
		{
			var s = GetContent(nameof(When_ResourceDictionary_With_Theme_And_Static));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as ResourceDictionary;

			Assert.AreEqual(2, SUT.ThemeDictionaries.Count);
			Assert.IsNotNull(SUT["CustomSecondBrush"]);
			Assert.IsNotNull(SUT["MyCustomFirstBrush"]);
		}

		[TestMethod]
		public void When_ResourceDictionary_With_Theme_And_No_Static()
		{
			var s = GetContent(nameof(When_ResourceDictionary_With_Theme_And_No_Static));
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load(s) as ResourceDictionary;

			Assert.AreEqual(2, SUT.ThemeDictionaries.Count);
			Assert.IsNotNull(SUT["PrimaryColor"]);
		}

		[TestMethod]
		public void When_Ignore_Surrounding_Whitespace()
		{
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TextWrapping="WrapWholeWords">
				   BeforeLineBreak
				<LineBreak />
				   You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.
				<LineBreak />
				   AfterLineBreak 
				</TextBlock>
				""") as TextBlock;

			Assert.AreEqual(5, SUT.Inlines.Count);
			Assert.AreEqual("BeforeLineBreak", ((Run)SUT.Inlines[0]).Text);
			Assert.IsInstanceOfType(SUT.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.", ((Run)SUT.Inlines[2]).Text);
			Assert.IsInstanceOfType(SUT.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("AfterLineBreak", ((Run)SUT.Inlines[4]).Text);
		}

		[TestMethod]
		public void When_Ignore_Surrounding_Whitespace_With_Preserve_Space()
		{
			var SUT = Microsoft.UI.Xaml.Markup.XamlReader.Load("""
				<TextBlock xml:space="preserve" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TextWrapping="WrapWholeWords">
				   BeforeLineBreak
				<LineBreak />
				   You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.
				<LineBreak />
				   AfterLineBreak 
				</TextBlock>
				""") as TextBlock;

			Assert.AreEqual(5, SUT.Inlines.Count);
			Assert.AreEqual("\n   BeforeLineBreak\n", ((Run)SUT.Inlines[0]).Text);
			Assert.IsInstanceOfType(SUT.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("\n   You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.\n", ((Run)SUT.Inlines[2]).Text);
			Assert.IsInstanceOfType(SUT.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("\n   AfterLineBreak \n", ((Run)SUT.Inlines[4]).Text);
		}

		[TestMethod]
		public void When_Binding_Converter_StaticResource()
		{
			var root = (StackPanel)XamlReader.Load(
				@"<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                        xmlns:primitives='using:Microsoft.UI.Xaml.Controls.Primitives'> 
                    <StackPanel.Resources>
                        <primitives:CornerRadiusFilterConverter x:Key='RightCornerRadiusFilterConverter' Filter='Right'/>
                    </StackPanel.Resources>
					<Grid x:Name='SourceGrid' CornerRadius='6,6,6,6' />
                    <Grid x:Name='RightRadiusGrid'
                        CornerRadius='{Binding ElementName=SourceGrid, Path=CornerRadius, Converter={StaticResource RightCornerRadiusFilterConverter}}'>
                    </Grid>
                </StackPanel>");

			var rightRadiusGrid = (Grid)root.FindName("RightRadiusGrid");

			Assert.AreEqual(new CornerRadius(0, 6, 6, 0), rightRadiusGrid.CornerRadius);
		}

		[TestMethod]
		public void When_Vector3_Property()
		{
			var xaml =
				"""
				<VectorPropertyControl xmlns='Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests' Vec3="1,4,100" />
				""";
			var control = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as VectorPropertyControl;

			Assert.IsNotNull(control);
			Assert.AreEqual(new Vector3(1, 4, 100), control.Vec3);
		}

		[TestMethod]
		public void When_Vector2_Property()
		{
			var xaml =
				"""
				<VectorPropertyControl xmlns='Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests' Vec2="31,4" />
				""";
			var control = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as VectorPropertyControl;

			Assert.IsNotNull(control);
			Assert.AreEqual(new Vector2(31, 4), control.Vec2);
		}

		[TestMethod]
		public void When_CustomMarkupExtension_Simple()
		{
			var xaml =
				"""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests' Tag="{local:CustomMarkupExtension Value1=Hello, Value2=World}" />
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;

			Assert.IsNotNull(element);
			Assert.AreEqual("Hello,World", element.Tag);
		}

		[TestMethod]
		public void When_CustomMarkupExtension_Responsive()
		{
			var xaml =
				"""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests' Grid.RowSpan="{local:ResponsiveExtension Narrow=1, Wide=2}" />
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;

			Assert.IsNotNull(element);
			Assert.AreEqual(2, Grid.GetRowSpan(element));
		}

		[TestMethod]
		public void When_CustomMarkupExtension_ShortName()
		{
			var xaml =
				"""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests' Tag="{local:CustomMarkup Value1=A, Value2=B}" />
				""";

			var element = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml) as Grid;

			Assert.IsNotNull(element);
			Assert.AreEqual("A,B", element.Tag);
		}

		private string GetContent(string testName)
		{
			var assembly = this.GetType().Assembly;
			var name = $"{GetType().Namespace}.{testName}.xamltest";
			// "Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests.BasicReader.xamltest"
			using (var stream = assembly.GetManifestResourceStream(name))
			{
				using (var streamReader = new StreamReader(stream))
				{
					return streamReader.ReadToEnd();
				}
			}
		}
	}
}
