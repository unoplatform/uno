#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.InputValidation;

[TestClass]
public partial class Given_InputValidationProperty
{
	[TestMethod]
	public void When_No_Attribute()
	{
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(Unannotated)]);
	}

	[TestMethod]
	public void When_Attribute_On_Leaf_Then_Base_Property_Resolves()
	{
		// The case the rejected metadata-flag route could not express: the DP is registered on the shared
		// base, the attribute sits on the leaf, and the sibling leaf must not participate.
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.InputValidation.ValidationProperties[typeof(ValidatingLeaf)]);
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(NonValidatingLeaf)]);
	}

	[TestMethod]
	public void When_Derived_Type_Then_Attribute_Is_Inherited()
	{
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.InputValidation.ValidationProperties[typeof(DerivedFromValidatingLeaf)]);
	}

	[TestMethod]
	public void When_Derived_Redeclares_Then_It_Shadows_The_Base()
	{
		Assert.AreSame(ShadowingDerived.OtherValueProperty, FeatureConfiguration.InputValidation.ValidationProperties[typeof(ShadowingDerived)]);
		Assert.AreSame(SharedBase.SharedValueProperty, FeatureConfiguration.InputValidation.ValidationProperties[typeof(ValidatingLeaf)]);
	}

	[TestMethod]
	public void When_Empty_Name_Then_Opted_Out()
	{
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(OptedOutDerived)]);
	}

	[TestMethod]
	public void When_Unknown_Property_Name()
	{
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(AnnotatedWithUnknownProperty)]);
	}

	[TestMethod]
	public void When_Resolved_Twice_Then_Cached()
	{
		// Negative answers must be cached too, or every non-participating control re-walks its attributes.
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(Unannotated)]);
		Assert.IsNull(FeatureConfiguration.InputValidation.ValidationProperties[typeof(Unannotated)]);

		var first = FeatureConfiguration.InputValidation.ValidationProperties[typeof(ValidatingLeaf)];
		Assert.AreSame(first, FeatureConfiguration.InputValidation.ValidationProperties[typeof(ValidatingLeaf)]);
	}

	[TestMethod]
	public void When_Registered_Then_Participates_Without_The_Attribute()
	{
		// The reason the map is public: a control that cannot carry the attribute — sealed, or from a library
		// that does not reference Uno — is registered from the outside.
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		try
		{
			properties[typeof(RegisteredFromOutside)] = SharedBase.SharedValueProperty;

			Assert.AreSame(SharedBase.SharedValueProperty, properties[typeof(RegisteredFromOutside)]);
		}
		finally
		{
			properties.Remove(typeof(RegisteredFromOutside));
		}
	}

	[TestMethod]
	public void When_Registered_After_Resolution_Then_Registration_Wins()
	{
		// The memoized negative answer must not make the entry immutable.
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		Assert.IsNull(properties[typeof(ResolvedThenRegistered)]);

		try
		{
			properties[typeof(ResolvedThenRegistered)] = SharedBase.SharedValueProperty;

			Assert.AreSame(SharedBase.SharedValueProperty, properties[typeof(ResolvedThenRegistered)]);
		}
		finally
		{
			properties.Remove(typeof(ResolvedThenRegistered));
		}
	}

	[TestMethod]
	public void When_Registered_Null_Then_Opted_Out_Of_Its_Attribute()
	{
		// A registration also opts a type out, and is never overwritten by the attribute it shadows.
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		try
		{
			properties[typeof(RegisteredOptOut)] = null;

			Assert.IsNull(properties[typeof(RegisteredOptOut)]);
		}
		finally
		{
			properties.Remove(typeof(RegisteredOptOut));
		}
	}

	[TestMethod]
	public void When_Registration_Removed_Then_The_Attribute_Resolves_Again()
	{
		var properties = FeatureConfiguration.InputValidation.ValidationProperties;

		properties[typeof(RegistrationRemoved)] = RegistrationRemoved.OtherValueProperty;
		Assert.AreSame(RegistrationRemoved.OtherValueProperty, properties[typeof(RegistrationRemoved)]);

		Assert.IsTrue(properties.Remove(typeof(RegistrationRemoved)));
		Assert.AreSame(SharedBase.SharedValueProperty, properties[typeof(RegistrationRemoved)]);
	}

	/// <summary>
	/// The read model has to be reachable from a binding path. Now that it is a pair of dependency properties
	/// on the control rather than attached ones, the path is an ordinary property name — no parenthesized
	/// attached syntax, and no owner type outside the default xmlns.
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
				Path = new PropertyPath(nameof(TextBox.HasValidationErrors)),
				Source = source,
			});
		errors.SetBinding(
			Border.TagProperty,
			new Binding
			{
				Path = new PropertyPath(nameof(TextBox.ValidationErrors)),
				Source = source,
			});

		Assert.AreEqual(false, hasErrors.Tag);

		source.SetValue(TextBox.HasValidationErrorsProperty, true);

		// Reading the property is what materializes the collection, and so what pushes it to the binding.
		var reported = source.ValidationErrors;
		reported.Add(new InputValidationError("must not be empty"));

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

	[InputValidationProperty("SharedValue")]
	private partial class ValidatingLeaf : SharedBase;

	private partial class NonValidatingLeaf : SharedBase;

	private partial class DerivedFromValidatingLeaf : ValidatingLeaf;

	[InputValidationProperty("OtherValue")]
	private partial class ShadowingDerived : ValidatingLeaf
	{
		public static DependencyProperty OtherValueProperty { get; } =
			DependencyProperty.Register(
				"OtherValue",
				typeof(double),
				typeof(ShadowingDerived),
				new FrameworkPropertyMetadata(0d));
	}

	[InputValidationProperty("")]
	private partial class OptedOutDerived : ValidatingLeaf;

	[InputValidationProperty("ThereIsNoSuchProperty")]
	private partial class AnnotatedWithUnknownProperty : SharedBase;

	private partial class RegisteredFromOutside : SharedBase;

	private partial class ResolvedThenRegistered : SharedBase;

	[InputValidationProperty("SharedValue")]
	private partial class RegisteredOptOut : SharedBase;

	[InputValidationProperty("SharedValue")]
	private partial class RegistrationRemoved : SharedBase
	{
		public static DependencyProperty OtherValueProperty { get; } =
			DependencyProperty.Register(
				"OtherValue",
				typeof(double),
				typeof(RegistrationRemoved),
				new FrameworkPropertyMetadata(0d));
	}
}
