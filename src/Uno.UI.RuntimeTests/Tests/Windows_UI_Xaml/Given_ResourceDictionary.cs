using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

#if HAS_UNO
using Uno.UI.Xaml;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ResourceDictionary
	{
		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("iOS and macOS don't yet load/unload from resources - https://github.com/unoplatform/uno/issues/5208")]
#endif
		public async Task When_FrameworkElement_In_Resources_Should_Receive_Loaded_Unloaded()
		{
			var textBlock = new TextBlock();
			var resourceTextBlock = new TextBlock();
			var resourceGrid = new Grid()
			{
				Children =
				{
					resourceTextBlock,
				},
			};
			var grid = new Grid()
			{
				Children =
				{
					textBlock,
				},
				Width = 100,
				Height = 100,
			};

			grid.Resources.Add("MyResourceGridKey", resourceGrid);

			bool wasXamlRootNull = false;
			string result = "";

			resourceTextBlock.Loaded += (_, _) =>
			{
				wasXamlRootNull |= resourceTextBlock.XamlRoot is null;
				result += "ResourceTextBlockLoaded,";
			};
			resourceGrid.Loaded += (_, _) =>
			{
				wasXamlRootNull |= resourceGrid.XamlRoot is null;
				result += "ResourceGridLoaded,";
			};
			textBlock.Loaded += (_, _) =>
			{
				wasXamlRootNull |= textBlock.XamlRoot is null;
				result += "TextBlockLoaded,";
			};

			resourceTextBlock.Unloaded += (_, _) => result += "ResourceTextBlockUnloaded,";
			resourceGrid.Unloaded += (_, _) => result += "ResourceGridUnloaded,";
			textBlock.Unloaded += (_, _) => result += "TextBlockUnloaded,";

			await UITestHelper.Load(grid);

#if WINAPPSDK || UNO_HAS_ENHANCED_LIFECYCLE
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,", result);
#else
			Assert.AreEqual("ResourceGridLoaded,ResourceTextBlockLoaded,TextBlockLoaded,", result);
#endif

			await UITestHelper.Load(new Border() { Width = 1, Height = 1 });

#if WINAPPSDK || UNO_HAS_ENHANCED_LIFECYCLE
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,TextBlockUnloaded,", result);
#elif __ANDROID__
			Assert.AreEqual("ResourceGridLoaded,ResourceTextBlockLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,TextBlockUnloaded,", result);
#else
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceTextBlockUnloaded,ResourceGridUnloaded,TextBlockUnloaded,", result);
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("iOS and macOS don't yet load/unload from resources - https://github.com/unoplatform/uno/issues/5208")]
#endif
		public async Task When_FrameworkElement_In_Resources_Then_Removed_Should_Receive_Loaded_Unloaded()
		{
			var textBlock = new TextBlock();
			var resourceTextBlock = new TextBlock();
			var resourceGrid = new Grid()
			{
				Children =
				{
					resourceTextBlock,
				},
			};
			var grid = new Grid()
			{
				Children =
				{
					textBlock,
				},
				Width = 100,
				Height = 100,
			};

			grid.Resources.Add("MyResourceGridKey", resourceGrid);

			string result = "";

			resourceTextBlock.Loaded += (_, _) => result += "ResourceTextBlockLoaded,";
			resourceGrid.Loaded += (_, _) => result += "ResourceGridLoaded,";
			textBlock.Loaded += (_, _) => result += "TextBlockLoaded,";

			resourceTextBlock.Unloaded += (_, _) => result += "ResourceTextBlockUnloaded,";
			resourceGrid.Unloaded += (_, _) => result += "ResourceGridUnloaded,";
			textBlock.Unloaded += (_, _) => result += "TextBlockUnloaded,";

			await UITestHelper.Load(grid);

#if WINAPPSDK || UNO_HAS_ENHANCED_LIFECYCLE
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,", result);
#else
			Assert.AreEqual("ResourceGridLoaded,ResourceTextBlockLoaded,TextBlockLoaded,", result);
#endif

			grid.Resources.Remove("MyResourceGridKey");
			await TestServices.WindowHelper.WaitForIdle();

#if WINAPPSDK || UNO_HAS_ENHANCED_LIFECYCLE
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,", result);
#elif __ANDROID__
			Assert.AreEqual("ResourceGridLoaded,ResourceTextBlockLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,", result);
#else
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceTextBlockUnloaded,ResourceGridUnloaded,", result);
#endif

			await UITestHelper.Load(new Border() { Width = 1, Height = 1 });

#if WINAPPSDK || UNO_HAS_ENHANCED_LIFECYCLE
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,TextBlockUnloaded,", result);
#elif __ANDROID__
			Assert.AreEqual("ResourceGridLoaded,ResourceTextBlockLoaded,TextBlockLoaded,ResourceGridUnloaded,ResourceTextBlockUnloaded,TextBlockUnloaded,", result);
#else
			Assert.AreEqual("ResourceTextBlockLoaded,ResourceGridLoaded,TextBlockLoaded,ResourceTextBlockUnloaded,ResourceGridUnloaded,TextBlockUnloaded,", result);
#endif
		}

		[TestMethod]
		public void When_Key_Overwritten()
		{
			const string key = "TestKey";
			const string originalValue = "original";
			const string newValue = "newValue";

			var resourceDictionary = new ResourceDictionary();
			resourceDictionary[key] = originalValue;
			resourceDictionary[key] = newValue;

			Assert.AreEqual(newValue, resourceDictionary[key]);
		}

		[TestMethod]
		public async Task When_ResourceDictionary_DP()
		{
			var SUT = new When_ResourceDictionary_DP();
			await UITestHelper.Load(SUT);
			var resourceDictionary = MyClass.GetX(SUT.MyButton);
			Assert.AreEqual(2, resourceDictionary.Count);
			Assert.AreEqual(Colors.Yellow, (Color)resourceDictionary["PrimaryColor"]);
			Assert.AreEqual(Colors.Red, (Color)resourceDictionary["SecondaryColor"]);
		}

#if HAS_UNO // uses uno specifics code
		[TestMethod]
		public void When_LinkedResDict_ThemeUpdated()
		{
			const string TestBrush = nameof(TestBrush);
			const string TestThemeColor = nameof(TestThemeColor);
			const string Light = nameof(Light);
			const string Dark = nameof(Dark);

			var theme = ResourceDictionary.GetActiveTheme();
			try
			{
				// setup
				ResourceDictionary.SetActiveTheme("Light");

				// initialize source res-dict
				var parserContext = new XamlParseContext();
				var dontcare = new object();
				var source = new ResourceDictionary()
				{
					IsParsing = true,
					ThemeDictionaries =
					{
						[Light] = new WeakResourceInitializer(dontcare, that => new ResourceDictionary { [TestThemeColor] = Colors.Red, }),
						[Dark] = new WeakResourceInitializer(dontcare, that => new ResourceDictionary { [TestThemeColor] = Colors.Blue, }),
					},
					[TestBrush] = new WeakResourceInitializer(dontcare, that =>
					{
						var brush = new SolidColorBrush();
						ResourceResolverSingleton.Instance.ApplyResource(brush, SolidColorBrush.ColorProperty, TestThemeColor, true, false, parserContext);

						return brush;
					})
				};
				source.CreationComplete();

				// making a copy from source
				var copy = new ResourceDictionary();
				copy.CopyFrom(source);

				// retrieve the "TestBrush" from each res-dict while also materializing it in both the source and in the copy
				var materialized1 = (SolidColorBrush)source[TestBrush];
				var materialized2 = (SolidColorBrush)copy[TestBrush];
				var materialized2Color = materialized2.Color;

				// set active theme and update the copy res-dict
				ResourceDictionary.SetActiveTheme(Dark);
				copy.UpdateThemeBindings(Microsoft.UI.Xaml.Data.ResourceUpdateReason.ThemeResource);

				// retrieve the "TestBrush" again from each res-dict
				var materialized3 = (SolidColorBrush)source[TestBrush];
				var materialized4 = (SolidColorBrush)copy[TestBrush];

				// validation
				Assert.IsFalse(ReferenceEquals(materialized1, materialized2)); // we are expecting these to be different, as the CopyFrom should copy them as WeakResourceInitializer...
				Assert.IsFalse(ReferenceEquals(materialized3, materialized4)); // ^same
				Assert.AreNotEqual(materialized2Color, materialized4.Color); // check the theme change is actually applied (otherwise it would void the next check)
				Assert.AreEqual(materialized3.Color, materialized4.Color); // check the theme change is propagated to the source res-dict
			}
			finally
			{
				// clean up
				ResourceDictionary.SetActiveTheme(theme);
			}
		}

		[TestMethod]
		public void When_Key_Added_Then_NotFound_Cleared()
		{
			var resourceDictionary = new ResourceDictionary();

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res1, shouldCheckSystem: false));
			resourceDictionary["Key1"] = "Value1";
			Assert.IsTrue(resourceDictionary.TryGetValue("Key1", out var res2, shouldCheckSystem: false));
		}

		[TestMethod]
		public void When_Merged_Dictionary_Added_Then_NotFound_Cleared()
		{
			var resourceDictionary = new ResourceDictionary();

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res1, shouldCheckSystem: false));

			var m1 = new ResourceDictionary();
			m1["Key1"] = "Value1";

			resourceDictionary.MergedDictionaries.Add(m1);

			Assert.IsTrue(resourceDictionary.TryGetValue("Key1", out var res2, shouldCheckSystem: false));
		}

		[TestMethod]
		public void When_Merged_Dictionary_Key_Added_Then_NotFound_Cleared()
		{
			var resourceDictionary = new ResourceDictionary();

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res1, shouldCheckSystem: false));

			var m1 = new ResourceDictionary();
			resourceDictionary.MergedDictionaries.Add(m1);

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res2, shouldCheckSystem: false));

			m1["Key1"] = "Value1";

			Assert.IsTrue(resourceDictionary.TryGetValue("Key1", out var res3, shouldCheckSystem: false));
		}

		[TestMethod]
		public void When_Theme_Dictionary_Key_Added_Then_NotFound_Cleared()
		{
			var resourceDictionary = new ResourceDictionary();

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res1, shouldCheckSystem: false));

			var m1 = new ResourceDictionary();
			resourceDictionary.ThemeDictionaries["Light"] = m1;

			Assert.IsFalse(resourceDictionary.TryGetValue("Key1", out var res2, shouldCheckSystem: false));

			m1["Key1"] = "Value1";

			Assert.IsTrue(resourceDictionary.TryGetValue("Key1", out var res3, shouldCheckSystem: false));
		}
#endif
	}
}
