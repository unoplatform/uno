#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests
{
	/// <summary>
	/// Bindable metadata providers match types by full name, so when the same assembly is loaded
	/// in a second AssemblyLoadContext a lookup can return metadata generated for the identically
	/// named type from the other context. Its typed getters then throw InvalidCastException. The
	/// binder must treat such a type-identity mismatch as a miss and fall back to reflection.
	/// </summary>
	// RunWithProvider swaps the static BindingPropertyHelper.BindableMetadataProvider and clears the
	// binder caches; running concurrently with any other test that touches that shared state would flake.
	[TestClass]
	[DoNotParallelize]
	public partial class Given_Binder_MetadataProviderIdentity
	{
		[TestMethod]
		public void When_Provider_Returns_Matching_Type_Then_Metadata_Is_Used()
		{
			var matching = new BindableType(0, typeof(MetadataIdentitySource));

			IBindableType? result = null;
			RunWithProvider(
				new StubMetadataProvider(matching),
				() => result = BindingPropertyHelper.GetValidatedBindableType(typeof(MetadataIdentitySource)));

			Assert.AreSame(matching, result);
		}

		[TestMethod]
		public void When_Provider_Returns_Mismatched_Type_Then_Lookup_Is_A_Miss()
		{
			// Metadata whose Type is NOT the requested type — the shape a cross-ALC full-name match produces.
			var mismatched = new BindableType(0, typeof(MetadataIdentityOther));

			IBindableType? result = null;
			RunWithProvider(
				new StubMetadataProvider(mismatched),
				() => result = BindingPropertyHelper.GetValidatedBindableType(typeof(MetadataIdentitySource)));

			Assert.IsNull(result);
		}

		[TestMethod]
		public void When_Provider_Returns_Mismatched_Type_Then_Binding_Falls_Back_To_Reflection()
		{
			// The mismatched metadata carries a poisoned getter; a binding must ignore it and
			// resolve the real value through reflection.
			var poisoned = new BindableType(1, typeof(MetadataIdentityOther));
			poisoned.AddProperty(
				"Value",
				typeof(string),
				(instance, precedence) => throw new InvalidCastException("poisoned cross-context getter"));

			var target = new MetadataIdentityControl();
			RunWithProvider(new StubMetadataProvider(poisoned), () =>
			{
				target.SetBinding(
					MetadataIdentityControl.MyPropertyProperty,
					new Binding { Path = new PropertyPath("Value") });

				target.DataContext = new MetadataIdentitySource { Value = "reflected" };
			});

			Assert.AreEqual("reflected", target.MyProperty);
		}

		private static void RunWithProvider(IBindableMetadataProvider provider, Action action)
		{
			var previous = BindingPropertyHelper.BindableMetadataProvider;
			BindingPropertyHelper.BindableMetadataProvider = provider;
			BindingPropertyHelper.ClearCaches();

			try
			{
				action();
			}
			finally
			{
				BindingPropertyHelper.BindableMetadataProvider = previous;
				BindingPropertyHelper.ClearCaches();
			}
		}

		private class StubMetadataProvider : IBindableMetadataProvider
		{
			private readonly IBindableType _result;

			public StubMetadataProvider(IBindableType result)
			{
				_result = result;
			}

			public IBindableType GetBindableTypeByType(Type type) => _result;

			public IBindableType GetBindableTypeByFullName(string fullName) => _result;
		}

		public partial class MetadataIdentityControl : FrameworkElement
		{
			public string MyProperty
			{
				get => (string)GetValue(MyPropertyProperty);
				set => SetValue(MyPropertyProperty, value);
			}

			public static readonly DependencyProperty MyPropertyProperty =
				DependencyProperty.Register(nameof(MyProperty), typeof(string), typeof(MetadataIdentityControl), new PropertyMetadata(null));
		}
	}

	// Top-level public on purpose: IsValidMetadataProviderType rejects nested types (Type.IsPublic is
	// false for them), so a nested source would bypass the provider path this fixture exercises.
	public partial class MetadataIdentitySource
	{
		public string? Value { get; set; }
	}

	public partial class MetadataIdentityOther
	{
		public string? Value { get; set; }
	}
}
