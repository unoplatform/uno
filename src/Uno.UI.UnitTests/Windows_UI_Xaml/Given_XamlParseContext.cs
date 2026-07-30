using System;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_XamlParseContext
	{
		[TestMethod]
		public void When_Alc_Assigned_Then_Returned_While_Alive()
		{
			var alc = new AssemblyLoadContext("Given_XamlParseContext-alive", isCollectible: true);
			try
			{
				var context = new XamlParseContext
				{
					AssemblyName = "NoSuchAssembly_XamlParseContextTest",
					AssemblyLoadContext = alc,
				};

				Assert.AreSame(alc, context.AssemblyLoadContext, "The getter must return the live ALC while it is strongly held by the caller.");
			}
			finally
			{
				alc.Unload();
			}
		}

		[TestMethod]
		public void When_Default_Alc_Assigned_Then_Returned()
		{
			var context = new XamlParseContext
			{
				AssemblyName = "NoSuchAssembly_XamlParseContextTest",
				AssemblyLoadContext = AssemblyLoadContext.Default,
			};

			Assert.AreSame(AssemblyLoadContext.Default, context.AssemblyLoadContext);
		}

		[TestMethod]
		public void When_Collectible_Alc_Unloaded_Then_Context_Does_Not_Pin_It()
		{
			var (context, alcTracker) = CreateContextWithCollectibleAlc();

			for (var i = 0; i < 10 && alcTracker.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				// A second collect after finalizers run reclaims the unloaded collectible ALC
				// deterministically — finalization can release the last references that keep it alive.
				GC.Collect();
			}

			Assert.IsFalse(
				alcTracker.IsAlive,
				"XamlParseContext must hold the AssemblyLoadContext weakly; a strongly-held parse context must not prevent an unloaded collectible ALC from being collected.");

			Assert.IsNull(
				context.AssemblyLoadContext,
				"After the ALC is collected and no same-name assembly is loaded, the getter must return null.");

			GC.KeepAlive(context);
		}

		/// <summary>
		/// The assignment frame is isolated in a non-inlined method so the JIT cannot
		/// extend the lifetime of the local ALC reference into the collection loop of
		/// the calling test.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (XamlParseContext Context, WeakReference AlcTracker) CreateContextWithCollectibleAlc()
		{
			var alc = new AssemblyLoadContext("Given_XamlParseContext-collectible", isCollectible: true);

			var context = new XamlParseContext
			{
				// Deliberately not a loaded assembly: after the ALC is collected, the
				// lazy by-name re-resolution must find nothing and return null.
				AssemblyName = "NoSuchAssembly_XamlParseContextTest",
				AssemblyLoadContext = alc,
			};

			Assert.AreSame(alc, context.AssemblyLoadContext, "Pre-condition: the getter must return the live ALC before unload.");

			var tracker = new WeakReference(alc);
			alc.Unload();

			return (context, tracker);
		}
	}
}
