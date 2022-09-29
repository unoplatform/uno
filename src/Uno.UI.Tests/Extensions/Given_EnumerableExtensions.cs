#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.Extensions.Specialized;

namespace Umbrella.Views.UI.Tests.Shared.Extensions
{
	[TestClass]
	public class Given_EnumerableExtensions
	{
		private Mock<IList> _listMock;
		private Mock<IEnumerable> _enumerableMock;

		[TestInitialize]
		public void Setup()
		{
			_listMock = new Mock<IList>(MockBehavior.Strict);
			_listMock.Setup(l => l.Count).Returns(1);
			_listMock.Setup(l => l[It.IsAny<int>()]).Returns(new object());

			_enumerableMock = new Mock<IEnumerable>(MockBehavior.Strict);
			_enumerableMock.Setup(l => l.GetEnumerator()).Returns(new object[1].GetEnumerator());
		}

		[TestMethod]
		public void When_Calling_ForEach_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.ForEach((object o) => { });

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_ForEach_On_List_Then_NoEnumeration()
		{
			_listMock.Object.ForEach((object o) => { });

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}

		[TestMethod]
		public void When_Calling_Any_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.Any();

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_Any_On_List_Then_NoEnumeration()
		{
			_listMock.Object.Any();

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}

		[TestMethod]
		public void When_Calling_None_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.None();

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_None_On_List_Then_NoEnumeration()
		{
			_listMock.Object.None();

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}

		[TestMethod]
		public void When_Calling_ElementAt_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.ElementAt(0);

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_ElementAt_On_List_Then_NoEnumeration()
		{
			_listMock.Object.ElementAt(0);

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}

		[TestMethod]
		public void When_Calling_ElementAtOrDefault_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.ElementAtOrDefault(0);

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_ElementAtOrDefault_On_List_Then_NoEnumeration()
		{
			_listMock.Object.ElementAtOrDefault(0);

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}

		[TestMethod]
		public void When_Calling_Count_On_Enumerable_Then_Enumeration()
		{
			_enumerableMock.Object.Count();

			_enumerableMock.Verify(l => l.GetEnumerator(), Times.Once(), "Enumerable should be enumerated");
		}

		[TestMethod]
		public void When_Calling_Count_On_List_Then_NoEnumeration()
		{
			_listMock.Object.Count();

			_listMock.Verify(l => l.GetEnumerator(), Times.Never(), "List should not be enumerated");
		}
	}
}
