#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.ContentControlTests
{
	/// <summary>
	/// <see cref="ContentControl"/> memoizes "does the default style for this DefaultStyleKey
	/// type set a Template" per <see cref="Type"/>. Keys from a collectible
	/// AssemblyLoadContext's controls pin the unloaded ALC through the memoization dictionary.
	/// ALC teardown must drop the memo (it rebuilds lazily).
	/// </summary>
	[TestClass]
	public class Given_ContentControl_DefaultTemplateMemo
	{
		[TestMethod]
		public void When_CleanupNonDefaultAlcCaches_Then_DefaultTemplate_Memo_Is_Reset()
		{
			var field = typeof(ContentControl).GetField("HasDefaultTemplate", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(field, "ContentControl.HasDefaultTemplate memo field must exist");

			var before = (Func<Type, bool>)field!.GetValue(null)!;

			// Populate the memo with a collectible Type key (the shape an unloaded secondary
			// app's control type would have).
			var collectibleType = EmitCollectibleControlType();
			before(collectibleType);

			Application.CleanupNonDefaultAlcCaches();

			var after = (Func<Type, bool>)field.GetValue(null)!;
			Assert.AreNotSame(
				before,
				after,
				"ALC teardown must replace the memoized default-template lookup; the old memo's " +
				"dictionary keys (collectible Types) pin the unloaded AssemblyLoadContext.");

			// The fresh memo still answers lookups.
			after(typeof(ContentControl));
		}

		private static Type EmitCollectibleControlType()
		{
			if (!RuntimeFeature.IsDynamicCodeSupported)
			{
				Assert.Inconclusive("Reflection.Emit (RunAndCollect) is not supported on this target.");
			}

			var typeBuilder = AssemblyBuilder
				.DefineDynamicAssembly(new AssemblyName("CollectibleDefaultTemplateProbe"), AssemblyBuilderAccess.RunAndCollect)
				.DefineDynamicModule("main")
				.DefineType("CollectibleControl", TypeAttributes.Public, typeof(ContentControl));

			var emitted = typeBuilder.CreateType()!;
			Assert.IsTrue(emitted.Assembly.IsCollectible, "Pre-condition: the probe type must be collectible");
			return emitted;
		}
	}
}
