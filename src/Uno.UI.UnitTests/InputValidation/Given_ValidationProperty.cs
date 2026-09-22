#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.InputValidation;

[TestClass]
public partial class Given_ValidationProperty
{
	[TestMethod]
	public void When_No_Attribute()
	{
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(Unannotated)));
	}

	[TestMethod]
	public void When_Attribute_On_Leaf_Then_Base_Property_Resolves()
	{
		// The case the rejected metadata-flag route could not express: the DP is registered on the shared
		// base, the attribute sits on the leaf, and the sibling leaf must not participate.
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.Validation.GetValidationProperty(typeof(ValidatingLeaf)));
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(NonValidatingLeaf)));
	}

	[TestMethod]
	public void When_Derived_Type_Then_Attribute_Is_Inherited()
	{
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.Validation.GetValidationProperty(typeof(DerivedFromValidatingLeaf)));
	}

	[TestMethod]
	public void When_Derived_Redeclares_Then_It_Shadows_The_Base()
	{
		Assert.AreSame(ShadowingDerived.OtherValueProperty, FeatureConfiguration.Validation.GetValidationProperty(typeof(ShadowingDerived)));
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.Validation.GetValidationProperty(typeof(ValidatingLeaf)));
	}

	[TestMethod]
	public void When_Empty_Name_Then_Opted_Out()
	{
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(OptedOutDerived)));
	}

	[TestMethod]
	public void When_Unknown_Property_Name()
	{
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(AnnotatedWithUnknownProperty)));
	}

	[TestMethod]
	public void When_Resolved_Twice_Then_Cached()
	{
		// Negative answers must be cached too, or every non-participating control re-walks its attributes.
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(Unannotated)));
		Assert.IsNull(FeatureConfiguration.Validation.GetValidationProperty(typeof(Unannotated)));

		var first = FeatureConfiguration.Validation.GetValidationProperty(typeof(ValidatingLeaf));
		Assert.AreSame(first, FeatureConfiguration.Validation.GetValidationProperty(typeof(ValidatingLeaf)));
	}

	/// <summary>
	/// The read model has to be reachable from a binding path: the XAML generator rewrites
	/// <c>{Binding (uno:Validation.Errors)}</c> to <c>(Uno.UI.Xaml.Controls:Validation.Errors)</c>, so the
	/// owner type sits outside the default xmlns.
	/// </summary>
	[TestMethod]
	public void When_Binding_To_Read_Model()
	{
		var source = new TextBox();
		var hasErrors = new Border();
		var errors = new Border();

		hasErrors.SetBinding(
			Border.TagProperty,
			new Binding
			{
				Path = new PropertyPath("(Uno.UI.Xaml.Controls:Validation.HasErrors)"),
				Source = source,
			});
		errors.SetBinding(
			Border.TagProperty,
			new Binding
			{
				Path = new PropertyPath("(Uno.UI.Xaml.Controls:Validation.Errors)"),
				Source = source,
			});

		Assert.AreEqual(false, hasErrors.Tag);

		var reported = new object[] { "must not be empty" };
		Validation.SetHasErrors(source, true);
		Validation.SetErrors(source, reported);

		Assert.AreEqual(true, hasErrors.Tag);
		Assert.AreSame(reported, errors.Tag);
	}

	private partial class SharedBase : Control
	{
		public static DependencyProperty SharedValueProperty { get; } =
			DependencyProperty.Register(
				"SharedValue",
				typeof(double),
				typeof(SharedBase),
				new FrameworkPropertyMetadata(0d));
	}

	private partial class Unannotated : SharedBase;

	[ValidationProperty("SharedValue")]
	private partial class ValidatingLeaf : SharedBase;

	private partial class NonValidatingLeaf : SharedBase;

	private partial class DerivedFromValidatingLeaf : ValidatingLeaf;

	[ValidationProperty("OtherValue")]
	private partial class ShadowingDerived : ValidatingLeaf
	{
		public static DependencyProperty OtherValueProperty { get; } =
			DependencyProperty.Register(
				"OtherValue",
				typeof(double),
				typeof(ShadowingDerived),
				new FrameworkPropertyMetadata(0d));
	}

	[ValidationProperty("")]
	private partial class OptedOutDerived : ValidatingLeaf;

	[ValidationProperty("ThereIsNoSuchProperty")]
	private partial class AnnotatedWithUnknownProperty : SharedBase;
}
