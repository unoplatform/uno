#nullable enable
#if !WINAPPSDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_xLoad
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_xLoad_Literal()
		{
			var sut = new xLoad_Literal();

			TestServices.WindowHelper.WindowContent = sut;
			var loadBorderFalse = sut.LoadBorderFalse;
			var loadBorderTrue = sut.LoadBorderTrue;

			Assert.IsNull(loadBorderFalse);
			Assert.IsNotNull(loadBorderTrue);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_Order()
		{
			var sut = new When_xLoad_Order();

			TestServices.WindowHelper.WindowContent = sut;

			Assert.IsInstanceOfType(sut.root.Children[0], typeof(ElementStub));
			Assert.IsInstanceOfType(sut.root.Children[1], typeof(ElementStub));
			Assert.IsInstanceOfType(sut.root.Children[2], typeof(ElementStub));

			sut.IsLoaded2 = true;
			sut.Refresh();

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType(sut.root.Children[0], typeof(ElementStub));
			Assert.IsInstanceOfType(sut.root.Children[1], typeof(Border));
			Assert.IsInstanceOfType(sut.root.Children[2], typeof(ElementStub));

			sut.IsLoaded3 = true;
			sut.Refresh();

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType(sut.root.Children[0], typeof(ElementStub));
			Assert.IsInstanceOfType(sut.root.Children[1], typeof(Border));
			Assert.IsInstanceOfType(sut.root.Children[2], typeof(Border));

			sut.IsLoaded1 = true;
			sut.Refresh();

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsInstanceOfType(sut.root.Children[0], typeof(Border));
			Assert.IsInstanceOfType(sut.root.Children[1], typeof(Border));
			Assert.IsInstanceOfType(sut.root.Children[2], typeof(Border));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind()
		{
			var sut = new xLoad_xBind();

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForLoaded(sut);

			var loadBorder = sut.LoadBorder;
			Assert.IsNull(sut.LoadBorder);

			sut.IsLoad = true;

			Assert.IsNotNull(sut.LoadBorder);
			var parent = (Border)sut.LoadBorder.Parent;

			sut.IsLoad = false;

			Assert.IsFalse(((ElementStub)parent.Child).Load);

			sut.IsLoad = true;

			Assert.IsNotNull(sut.LoadBorder);
			parent = (Border)sut.LoadBorder.Parent;

			sut.IsLoad = false;

			Assert.IsFalse(((ElementStub)parent.Child).Load);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_xLoad_Visibility_While_Materializing()
		{
			var SUT = new When_xLoad_Visibility_While_Materializing();

			Assert.AreEqual(0, When_xLoad_Visibility_While_Materializing_Content.Instances);

			TestServices.WindowHelper.WindowContent = SUT;

			Assert.AreEqual(0, When_xLoad_Visibility_While_Materializing_Content.Instances);

			SUT.Model.IsVisible = true;

			Assert.AreEqual(1, When_xLoad_Visibility_While_Materializing_Content.Instances);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind_xLoad_Initial()
		{
			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;

			var SUT = new When_xLoad_xBind_xLoad_Initial();
			grid.Children.Add(SUT);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual(1, SUT.tb01.Tag);

			SUT.Model.MyValue = 42;

			Assert.AreEqual(42, SUT.tb01.Tag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind_xLoad_While_Loading()
		{
			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;

			var SUT = new When_xLoad_xBind_xLoad_While_Loading();
			grid.Children.Add(SUT);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual(1, SUT.tb01.Tag);

			SUT.Model.MyValue = 42;

			Assert.AreEqual(42, SUT.tb01.Tag);
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16250")]
		public async Task When_xLoad_Referenced_By_xBind()
		{
			var SUT = new When_xLoad_Referenced_By_xBind();
			await UITestHelper.Load(SUT);

			Assert.IsNull(SUT.LoadElement);
			Assert.IsTrue(SUT.button1.IsEnabled);
			Assert.IsFalse(SUT.ToggleLoad.IsChecked);

			SUT.ToggleLoad.IsChecked = true;

			Assert.IsNotNull(SUT.LoadElement);
			Assert.IsFalse(SUT.button1.IsEnabled);
			Assert.IsTrue(SUT.ToggleLoad.IsChecked);
		}


#if __ANDROID__
		[Ignore("https://github.com/unoplatform/uno/issues/7305")]
#endif
		[TestMethod]
		public async Task When_Binding_xLoad_Nested()
		{
			var SUT = new Binding_xLoad_Nested();
			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			Assert.AreEqual(0, SUT.TopLevelVisiblity1GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity1SetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity2GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity2SetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity3GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity3SetCount);

			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;
			grid.Children.Add(SUT);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
			Assert.AreEqual(2, SUT.TopLevelVisiblity1GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity1SetCount);

			SUT.TopLevelVisiblity1 = true;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			SUT.TopLevelVisiblity2 = true;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			SUT.TopLevelVisiblity3 = true;

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNotNull(SUT.panel02);
			Assert.IsNotNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNotNull(SUT.tb06);

			SUT.TopLevelVisiblity3 = false;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNotNull(SUT.panel01);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			SUT.TopLevelVisiblity2 = false;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			// Note: If not null, this usually means that the control is leaking!!!
			Assert.IsNull(SUT.panel01);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb06);
			Assert.IsNull(SUT.panel03);
			Assert.IsNull(SUT.tb05);

			SUT.TopLevelVisiblity1 = false;

			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.panel01);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.panel03);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
		}

#if HAS_UNO
#if __ANDROID__
		[Ignore("https://github.com/unoplatform/uno/issues/7305")]
#endif
		[TestMethod]
		public async Task When_Binding_xLoad_Nested_With_ElementStub_LoadCount()
		{
			//
			// This test is the same as When_Binding_xLoad_Nested, but with explicit querying
			// of ElementStub instances. This prevents the GC from collecting instances, but allows
			// for counting Load/Unload counts properly.
			//

			var SUT = new Binding_xLoad_Nested();
			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			Assert.AreEqual(0, SUT.TopLevelVisiblity1GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity1SetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity2GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity2SetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity3GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity3SetCount);

			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;
			grid.Children.Add(SUT);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);

			Assert.AreEqual(2, SUT.TopLevelVisiblity1GetCount);
			Assert.AreEqual(0, SUT.TopLevelVisiblity1SetCount);

			var stubs = GetAllChildren(SUT).OfType<ElementStub>().ToList();
			var tb01Stub = stubs.First(e => e.Name == "tb01");
			var tb02Stub = stubs.First(e => e.Name == "tb02")!;
			var panel01Stub = stubs.First(e => e.Name == "panel01")!;
			var panel03Stub = stubs.First(e => e.Name == "panel03");

			var tb01StubChangedCount = 0;
			tb01Stub.MaterializationChanged += _ => tb01StubChangedCount++;

			var tb02StubChangedCount = 0;
			tb02Stub.MaterializationChanged += _ => tb02StubChangedCount++;

			var panel01StubChangedCount = 0;
			panel01Stub.MaterializationChanged += _ => panel01StubChangedCount++;

			var panel03StubChangedCount = 0;
			panel03Stub.MaterializationChanged += _ => panel03StubChangedCount++;

			SUT.TopLevelVisiblity1 = true;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
			Assert.IsFalse(panel03Stub.Load);
			Assert.AreEqual(1, tb01StubChangedCount);
			Assert.AreEqual(1, tb02StubChangedCount);
			Assert.AreEqual(0, panel01StubChangedCount);

			SUT.TopLevelVisiblity2 = true;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
			Assert.IsTrue(panel03Stub.Load);
			Assert.AreEqual(1, tb01StubChangedCount);
			Assert.AreEqual(1, tb02StubChangedCount);
			Assert.AreEqual(1, panel01StubChangedCount);

			var panel02Stub = GetAllChildren(SUT).OfType<ElementStub>().First(e => e.Name == "panel02");

			var panel02StubChangedCount = 0;
			panel02Stub.MaterializationChanged += _ => panel02StubChangedCount++;

			SUT.TopLevelVisiblity3 = true;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNotNull(SUT.panel02);
			Assert.IsNotNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNotNull(SUT.tb06);
			Assert.IsTrue(panel03Stub.Load);
			Assert.AreEqual(1, tb01StubChangedCount);
			Assert.AreEqual(1, tb02StubChangedCount);
			Assert.AreEqual(1, panel01StubChangedCount);
			Assert.AreEqual(1, panel02StubChangedCount);

			SUT.TopLevelVisiblity3 = false;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNotNull(SUT.tb03);
			Assert.IsNotNull(SUT.panel01);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsNotNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
			Assert.IsTrue(panel03Stub.Load);
			Assert.AreEqual(1, tb01StubChangedCount);
			Assert.AreEqual(1, tb02StubChangedCount);
			Assert.AreEqual(1, panel01StubChangedCount);
			Assert.AreEqual(2, panel02StubChangedCount);

			SUT.TopLevelVisiblity2 = false;

			await Task.Yield();

			Assert.IsNotNull(SUT.tb01);
			Assert.IsNotNull(SUT.tb02);
			Assert.IsNull(SUT.panel01);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsNull(SUT.tb06);
			Assert.IsFalse(panel03Stub.Load);
			Assert.IsNull(SUT.panel03);
			Assert.IsNull(SUT.tb05);
			Assert.AreEqual(1, tb01StubChangedCount);
			Assert.AreEqual(1, tb02StubChangedCount);
			Assert.AreEqual(2, panel01StubChangedCount);
			Assert.AreEqual(2, panel02StubChangedCount);

			SUT.TopLevelVisiblity1 = false;

			await Task.Yield();

			Assert.IsNull(SUT.tb01);
			Assert.IsNull(SUT.tb02);
			Assert.IsNull(SUT.panel01);
			Assert.IsNull(SUT.tb03);
			Assert.IsNull(SUT.panel02);
			Assert.IsNull(SUT.tb04);
			Assert.IsFalse(panel03Stub.Load);
			Assert.IsNull(SUT.panel03);
			Assert.IsNull(SUT.tb05);
			Assert.IsNull(SUT.tb06);
			Assert.AreEqual(2, tb01StubChangedCount);
			Assert.AreEqual(2, tb02StubChangedCount);
			Assert.AreEqual(2, panel01StubChangedCount);
			Assert.AreEqual(2, panel02StubChangedCount);

			IEnumerable<UIElement> GetAllChildren(UIElement element)
			{
				yield return element;
				foreach (var child in VisualTreeHelper.GetChildren(element).OfType<UIElement>())
				{
					foreach (var childChild in GetAllChildren(child))
					{
						yield return childChild;
					}
				}
			}
		}
#endif

		[TestMethod]
		public async Task When_xLoad_Visibility_Set()
		{
			var SUT = new xLoad_Visibility();
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.GetChildren().Count(c => c is ElementStub));
			Assert.AreEqual(0, SUT.GetChildren().Count(c => c is Border));
		}
	}
}
#endif
