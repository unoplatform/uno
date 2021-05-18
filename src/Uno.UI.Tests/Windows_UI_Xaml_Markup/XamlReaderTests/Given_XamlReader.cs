using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using View = Windows.UI.Xaml.FrameworkElement;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using FluentAssertions;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.UI;

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
            var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as Page;

            Assert.IsNotNull(r);
            Assert.AreEqual("testPage", r.Name);
        }

        [TestMethod]
        public void When_BasicProperty()
        {
            var s = GetContent(nameof(When_BasicProperty));
            var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

            Assert.IsNotNull(r);
            Assert.AreEqual("testPage", r.Name);
            Assert.AreEqual(42.0, r.Width);
        }

        [TestMethod]
        public void When_UserControl_With_Content()
        {
            var s = GetContent(nameof(When_UserControl_With_Content));
            var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
            var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

            Assert.IsNotNull(r);
            Assert.AreEqual("testPage", r.Name);

            var grid = r.Content as Grid;
            Assert.IsNotNull(grid);

            Assert.AreEqual(2, grid.Children.Count);

			var border1 = grid.Children.ElementAt(0) as Border;
			var border2 = grid.Children.ElementAt(1) as Border;

			Assert.AreEqual((border1.Background as SolidColorBrush).Color, Windows.UI.Colors.Red);
			Assert.AreEqual((border2.Background as SolidColorBrush).Color, Windows.UI.Colors.Blue);
		}

        [TestMethod]
        public void When_MultipleBindings()
        {
            var s = GetContent(nameof(When_MultipleBindings));
            var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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

			var content = itemsPanel.LoadContent() as StackPanel;
			Assert.IsNotNull(content);
			Assert.AreEqual(content.Name, "InnerStackPanel");

			var template = page.Resources["PhotoTemplate"] as DataTemplate;
			Assert.IsNotNull(template);

			var photoTemplateContent = template.LoadContent() as FrameworkElement;
			Assert.IsNotNull(photoTemplateContent);

			var border01 = photoTemplateContent.FindName("border01") as Border;
			Assert.IsNotNull(border01);
			var stops = (border01.Background as LinearGradientBrush).GradientStops;
			Assert.AreEqual(2, stops.Count);
			Assert.AreEqual(Windows.UI.Colors.Transparent, stops[0].Color);
			Assert.AreEqual(Windows.UI.ColorHelper.FromARGB(0x33, 0, 0, 0), stops[1].Color);
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

			var uriSourceExpression = photoTemplateImage.Source.GetBindingExpression(Windows.UI.Xaml.Media.Imaging.BitmapImage.UriSourceProperty);
			Assert.IsNotNull(uriSourceExpression);
			Assert.AreEqual("Thumbnail", uriSourceExpression.ParentBinding.Path.Path);
		}

		[TestMethod]
		public void When_AttachedProperty_Different_Target()
		{
			var s = GetContent(nameof(When_AttachedProperty_Different_Target));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var grid = r.FindName("rootGrid") as Grid;
			Assert.IsNotNull(grid);

			Assert.AreEqual(42, Grid.GetRow(grid));
		}

		[TestMethod]
		public void When_AttachedProperty_Binding()
		{
			var s = GetContent(nameof(When_AttachedProperty_Binding));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var stackPanel = r.FindName("innerPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			var expression = stackPanel.GetBindingExpression(StackPanel.OrientationProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyOrientation", expression.ParentBinding.Path.Path);
			Assert.AreEqual(Windows.UI.Xaml.Data.BindingMode.TwoWay, expression.ParentBinding.Mode);

			var expressionWidth = stackPanel.GetBindingExpression(StackPanel.WidthProperty);
			Assert.IsNotNull(expressionWidth);
			Assert.AreEqual("MyWidth", expressionWidth.ParentBinding.Path.Path);
			Assert.AreEqual(Windows.UI.Xaml.Data.BindingMode.OneTime, expressionWidth.ParentBinding.Mode);
		}

		[TestMethod]
		public void When_StaticResource()
		{
			var app = UnitTestsApp.App.EnsureApplication();
			app.Resources["StaticRow"] = 42;
			app.Resources["StaticWidth"] = 42.0;
			app.Resources["StaticHeight"] = 44.0;

			var s = GetContent(nameof(When_StaticResource));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
		public void When_TextBlock_Basic()
		{
			var s = GetContent(nameof(When_TextBlock_Basic));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var tb01 = r.FindName("tb01") as TextBlock;
			Assert.IsNotNull(tb01);
			Assert.AreEqual("My Text 01", tb01.Text);

			var tb02 = r.FindName("tb02") as TextBlock;
			Assert.IsNotNull(tb02);
			var tb02_run = tb02.Inlines.FirstOrDefault() as Windows.UI.Xaml.Documents.Run;
			Assert.AreEqual("My Text 02", tb02_run.Text);

			var tb03 = r.FindName("tb03") as TextBlock;
			Assert.IsNotNull(tb03);
			var tb03_run = tb03.Inlines.FirstOrDefault() as Windows.UI.Xaml.Documents.Run;
			Assert.AreEqual("My Run Text", tb03_run.Text);
		}

		[TestMethod]
		public void When_ElementName()
		{
			var s = GetContent(nameof(When_ElementName));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsNotNull(r);

			var stackPanel = r.FindName("rootPanel") as StackPanel;
			Assert.IsNotNull(stackPanel);

			var textBlock = r.FindName("innerTextBlock") as TextBlock;
			Assert.IsNotNull(textBlock);

			var expression = textBlock.GetBindingExpression(TextBlock.WidthProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("Width", expression.ParentBinding.Path.Path);
			Assert.AreEqual(stackPanel, (expression.ParentBinding.ElementName as ElementNameSubject)?.ElementInstance);
			Assert.AreEqual(42.0, textBlock.Width);
		}

		[TestMethod]
		public void When_ContentControl_ControlTemplate()
		{
			var s = GetContent(nameof(When_ContentControl_ControlTemplate));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
			Assert.IsNotNull(r);

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var border1 = r.FindName("border1") as Border;
			Assert.AreEqual(0, Grid.GetRow(border1));

			Window.Current.SetWindowSize(new Windows.Foundation.Size(721, 100));

			Assert.AreEqual(1, Grid.GetRow(border1));
		}

		[TestMethod]
		public void When_VisualStateGroup()
		{
			var s = GetContent(nameof(When_VisualStateGroup));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as Grid;

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
			Assert.IsNull(setter.Target.Target);

			// Force a size change, otherwise setter.Target.Target won't get evaluated
			Window.Current.SetWindowSize(new Windows.Foundation.Size(719, 100));
			Window.Current.SetWindowSize(new Windows.Foundation.Size(721, 100));

			Assert.IsNotNull(setter.Target.Target);

			var myPanel = setter.Target.Target as StackPanel;
			Assert.AreEqual("myPanel", myPanel?.Name);
			Assert.AreEqual(Orientation.Horizontal, myPanel.Orientation);

			Window.Current.SetWindowSize(new Windows.Foundation.Size(719, 100));
			Assert.AreEqual(Orientation.Vertical, myPanel.Orientation);
		}

		[TestMethod]
		public void When_XNull()
		{
			var s = GetContent(nameof(When_XNull));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as Grid;
			var innerPanel = r.FindName("innerPanel") as StackPanel;

			Assert.AreEqual(root.DataContext, "42");
			Assert.IsNull(innerPanel.DataContext);
		}

		[TestMethod]
		public void When_Binding_TwoWay_UpdateSourceTrigger()
		{
			var s = GetContent(nameof(When_Binding_TwoWay_UpdateSourceTrigger));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var innerPanel = r.FindName("innerPanel") as StackPanel;

			var expression = innerPanel.GetBindingExpression(StackPanel.OrientationProperty);
			Assert.IsNotNull(expression);
			Assert.AreEqual("MyOrientation", expression.ParentBinding.Path.Path);
			Assert.AreEqual(Windows.UI.Xaml.Data.BindingMode.TwoWay, expression.ParentBinding.Mode);
			Assert.AreEqual(Windows.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged, expression.ParentBinding.UpdateSourceTrigger);

			Assert.IsNull(innerPanel.DataContext);
		}

		[TestMethod]
		public void When_Binding_TargetNull()
		{
			var s = GetContent(nameof(When_Binding_TargetNull));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tb1 = r.FindName("tb01") as TextBlock;
			var link = tb1.Inlines.OfType<Hyperlink>().Single();
			link.NavigateUri.ToString().Should().Be("http://www.site.com/");
			link.Inlines.Single().Should().BeOfType<Run>();
			((Run) link.Inlines.Single()).Text.Should().Be("Nav");

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = r.FindName("root") as ListViewItem;
			var test = r.FindName("test");

		}

		[TestMethod]
		public void When_TextBlock_FontFamily()
		{
			var s = GetContent(nameof(When_TextBlock_FontFamily));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			Assert.IsTrue(r.Resources.ContainsKey(typeof(Grid)));
			Assert.IsTrue(r.Resources.ContainsKey(typeof(TextBlock)));
		}

		[TestMethod]
		public void When_Binding_Converter()
		{
			var s = GetContent(nameof(When_Binding_Converter));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var c = r.Content as ContentControl;

			Assert.AreEqual("42", c.GetBindingExpression(UIElement.VisibilityProperty).ParentBinding.ConverterParameter);
		}

		[TestMethod]
		public void When_StaticResource_Style_And_Binding()
		{
			var s = GetContent(nameof(When_StaticResource_Style_And_Binding));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
		public void When_ImplicitStyle_WithoutKey()
		{
			Assert.ThrowsException<InvalidOperationException>(() => {
				var s = GetContent(nameof(When_ImplicitStyle_WithoutKey));
				var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
			});
		}
		
		[TestMethod]
		public void When_NonDependencyPropertyAssignable()
		{
			var s = GetContent(nameof(When_NonDependencyPropertyAssignable));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = (TypeConvertersControl)r.Content;

			Assert.AreEqual(typeof(TypeConvertersControl), root.TypeProperty);
			Assert.AreEqual(new Uri("https://platform.uno/"), root.UriProperty);
		}

		[TestMethod]
		public void When_SetLessProperty()
		{
			var s = GetContent(nameof(When_SetLessProperty));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var root = (SetLessPropertyControl)r.Content;
		}

		[TestMethod]
		public void When_TopLevel_ResourceDictionary()
		{
			var s = GetContent(nameof(When_TopLevel_ResourceDictionary));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as ResourceDictionary;

			Assert.IsTrue(r.ContainsKey("DefaultColumnStyle"));
			var style = r["DefaultColumnStyle"] as Style;
			Assert.IsNotNull(style);
			Assert.AreEqual(typeof(TextBlock), style.TargetType);
		}

		[TestMethod]
		public void When_StaticResource_SystemTypes()
		{
			var s = GetContent(nameof(When_StaticResource_SystemTypes));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

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
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;

			var tabView1 = r.FindName("tabView1") as Microsoft.UI.Xaml.Controls.TabView;

			Assert.AreEqual(2, tabView1.TabItems.Count);
		}

		[TestMethod]
		public void When_StateTrigger_PropertyPath()
		{
			var s = GetContent(nameof(When_StateTrigger_PropertyPath));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as UserControl;
		}

		[TestMethod]
		public void When_Brush_And_StringColor()
		{
			var s = GetContent(nameof(When_Brush_And_StringColor));
			var r = Windows.UI.Xaml.Markup.XamlReader.Load(s) as ContentControl;
		}

		private string GetContent(string testName)
		{
			var assembly = this.GetType().Assembly;
			var name = $"{GetType().Namespace}.{testName}.xamltest";
			// "Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests.BasicReader.xamltest"
			using (var stream = assembly.GetManifestResourceStream(name))
			{
				return stream.ReadToEnd();
			}
		}
	}
}
