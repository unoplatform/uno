using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_NameScope
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_NameScope()
		{
			var SUT = new When_NameScope();
			SUT.ForceLoaded();

			var OuterElementName = NameScope.FindInNamescopes(
				SUT,
				nameof(SUT.OuterElementName)) as FrameworkElement;
			var OuterBorder = NameScope.FindInNamescopes(
				SUT,
				nameof(SUT.OuterBorder)) as FrameworkElement;

			Assert.IsNotNull(OuterElementName);
			Assert.IsNotNull(OuterBorder);

			var OuterElementTopLevelFromOuterBorder = NameScope.FindInNamescopes(
				OuterBorder,
				nameof(SUT.OuterElementTopLevel)) as FrameworkElement;
			Assert.IsNotNull(OuterElementTopLevelFromOuterBorder);

			// search through visual tree walking
			var OuterElementTopLevelFromOuterElementName = NameScope.FindInNamescopes(
				OuterElementName,
				nameof(SUT.OuterElementTopLevel)) as FrameworkElement;
			var OuterBorderFromOuterElementName = NameScope.FindInNamescopes(
				OuterElementName,
				nameof(SUT.OuterBorder)) as FrameworkElement;

			Assert.IsNotNull(OuterElementTopLevelFromOuterElementName);
			Assert.IsNotNull(OuterBorderFromOuterElementName);

			// search via namescope
			var InnerBorderFromOuterElementName = NameScope.FindInNamescopes(
				SUT.OuterElementName as FrameworkElement,
				nameof(When_NameScope_Inner.InnerBorder)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementName);

			var InnerBorderFromOuterElementNameContent = NameScope.FindInNamescopes(
				SUT.OuterElementName.Content as FrameworkElement,
				nameof(When_NameScope_Inner.InnerBorder)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementNameContent);

			// Search through namescope from the inner element to the inner top level name
			var InnerElementTopLevelFromInnerBorder = NameScope.FindInNamescopes(
				InnerBorderFromOuterElementNameContent,
				nameof(When_NameScope_Inner.InnerElementTopLevel)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementNameContent);
			Assert.AreEqual(InnerElementTopLevelFromInnerBorder.Name, "OuterElementName");
		}

		[TestMethod]
		public void When_NameScope_DataTemplate()
		{
			var SUT = new When_NameScope_DataTemplate();
			SUT.ForceLoaded();

			var OuterBorder = NameScope.FindInNamescopes(
				SUT,
				nameof(SUT.OuterBorder)) as FrameworkElement;

			Assert.IsNotNull(SUT.OuterElementName);
			Assert.IsNotNull(OuterBorder);

			var OuterElementTopLevelFromOuterBorder = NameScope.FindInNamescopes(
				OuterBorder,
				nameof(SUT.OuterElementTopLevel)) as FrameworkElement;
			Assert.IsNotNull(OuterElementTopLevelFromOuterBorder);

			// search through visual tree walking
			var OuterElementTopLevelFromOuterElementName = NameScope.FindInNamescopes(
				SUT.OuterElementName,
				nameof(SUT.OuterElementTopLevel)) as FrameworkElement;
			var OuterBorderFromOuterElementName = NameScope.FindInNamescopes(
				SUT.OuterElementName,
				nameof(SUT.OuterBorder)) as FrameworkElement;

			Assert.IsNotNull(OuterElementTopLevelFromOuterElementName);
			Assert.IsNotNull(OuterBorderFromOuterElementName);

			// search via namescope
			var InnerBorderFromOuterElementName = NameScope.FindInNamescopes(
				SUT.OuterElementName as FrameworkElement,
				nameof(When_NameScope_Inner.InnerBorder)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementName);

			var InnerBorderFromOuterElementNameContent = NameScope.FindInNamescopes(
				SUT.OuterElementName.Content as FrameworkElement,
				nameof(When_NameScope_Inner.InnerBorder)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementNameContent);

			// Search through namescope from the inner element to the inner top level name
			var InnerElementTopLevelFromInnerBorder = NameScope.FindInNamescopes(
				InnerBorderFromOuterElementNameContent,
				nameof(When_NameScope_Inner.InnerElementTopLevel)) as FrameworkElement;
			Assert.IsNotNull(InnerBorderFromOuterElementNameContent);

			Assert.AreEqual(InnerElementTopLevelFromInnerBorder.Name, "OuterElementName");
		}
	}
}
