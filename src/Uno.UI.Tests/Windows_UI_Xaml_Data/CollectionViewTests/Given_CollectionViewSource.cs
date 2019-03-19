using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.CollectionViewTests
{
	[TestClass]
	public class Given_CollectionViewSource
	{
		[TestMethod]
		public void When_Source_Is_Not_Enumerable()
		{
			var SUT = new CollectionViewSource();

			Assert.IsNull(SUT.View);

			SUT.Source = 42;

			Assert.IsNull(SUT.View);
		}

		[TestMethod]
		public void When_Source_IsArray()
		{
			var SUT = new CollectionViewSource();

			Assert.IsNull(SUT.View);

			SUT.Source = new[] { 42 };

			Assert.IsNotNull(SUT.View);
			Assert.AreEqual(1, SUT.View.Count);
		}

		[TestMethod]
		public void When_Source_Notify()
		{
			var SUT = new CollectionViewSource();
			int viewChanged = 0;
			SUT.RegisterPropertyChangedCallback(CollectionViewSource.ViewProperty, (s, e) => viewChanged++);

			Assert.IsNull(SUT.View);

			SUT.Source = new[] { 42 };

			Assert.IsNotNull(SUT.View);
			Assert.AreEqual(1, SUT.View.Count);
			Assert.AreEqual(1, viewChanged);
		}
	}
}
