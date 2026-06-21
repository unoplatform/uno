using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_ResourceDictionary_NotFoundCache
	{
		[TestMethod]
		public void When_Collectible_Type_Key_Misses_Then_Cache_Does_Not_Pin_Type()
		{
			var (dictionary, typeTracker) = ProbeDictionaryWithCollectibleTypeKey();

			for (var i = 0; i < 10 && typeTracker.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				// A second collect after finalizers run reclaims the collectible type's LoaderAllocator
				// deterministically — finalization can release the last references keeping it alive.
				GC.Collect();
			}

			Assert.IsFalse(
				typeTracker.IsAlive,
				"A not-found lookup keyed by a collectible Type must not be cached in the dictionary's " +
				"key-not-found cache; the cached RuntimeType pins its LoaderAllocator and the whole " +
				"collectible AssemblyLoadContext after unload.");

			GC.KeepAlive(dictionary);
		}

		[TestMethod]
		public void When_NonCollectible_Type_Key_Misses_Then_Lookup_Still_Misses()
		{
			var dictionary = new ResourceDictionary();

			// Non-collectible types remain cacheable; behavior must stay a miss on re-query.
			Assert.IsFalse(dictionary.TryGetValue(typeof(Given_ResourceDictionary_NotFoundCache), out _, shouldCheckSystem: false));
			Assert.IsFalse(dictionary.TryGetValue(typeof(Given_ResourceDictionary_NotFoundCache), out _, shouldCheckSystem: false));
		}

		/// <summary>
		/// Probes a dictionary with a collectible Type key in a non-inlined frame so the JIT
		/// cannot extend the local Type reference's lifetime into the caller's GC loop.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (ResourceDictionary Dictionary, WeakReference TypeTracker) ProbeDictionaryWithCollectibleTypeKey()
		{
			var collectibleType = EmitCollectibleType();
			Assert.IsTrue(collectibleType.IsCollectible, "Pre-condition: the emitted type must be collectible.");

			var dictionary = new ResourceDictionary();

			// A miss for a typed key (e.g. an implicit-style lookup for a type from a
			// collectible ALC) goes through the key-not-found cache path.
			Assert.IsFalse(dictionary.TryGetValue(collectibleType, out _, shouldCheckSystem: false));

			return (dictionary, new WeakReference(collectibleType));
		}

		private static Type EmitCollectibleType()
		{
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("NotFoundCacheCollectibleProbe"),
				AssemblyBuilderAccess.RunAndCollect);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
			var typeBuilder = moduleBuilder.DefineType("ProbeType", TypeAttributes.Public | TypeAttributes.Class);
			return typeBuilder.CreateType();
		}
	}
}
