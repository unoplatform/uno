using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_HtmlPropertyPairs
	{
		private static string[] _flatSink;
		private static (string name, string value)[] _pairsSink;

		[TestMethod]
		public void When_Flatten_Empty()
		{
			var flat = HtmlPropertyPairs.Flatten();

			Assert.AreEqual(0, flat.Length);
		}

		[TestMethod]
		public void When_Flatten_Single_Pair()
		{
			var flat = HtmlPropertyPairs.Flatten(("color", "red"));

			CollectionAssert.AreEqual(new[] { "color", "red" }, flat);
		}

		[TestMethod]
		public void When_Flatten_Multiple_Pairs_Then_Order_Is_Preserved()
		{
			var flat = HtmlPropertyPairs.Flatten(
				("border-style", "solid"),
				("border-color", ""),
				("border-width", "1px 2px 3px 4px"));

			CollectionAssert.AreEqual(
				new[] { "border-style", "solid", "border-color", "", "border-width", "1px 2px 3px 4px" },
				flat);
		}

		[TestMethod]
		public void When_Flatten_From_Array_Then_Layout_Matches_Params()
		{
			var pairs = new[] { ("a", "1"), ("b", "2") };

			CollectionAssert.AreEqual(HtmlPropertyPairs.Flatten(("a", "1"), ("b", "2")), HtmlPropertyPairs.Flatten(pairs));
		}

		/// <summary>
		/// The batched DOM interop entry points take <c>params ReadOnlySpan</c> so that inline call sites
		/// (the vast majority of <c>SetStyle</c>/<c>SetAttribute</c>/<c>SetProperty</c> uses) get a stack
		/// allocated argument buffer instead of a heap allocated tuple array.
		/// </summary>
		[TestMethod]
		public void When_Params_Span_Then_Intermediate_Tuple_Array_Is_Not_Allocated()
		{
			const int Iterations = 1000;
			const int PairCount = 4;

			// Warm-up: keep JIT/tiering allocations out of the measured runs.
			MeasureSpan(16);
			MeasureArray(16);

			var spanBytes = MeasureSpan(Iterations);
			var arrayBytes = MeasureArray(Iterations);

			// A (string, string)[PairCount] payload is PairCount * 2 pointers, on top of the object header.
			var tuplePayload = (long)Iterations * PairCount * 2 * IntPtr.Size;

			Assert.IsTrue(
				arrayBytes - spanBytes >= tuplePayload,
				$"Expected the params ReadOnlySpan overload to save at least {tuplePayload} bytes over {Iterations} calls, " +
				$"but it allocated {spanBytes} bytes against {arrayBytes} bytes for the params array overload.");

			// The flat string[PairCount * 2] handed to the JS interop must remain the only allocation.
			var flatUpperBound = (long)Iterations * ((PairCount * 2 * IntPtr.Size) + 32);

			Assert.IsTrue(
				spanBytes <= flatUpperBound,
				$"Expected the params ReadOnlySpan overload to allocate only the flat interop array " +
				$"(at most {flatUpperBound} bytes over {Iterations} calls), but it allocated {spanBytes} bytes.");

			Assert.IsNotNull(_flatSink);
			Assert.IsNotNull(_pairsSink);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static long MeasureSpan(int iterations)
		{
			var before = GC.GetAllocatedBytesForCurrentThread();

			for (var i = 0; i < iterations; i++)
			{
				_flatSink = HtmlPropertyPairs.Flatten(("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"));
			}

			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static long MeasureArray(int iterations)
		{
			var before = GC.GetAllocatedBytesForCurrentThread();

			for (var i = 0; i < iterations; i++)
			{
				_flatSink = FlattenViaParamsArray(("a", "1"), ("b", "2"), ("c", "3"), ("d", "4"));
			}

			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		/// <summary>
		/// Mirrors the previous <c>params (string name, string value)[]</c> signature. The buffer is published
		/// to a field so it cannot be stack allocated by the JIT, matching the original code where it flowed
		/// through to the JS interop layer.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static string[] FlattenViaParamsArray(params (string name, string value)[] pairs)
		{
			_pairsSink = pairs;

			return HtmlPropertyPairs.Flatten(pairs);
		}
	}
}
