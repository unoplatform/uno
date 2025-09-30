using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using AwesomeAssertions.Execution;
using Uno.UI.Extensions;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_xLoad
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_xLoad_Multiple()
		{
			var SUT = new When_xLoad();

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();

			Assert.AreEqual(8, stubs.Count());
		}

		[TestMethod]
		public void When_xLoad_LoadSingle()
		{
			var SUT = new When_xLoad_LoadSingle();

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border1);

			var border1 = SUT.FindName("border1");
			Assert.AreEqual(SUT.border1, border1);
		}

		[TestMethod]
		public void When_xLoad_Deferred_StaticCollapsed()
		{
			var SUT = new When_xLoad_Deferred_StaticCollapsed();

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border6);

			var border1 = SUT.FindName("border6");
			Assert.AreEqual(SUT.border6, border1);
		}

		[TestMethod]
		public void When_xLoad_Deferred_VisibilityBinding()
		{
			var SUT = new When_xLoad_Deferred_VisibilityBinding();
			SUT.ForceLoaded();

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border7);

			// Changing the visibility DOES NOT materialize the lazily-loaded element.
			SUT.DataContext = true;
			Assert.IsNull(SUT.border7);

			var border = SUT.FindName("border7");
			Assert.IsNotNull(SUT.border7);
			Assert.AreEqual(SUT.border7, border);
		}

		[TestMethod]
		public void When_xLoad_Deferred_VisibilityxBind()
		{
			var SUT = new When_xLoad_Deferred_VisibilityxBind();
			SUT.ForceLoaded();
			SUT.Measure(new Size(42, 42));

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border8);

			// Changing the visibility DOES NOT materialize the lazily-loaded element.
			SUT.MyVisibility = true;
			SUT.Measure(new Size(42, 42));

			Assert.IsNull(SUT.border8);

			var border1 = SUT.FindName("border8");
			Assert.IsNotNull(SUT.border8);
			Assert.AreEqual(SUT.border8, border1);
		}

		[TestMethod]
		public void When_Deferred_Visibility_and_StaticResource()
		{
			var SUT = new When_xLoad_Multiple();
			SUT.ForceLoaded();
			SUT.Measure(new Size(42, 42));
			SUT.DataContext = Visibility.Collapsed;

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border1);

			var border1 = SUT.FindName("border1");
			SUT.Measure(new Size(42, 42));

			Assert.IsNotNull(SUT.border1);
			Assert.AreEqual(SUT.border1, border1);
		}

		[TestMethod]
		public void When_xLoad_xBind()
		{
			var SUT = new When_xLoad_xBind();
			SUT.ForceLoaded();
			SUT.Measure(new Size(42, 42));

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			Assert.IsNull(SUT.border1);

			SUT.IsLoad = true;
			SUT.Measure(new Size(42, 42));

			Assert.IsNotNull(SUT.border1);

			var border1 = SUT.FindName("border1");
			Assert.AreEqual(SUT.border1, border1);

			SUT.IsLoad = false;
			SUT.Measure(new Size(42, 42));

			stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			var borders = SUT.EnumerateAllChildren().OfType<Border>();
			Assert.AreEqual(0, borders.Count());
		}

		[TestMethod]
		public void When_xLoad_DataTemplate_In_ResDict()
		{
			var SUT = new When_xLoad_DataTemplate_In_ResDict();

			SUT.ForceLoaded();

			var stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			var tb02 = SUT.FindName("tb02") as TextBlock;
			Assert.IsNotNull(tb02);

			var model = new When_xLoad_DataTemplate_In_ResDict_Model();
			SUT.DataContext = model;

			stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(1, stubs.Count());

			model.Visible = true;

			stubs = SUT.EnumerateAllChildren().OfType<ElementStub>();
			Assert.AreEqual(0, stubs.Count());

			Assert.IsNotNull(tb02.Foreground);
			Assert.AreEqual("[SolidColorBrush #FFFF0000]", tb02.Foreground.ToString());

			var tb01 = SUT.FindName("tb01") as TextBlock;

			Assert.IsNotNull(tb01.Foreground);
			Assert.AreEqual("[SolidColorBrush #FFFF0000]", tb01.Foreground.ToString());
		}
	}
}
