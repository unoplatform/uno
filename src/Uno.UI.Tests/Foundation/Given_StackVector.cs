using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Collections;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_StackVector
	{
		private struct MyStruct
		{
			internal int A;
		}

		[TestMethod]
		public void EmptyStackVector()
		{
			var sut = new StackVector<MyStruct>(2);

			sut.Count.Should().Be(0);
			sut.Should().HaveCount(0);

			sut.ToArray().Should().HaveCount(0);
			sut.Memory.Length.Should().Be(0);
		}

		[TestMethod]
		public void NonEmptyStackVector_Then_CountCheck()
		{
			var sut = new StackVector<MyStruct>(2, 2);

			sut.Count.Should().Be(2);
			sut.Should().HaveCount(2);

			sut.ToArray().Should().HaveCount(2);
			sut.Memory.Length.Should().Be(2);
		}

		[TestMethod]
		public unsafe void ItemsInStackVector()
		{
			var sut = new StackVector<MyStruct>(2);

			ref var item1 = ref sut.PushBack();
			item1.A = 1;

			sut.Count.Should().Be(1);
			sut.Should().HaveCount(1);

			ref var item2 = ref sut.PushBack();
			item2.A = 2;

			sut.Count.Should().Be(2);
			sut.Should().HaveCount(2);

			sut.Select(i => i.A)
				.Should()
				.BeEquivalentTo(new[] { 1, 2 });

			var ptr1 = Unsafe.AsPointer(ref item1);
			var ptr2 = Unsafe.AsPointer(ref item2);

			foreach (ref var item in sut.Memory.Span)
			{
				// Ensure we're getting the same references

				var ptr = Unsafe.AsPointer(ref item);
				if (ptr != ptr1 && ptr != ptr2)
				{
					Assert.Fail("Lost reference");
				}
			}
		}

		[TestMethod]
		public void TestResizingShouldCopyOldElements()
		{
			var sut = new StackVector<int>(2);

			for (int i = 0; i < 200; i++)
			{
				ref var item1 = ref sut.PushBack();
				item1 = i;
			}

			sut.Count.Should().Be(200);
			sut.Should().HaveCount(200);

			for (int i = 0; i < 200; i++)
			{
				Assert.AreEqual(i, sut[i]);
			}
		}
	}
}
