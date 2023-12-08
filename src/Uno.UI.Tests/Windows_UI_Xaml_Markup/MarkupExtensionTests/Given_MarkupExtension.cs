using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml_Markup.MarkupExtensionTests.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.MarkupExtensionTests;

[TestClass]
public class Given_MarkupExtension
{
	[TestMethod]
	public void When_DP_Markup()
	{
		var setup = new When_DP_Markup_Setup(); // Tag="{local:DebugMarkupExtension}"
		var provider = setup.Tag as IXamlServiceProvider;
		var pvt = provider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
		var target = pvt.TargetObject;
		var pvtp = pvt.TargetProperty as ProvideValueTargetProperty;

		var expectedDP = FrameworkElement.TagProperty;

		Assert.AreEqual(setup, target);
		Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType);
		Assert.AreEqual(expectedDP.Name, pvtp.Name);
		Assert.AreEqual(expectedDP.Type, pvtp.Type);
	}

	[TestMethod]
	public void When_AttachedDP_Markup()
	{
		var setup = new When_AttachedDP_Markup_Setup(); // local:Attachable.Value="{local:DebugMarkupExtension}"
		var provider = Attachable.GetValue(setup) as IXamlServiceProvider;
		var pvt = provider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
		var target = pvt.TargetObject;
		var pvtp = pvt.TargetProperty as ProvideValueTargetProperty;

		var expectedDP = Attachable.ValueProperty;

		Assert.AreEqual(setup, target);
		Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType);
		Assert.AreEqual(expectedDP.Name, pvtp.Name);
		Assert.AreEqual(expectedDP.Type, pvtp.Type);
	}

	[TestMethod]
	public void When_DP_Markup_PropertyType()
	{
		var setup = new When_DP_Markup_Setup(); // local:Attachable.Value2="{local:DebugMarkupExtension Behavior=AssignToAttachableValue}"
		var provider = Attachable.GetValue(setup) as IXamlServiceProvider; // since target dp is an int, we are storing the provider in another dp
		var pvt = provider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
		var target = pvt.TargetObject;
		var pvtp = pvt.TargetProperty as ProvideValueTargetProperty;

		var expectedDP = Attachable.Value2Property;

		Assert.AreEqual(setup, target);
		Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType);
		Assert.AreEqual(expectedDP.Name, pvtp.Name);
		Assert.AreEqual(expectedDP.Type, pvtp.Type);
	}

	[TestMethod]
	public void When_AttachedDP_Markup_PropertyType()
	{
		var setup = new When_AttachedDP_Markup_Setup(); // local:Attachable.Value2="{local:DebugMarkupExtension Behavior=AssignToTag}"
		var provider = setup.Tag as IXamlServiceProvider; // since target dp is an int, we are storing the provider in another dp
		var pvt = provider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
		var target = pvt.TargetObject;
		var pvtp = pvt.TargetProperty as ProvideValueTargetProperty;

		var expectedDP = Attachable.Value2Property;

		Assert.AreEqual(setup, target);
		Assert.AreEqual(expectedDP.OwnerType, pvtp.DeclaringType);
		Assert.AreEqual(expectedDP.Name, pvtp.Name);
		Assert.AreEqual(expectedDP.Type, pvtp.Type);
	}
}
