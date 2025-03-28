using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public class PhaseBindingTests
	{

		[TestMethod]
		public void When_Chained_Binding_And_Null_DataContext_And_Suspend_And_Resume_Bindings_Twice()
		{
			var border = new Border();
			var dc = new MySimpleObject();
			dc.InnerObject.MyTag = "June";
			border.SetBinding(FrameworkElement.TagProperty, new Windows.UI.Xaml.Data.Binding(new PropertyPath("InnerObject.MyTag")));
			border.DataContext = dc;
			Assert.AreEqual("June", border.Tag);

			border.DataContext = null;
			var store = (border as IDependencyObjectStoreProvider).Store;
			store.SuspendBindings();
			store.ResumeBindings();
			store.SuspendBindings();
			store.ResumeBindings(); //Should not throw exception
		}
	}

	public class MySimpleObject
	{
		public MyInnerObject InnerObject { get; }
		public MySimpleObject()
		{
			InnerObject = new MyInnerObject();
		}

		public class MyInnerObject
		{
			public string MyTag { get; set; }
		}
	}
}
