using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows_UI_Xaml_Controls;
using static Private.Infrastructure.TestServices;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ContentControl
	{
		private ResourceDictionary _testsResources;

		public DataTemplate DataContextBindingDataTemplate => _testsResources["DataContextBindingDataTemplate"] as DataTemplate;
		public DataTemplate ContentControlComboTemplate => _testsResources["ContentControlComboTemplate"] as DataTemplate;

		private DataTemplate SelectableItemTemplateA => _testsResources["SelectableItemTemplateA"] as DataTemplate;
		private DataTemplate SelectableItemTemplateB => _testsResources["SelectableItemTemplateB"] as DataTemplate;
		private DataTemplate SelectableItemTemplateC => _testsResources["SelectableItemTemplateC"] as DataTemplate;

		// Commented types don't seem to use ContentTemplateSelector on UWP
		private static readonly Type[] _contentControlStyledDerivedTypes = new[]
		{
			typeof(Button),
			//typeof(AppBarButton),
			//typeof(AppBar),
			//typeof(CommandBar),
			//typeof(SplitButton),
			typeof(RepeatButton),
			typeof(ToggleButton),
			//typeof(AppBarToggleButton),
			typeof(CheckBox),
			typeof(RadioButton),
			//typeof(SplitButton),
			//typeof(ToggleSplitButton),
			typeof(DropDownButton)
		};

		public IEnumerable<ContentControl> DerivedStyledControlsInstances => _contentControlStyledDerivedTypes.Select(t => Activator.CreateInstance(t) as ContentControl);

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(typeof(Grid))]
		[DataRow(typeof(StackPanel))]
		[DataRow(typeof(Border))]
		[DataRow(typeof(ContentPresenter))]
		public async Task When_SelfLoading(Type type)
		{
			var control = (FrameworkElement)Activator.CreateInstance(type);

			control.Width = 200;
			control.Height = 200;

			await UITestHelper.Load(control);
		}

		[TestMethod]
		public async Task When_Binding_Within_Control_Template()
		{
			var contentControl = new ContentControl
			{
				ContentTemplate = DataContextBindingDataTemplate,
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel(),
			};

			grid.Children.Add(contentControl);
			TestServices.WindowHelper.WindowContent = grid;

			await TestServices.WindowHelper.WaitForLoaded(grid);

			var tb = grid.FindFirstChild<TextBlock>();

			Assert.IsNotNull(tb);

			await TestServices.WindowHelper.WaitFor(() => tb.Text == "Steve");
		}

		[TestMethod]
		public async Task When_ContentTemplateSelector_And_Default_Style()
		{
			var items = new[] { "item 1", "item 2", "item 3" };
			foreach (var control in DerivedStyledControlsInstances)
			{
				var templateSelector = new Given_ListViewBase.KeyedTemplateSelector
				{
					Templates =
					{
						{ items[0], SelectableItemTemplateA },
						{ items[1], SelectableItemTemplateB },
						{ items[2], SelectableItemTemplateC },
					}
				};

				control.ContentTemplateSelector = templateSelector;
				control.Content = "Dummy";

				WindowHelper.WindowContent = control;
				await WindowHelper.WaitForLoaded(control);
				control.Content = items[0];
				var text1 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"), message: $"Template selector not applied for {control.GetType()}");
				Assert.AreEqual("Selectable A", text1.Text, $"Template selector not applied for {control.GetType()}");

				control.Content = items[1];
				var text2 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"));
				Assert.AreEqual("Selectable B", text2.Text);

				control.Content = items[2];
				var text3 = await WindowHelper.WaitForNonNull(() => control.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate"));
				Assert.AreEqual("Selectable C", text3.Text);
			}
		}


		[TestMethod]
		public async Task When_ContentTemplateSelector_And_Default_Style_And_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_ContentTemplateSelector_And_Default_Style();
		}

		[TestMethod]
		public async Task When_Template_Applied_On_Loading_DataContext_Propagation()
		{
			var page = new Template_Loading_DataContext_Page();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			var comboBox = page.SpawnedButtonHost.PseudoContent as ComboBox;

			var dataContextChangedCounter = 0;
			var itemsSourceChangedCounter = 0;
			comboBox.DataContextChanged += (_, __) => dataContextChangedCounter++;
			comboBox.RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, (_, __) => itemsSourceChangedCounter++);

			page.SpawnedButtonHost.SpawnButton();
			Assert.IsNotNull(comboBox);

			await WindowHelper.WaitForLoaded(comboBox);
			Assert.AreEqual("Froot", comboBox.SelectedItem);
			Assert.AreEqual(1, dataContextChangedCounter);
			Assert.AreEqual(1, itemsSourceChangedCounter);
		}

		[TestMethod]
		public async Task When_Content_Set_Null_ComboBox()
		{
			var contentControl = new ContentControl
			{
				ContentTemplate = ContentControlComboTemplate,
			}.Apply(cc => cc.SetBinding(ContentControl.ContentProperty, new Binding()));

			var grid = new Grid()
			{
				DataContext = new ViewModel()
			};

			grid.Children.Add(contentControl);
			WindowHelper.WindowContent = grid;

			await WindowHelper.WaitForLoaded(grid);

			var comboBox = grid.FindFirstChild<ComboBox>();


			Assert.IsNotNull(comboBox);
			Assert.HasCount(3, comboBox.Items);

			contentControl.Content = null;

#if __APPLE_UIKIT__ || __ANDROID__
			Assert.IsEmpty(comboBox.Items);
			Assert.AreEqual(-1, comboBox.SelectedIndex);
#else // this is correct
			Assert.HasCount(3, comboBox.Items);
			Assert.AreEqual(1, comboBox.SelectedIndex);
#endif
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FindName_ContentControl_Without_ContentTemplate()
		{
			var sut = new ContentControl
			{
				Width = 100,
				Height = 100,
				Content = new TextBox()
			};

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);

			Assert.IsNotNull(sut.FindName("ContentElement"));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_FindName_ContentControl_With_ContentTemplate()
		{
			var sut = new ContentControl
			{
				Width = 100,
				Height = 100,
				Content = new TextBox(),
				ContentTemplate = new DataTemplate(() => new TextBlock())
			};

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);

			Assert.IsNull(sut.FindName("ContentElement"));
		}
#endif

		private class SignInViewModel
		{
			public string UserName { get; set; } = "Steve";
		}

		public sealed class Item
		{
			public string DisplayName { get; init; }
		}

		private class ViewModel
		{
			public SignInViewModel SignIn { get; set; } = new SignInViewModel();

			List<Item> _items = new()
			{
				new Item { DisplayName = "Test1" },
				new Item { DisplayName = "Test2" },
				new Item { DisplayName = "Test3" },
			};

			public IEnumerable<Item> Items => _items;

			public int SelectedIndex { get; set; } = 1;
		}
	}
}
