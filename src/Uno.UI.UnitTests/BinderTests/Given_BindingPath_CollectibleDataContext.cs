#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests
{
	/// <summary>
	/// A live binding keeps its reflection accessors cached when the DataContext goes null so a
	/// same-typed rebind is cheap. For a DataContext type from a collectible AssemblyLoadContext
	/// (an unloaded secondary app), the cached <c>ValueGetterHandler</c> closes over a
	/// <c>RuntimeMethodInfo</c> of the dead type — pinning the entire ALC for as long as the
	/// (host-lifetime) binding exists. The accessors must be dropped in that case, while the
	/// cheap-rebind optimization stays intact for non-collectible types.
	/// </summary>
	[TestClass]
	public partial class Given_BindingPath_CollectibleDataContext
	{
		[TestMethod]
		public void When_DataContext_Of_Collectible_Type_Goes_Null_Then_Accessors_Are_Dropped()
		{
			var path = new BindingPath("Value", null);
			var source = CreateCollectibleSource(out var sourceType);

			path.DataContext = source;
			Assert.AreEqual("probe", path.Value, "Pre-condition: the binding resolves the emitted property");

			var item = GetSingleBindingItem(path);
			Assert.IsNotNull(GetItemField(item, "_valueGetter"), "Pre-condition: the value getter is cached while bound");
			Assert.AreSame(sourceType, GetItemField(item, "_dataContextType"), "Pre-condition: the DataContext type is cached while bound");

			path.DataContext = null;

			Assert.IsNull(
				GetItemField(item, "_valueGetter"),
				"The cached getter closes over a RuntimeMethodInfo of the collectible type and pins its ALC — it must be dropped when the DataContext goes null.");
			Assert.IsNull(GetItemField(item, "_substituteValueGetter"), "The substitute getter must be dropped for a collectible DataContext type.");
			Assert.IsNull(GetItemField(item, "_valueSetter"), "The setter must be dropped for a collectible DataContext type.");
			Assert.IsNull(GetItemField(item, "_valueUnsetter"), "The unsetter must be dropped for a collectible DataContext type.");
			Assert.IsNull(GetItemField(item, "_dataContextType"), "The cached DataContext type itself pins the ALC — it must be dropped.");
		}

		[TestMethod]
		public void When_DataContext_Of_NonCollectible_Type_Goes_Null_Then_Accessors_Are_Kept()
		{
			var path = new BindingPath("Value", null);
			var source = new PlainSource();

			path.DataContext = source;
			Assert.AreEqual("probe", path.Value, "Pre-condition: the binding resolves the property");

			var item = GetSingleBindingItem(path);

			path.DataContext = null;

			Assert.AreSame(
				typeof(PlainSource),
				GetItemField(item, "_dataContextType"),
				"For non-collectible DataContext types the accessors stay cached so a same-typed rebind is cheap.");
		}

		[TestMethod]
		public void When_Same_Collectible_Type_Rebinds_Then_Value_Resolves_Again()
		{
			var path = new BindingPath("Value", null);

			var source = CreateCollectibleSource(out _);
			path.DataContext = source;
			Assert.AreEqual("probe", path.Value, "Pre-condition: the binding resolves the emitted property");

			path.DataContext = null;
			path.DataContext = source;

			Assert.AreEqual("probe", path.Value, "Rebinding the same collectible-typed source must rebuild the accessors");
		}

		private static object GetSingleBindingItem(BindingPath path)
			=> typeof(BindingPath)
				.GetField("_chain", BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetValue(path)!;

		private static object? GetItemField(object bindingItem, string fieldName)
			=> bindingItem.GetType()
				.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
				.GetValue(bindingItem);

		/// <summary>
		/// Emits a type with a string property <c>Value</c> (returning "probe") into a
		/// RunAndCollect (collectible) assembly — the same collectibility shape as a view model
		/// from an unloaded secondary app.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static object CreateCollectibleSource(out Type sourceType)
		{
			var typeBuilder = AssemblyBuilder
				.DefineDynamicAssembly(new AssemblyName("CollectibleBindingSourceProbe"), AssemblyBuilderAccess.RunAndCollect)
				.DefineDynamicModule("main")
				.DefineType("CollectibleSource", TypeAttributes.Public);

			var getter = typeBuilder.DefineMethod(
				"get_Value",
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				typeof(string),
				Type.EmptyTypes);
			var il = getter.GetILGenerator();
			il.Emit(OpCodes.Ldstr, "probe");
			il.Emit(OpCodes.Ret);

			typeBuilder
				.DefineProperty("Value", PropertyAttributes.None, typeof(string), null)
				.SetGetMethod(getter);

			sourceType = typeBuilder.CreateType()!;
			Assert.IsTrue(sourceType.Assembly.IsCollectible, "Pre-condition: the probe source type must be collectible");

			return Activator.CreateInstance(sourceType)!;
		}

		public sealed class PlainSource
		{
			public string Value => "probe";
		}
	}
}
