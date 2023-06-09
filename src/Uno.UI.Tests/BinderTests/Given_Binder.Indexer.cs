using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.BinderTests
{
	public partial class Given_Binder
	{
		[TestMethod]
		public void When_Binding_DataTable_With_Integer_Indexer()
		{
			var SUT = new TextBlock();
			var dt = new System.Data.DataTable();
			dt.Columns.Add("MyColumn", typeof(int));
			dt.Rows.Add(42);
			SUT.DataContext = dt.DefaultView;
			SUT.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("[0][0]") });

			Assert.AreEqual("42", SUT.Text);
		}

		[TestMethod]
		public void When_Binding_DataTable_With_Integer_Indexer_And_TwoWay()
		{
			var SUT = new TextBlock();
			var dt = new System.Data.DataTable();
			dt.Columns.Add("MyColumn", typeof(int));
			dt.Rows.Add(42);
			SUT.DataContext = dt.DefaultView;
			SUT.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("[0][0]"), Mode = BindingMode.TwoWay });

			Assert.AreEqual("42", SUT.Text);

			SUT.Text = "43";

			Assert.AreEqual(43, dt.Rows[0].ItemArray[0]);
		}

		[TestMethod]
		public void When_Binding_DataTable_With_String_Indexer()
		{
			var SUT = new Grid();
			var dt = new System.Data.DataTable();
			dt.Columns.Add("MyColumn", typeof(int));
			dt.Rows.Add(42);
			SUT.DataContext = dt.DefaultView;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[0][MyColumn]") });

			Assert.AreEqual(42, SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_DataTable_With_String_Indexer_And_TwoWay()
		{
			var SUT = new Grid();
			var dt = new System.Data.DataTable();
			dt.Columns.Add("MyColumn", typeof(int));
			dt.Rows.Add(42);
			SUT.DataContext = dt.DefaultView;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[0][MyColumn]"), Mode = BindingMode.TwoWay });

			Assert.AreEqual(42, SUT.Tag);

			SUT.Tag = 43;

			Assert.AreEqual(43, dt.Rows[0].ItemArray[0]);
		}

		[TestMethod]
		public void When_Binding_IntegerOnly_Indexer_Readonly()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntegerIndexerReadonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]") });

			Assert.AreEqual(43, SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_IntegerOnly_Indexer_Readonly_And_Invalid()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntegerIndexerReadonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]") });

			Assert.IsNull(SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_StringOnly_Indexer_Readonly_And_Integer()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestStringIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]") });

			Assert.AreEqual("421", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_StringOnly_Indexer_Readonly_And_String()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestStringIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]") });

			Assert.AreEqual("toto1", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestIntString_Indexer_Readonly_And_Integer()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntStringIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]") });

			Assert.AreEqual(43, SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestIntString_Indexer_Readonly_And_String()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntStringIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]") });

			Assert.AreEqual("toto1", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestObject_Indexer_Readonly_And_Integer()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestObjectIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]") });

			Assert.AreEqual("421", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestObject_Indexer_Readonly_And_String()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestObjectIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]") });

			Assert.AreEqual("toto1", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestIntObject_Indexer_Readonly_And_Integer()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntObjectIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]") });

			Assert.AreEqual(43, SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_TestIntObject_Indexer_Readonly_And_String()
		{
			var SUT = new Grid();
			SUT.DataContext = new TestIntObjectIndexer_Readonly();
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]") });

			Assert.AreEqual("toto1", SUT.Tag);
		}

		[TestMethod]
		public void When_Binding_IntegerOnly_Indexer_TwoWay()
		{
			var SUT = new Grid();
			var ctx = new TestIntegerIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]"), Mode = BindingMode.TwoWay });

			Assert.AreEqual(0, SUT.Tag);

			SUT.Tag = 44;
			Assert.AreEqual(SUT.Tag, ctx[42]);
		}

		[TestMethod]
		public void When_Binding_IntegerOnly_Indexer_And_Invalid()
		{
			var SUT = new Grid();
			var ctx = new TestIntegerIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = 44;
			Assert.AreEqual(0, ctx[42]);
		}

		[TestMethod]
		public void When_Binding_StringOnly_Indexer_And_Integer()
		{
			var SUT = new Grid();
			var ctx = new TestStringIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("4243", ctx["42"]);
		}

		[TestMethod]
		public void When_Binding_StringOnly_Indexer_And_String()
		{
			var SUT = new Grid();
			var ctx = new TestStringIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("toto43", ctx["toto"]);
		}

		[TestMethod]
		public void When_Binding_TestIntString_Indexer_And_Integer()
		{
			var SUT = new Grid();
			var ctx = new TestIntStringIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]"), Mode = BindingMode.TwoWay });

			Assert.AreEqual(0, SUT.Tag);

			SUT.Tag = 43;

			Assert.AreEqual(43, ctx[42]);
		}

		[TestMethod]
		public void When_Binding_TestIntString_Indexer_And_String()
		{
			var SUT = new Grid();
			var ctx = new TestIntStringIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("toto43", ctx["toto"]);
		}

		[TestMethod]
		public void When_Binding_TestObject_Indexer_And_Integer()
		{
			var SUT = new Grid();
			var ctx = new TestObjectIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("4342", ctx["42"]);
		}

		[TestMethod]
		public void When_Binding_TestObject_Indexer_And_String()
		{
			var SUT = new Grid();
			var ctx = new TestObjectIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("43toto", ctx["toto"]);
		}

		[TestMethod]
		public void When_Binding_TestIntObject_Indexer_And_Integer()
		{
			var SUT = new Grid();
			var ctx = new TestIntObjectIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[42]"), Mode = BindingMode.TwoWay });

			Assert.AreEqual(0, SUT.Tag);

			SUT.Tag = 43;

			Assert.AreEqual(43, ctx[42]);
		}

		[TestMethod]
		public void When_Binding_TestIntObject_Indexer_And_String()
		{
			var SUT = new Grid();
			var ctx = new TestIntObjectIndexer();
			SUT.DataContext = ctx;
			SUT.SetBinding(Grid.TagProperty, new Binding { Path = new PropertyPath("[toto]"), Mode = BindingMode.TwoWay });

			Assert.IsNull(SUT.Tag);

			SUT.Tag = "43";

			Assert.AreEqual("43toto", ctx["toto"]);
		}


		public class TestIntegerIndexerReadonly
		{
			public int this[int index]
				=> index + 1;
		}

		public class TestStringIndexer_Readonly
		{
			public string this[string value]
				=> value + "1";
		}

		public class TestIntStringIndexer_Readonly
		{
			public int this[int index]
				=> index + 1;

			public string this[string value]
				=> value + "1";
		}

		public class TestObjectIndexer_Readonly
		{
			public string this[object index]
				=> index.ToString() + "1";
		}

		public class TestIntObjectIndexer_Readonly
		{
			public int this[int index]
				=> index + 1;

			public string this[object index]
				=> index.ToString() + "1";
		}

		public class TestIntegerIndexer
		{
			private int _intValue;

			public int this[int index]
			{
				get => _intValue;
				set => _intValue = value;
			}
		}

		public class TestStringIndexer
		{
			private string _stringValue;

			public string this[string v]
			{
				get => _stringValue;
				set => _stringValue = v + value;
			}
		}

		public class TestIntStringIndexer
		{
			private string _stringValue;
			private int _intValue;

			public int this[int index]
			{
				get => _intValue;
				set => _intValue = value;
			}

			public string this[string v]
			{
				get => _stringValue;
				set => _stringValue = v + value;
			}
		}

		public class TestObjectIndexer
		{
			private string _stringValue;

			public string this[object index]
			{
				get => _stringValue;
				set => _stringValue = value + index;
			}
		}

		public class TestIntObjectIndexer
		{
			private string _stringValue;
			private int _intValue;


			public int this[int index]
			{
				get => _intValue;
				set => _intValue = value;
			}

			public string this[object index]
			{
				get => _stringValue;
				set => _stringValue = value + index;
			}
		}
	}
}
