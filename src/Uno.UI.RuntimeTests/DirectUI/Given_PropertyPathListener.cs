#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.DirectUI;

[TestClass]
[RunsOnUIThread]
public partial class Given_PropertyPathListener;
partial class Given_PropertyPathListener
{
	[TestMethod]
	public void BindPath_DoDp()
	{
		var source = new DepObject { A = "asd" };
		var result = ResolvePath(source, "A");

		Assert.AreEqual(source.A, result);
	}

	[TestMethod]
	public void BindPath_Poco()
	{
		var source = new PlainOldClrObject { A = "asd" };
		var result = ResolvePath(source, "A");

		Assert.AreEqual(source.A, result);
	}

	[TestMethod]
	public void BindPath_Bindable()
	{
		var source = new BindableObject { A = "asd" };
		var result = ResolvePath(source, "A");

		Assert.AreEqual(source.A, result);
	}

	[TestMethod]
	public void BindPath_RecordObject()
	{
		var source = new RecordObject() with { A = "asd" };
		var result = ResolvePath(source, "A");

		Assert.AreEqual(source.A, result);
	}

	[TestMethod]
	public void BindPath_IntIndexer()
	{
		var source = Enumerable.Range(0, 10).ToArray();
		var result = ResolvePath(source, "[2]");

		Assert.AreEqual(source[2], result);
	}

	[TestMethod]
	public void BindPath_StringIndexer()
	{
		var source = new Dictionary<string, object>
		{
			["qwe"] = "asd"
		};
		var result = ResolvePath(source, "[qwe]");

		Assert.AreEqual(source["qwe"], result);
	}

	[TestMethod]
	public void BindPath_AttachedProperty()
	{
		var source = new Border().Apply(x => ToolTipService.SetToolTip(x, "asd"));
		var result = ResolvePath(source, "(ToolTipService.ToolTip)");

		Assert.AreEqual(ToolTipService.GetToolTip(source), result);
	}

	[TestMethod]
	public void BindPath_CustomProperty()
	{
		var source = new TestCustomPropertyProvider(
			Inner: new()
			{
				["A"] = "asd"
			}
		);
		var result = ResolvePath(source, "A");
		var property = source.GetCustomProperty("A");
		var value = property.GetValue(source);

		Assert.AreEqual(value, result);
	}

	[TestMethod]
	public void BindPath_CustomIndexer()
	{
		var source = new TestCustomPropertyProvider(
			InnerIndexer: new()
			{
				["A"] = "asd",
				[1] = 123,
			}
		);
		{
			var result = ResolvePath(source, "[A]");
			var property = source.GetIndexedProperty("Item", typeof(object));
			var value = property.GetIndexedValue(source, "A");

			Assert.AreEqual(value, result);
		}
		{
			var result = ResolvePath(source, "[1]");
			var property = source.GetIndexedProperty("Item", typeof(object));
			var value = property.GetIndexedValue(source, 1);

			Assert.AreEqual(value, result);
		}
	}

	[TestMethod]
	public void BindPath_NestedObjects()
	{
		var source = BuildNestedObjects(@"dodp:Q\bindable:W\poco:Q\record:A\asd");
		var result = ResolvePath(source, "Q.W.Q.A");

		Assert.AreEqual("asd", result);
	}

	[TestMethod]
	public void BindPath_IncorrectPath()
	{
		var source = new PlainOldClrObject { A = "asd" };
		var result = ResolvePath(source, "NoSuchProperty"); // should not throw

		Assert.IsNull(result);
	}

	[TestMethod]
	public void BindPath_MalformedPath()
	{
		var source = new PlainOldClrObject { A = "asd" };

		Assert.Throws<ArgumentException>(() =>
		{
			var result = ResolvePath(source, "asd[0");
		});
	}
}
partial class Given_PropertyPathListener
{
	private static object ResolvePath(object source, string path) => ResolvePath(source, path, out _, out _);
	private static object ResolvePath(object source, string path, out PropertyPathParser parser, out PropertyPathListener listener)
	{
		parser = new PropertyPathParser();
		parser.SetSource(path, null);

		listener = new();
		listener.Initialize(pOwner: null, parser, fListenToChanges: false, fUseWeakReferenceForSource: false);

		listener.SetSource(source);
		return listener.GetValue();
	}

	private static object BuildNestedObjects(string blueprint)
	{
		object current = null;
		foreach (var segment in blueprint.Split('\\').AsEnumerable().Reverse())
		{
			if (segment == "asd")
			{
				if (current != null) throw new FormatException("'Asd' can only be a tail object.");

				current = "asd";
				continue;
			}

			if (segment.Split(':', 2) is not [string type, string { Length: 1 } property] || !"QWAS".Contains(property))
				throw new FormatException($"Invalid segment '{segment}' in: {blueprint}");

			string ThrowIfUncastable() => current is null or string
				? (string)current!
				: throw new FormatException($"Previous segment 'type={segment.GetType().Name}' cannot be assigned to '{segment}' from: {blueprint}'");
			var q = property == "Q" ? current! : default!;
			var w = property == "W" ? current! : default!;
			var a = property == "A" ? ThrowIfUncastable() : default!;
			var s = property == "S" ? ThrowIfUncastable() : default!;

			current = type switch
			{
				"dodp" => new DepObject { Q = q, W = w, A = a, S = s },
				"poco" => new PlainOldClrObject { Q = q, W = w, A = a, S = s },
				"bindable" => new BindableObject { Q = q, W = w, A = a, S = s },
				"record" => new RecordObject(q, w, a, s),

				_ => throw new FormatException($"Invalid '{type}' type in segment '{segment}' from: {blueprint}"),
			};
		}

		return current;
	}
}
partial class Given_PropertyPathListener
{
	public partial class DepObject : DependencyObject
	{
		#region DependencyProperty: Q

		public static DependencyProperty QProperty { get; } = DependencyProperty.Register(
			nameof(Q),
			typeof(object),
			typeof(DepObject),
			new PropertyMetadata(default(object)));

		public object Q
		{
			get => (object)GetValue(QProperty);
			set => SetValue(QProperty, value);
		}

		#endregion
		#region DependencyProperty: W

		public static DependencyProperty WProperty { get; } = DependencyProperty.Register(
			nameof(W),
			typeof(object),
			typeof(DepObject),
			new PropertyMetadata(default(object)));

		public object W
		{
			get => (object)GetValue(WProperty);
			set => SetValue(WProperty, value);
		}

		#endregion
		#region DependencyProperty: A

		public static DependencyProperty AProperty { get; } = DependencyProperty.Register(
			nameof(A),
			typeof(string),
			typeof(DepObject),
			new PropertyMetadata(default(string)));

		public string A
		{
			get => (string)GetValue(AProperty);
			set => SetValue(AProperty, value);
		}

		#endregion
		#region DependencyProperty: S

		public static DependencyProperty SProperty { get; } = DependencyProperty.Register(
			nameof(S),
			typeof(string),
			typeof(DepObject),
			new PropertyMetadata(default(string)));

		public string S
		{
			get => (string)GetValue(SProperty);
			set => SetValue(SProperty, value);
		}

		#endregion
	}

	public class PlainOldClrObject
	{
		public object Q { get; set; }
		public object W { get; set; }

		public string A { get; set; }
		public string S { get; set; }
	}

	[Bindable]
	public partial class BindableObject
	{
		public object Q { get; set; }
		public object W { get; set; }

		public string A { get; set; }
		public string S { get; set; }
	}

	public record class RecordObject(object Q = null, object W = null, string A = null, string S = null);

	public partial record class TestCustomPropertyProvider(Dictionary<string, object> Inner = null, Dictionary<object, object> InnerIndexer = null);
	public partial record class TestCustomPropertyProvider : ICustomPropertyProvider
	{
		public Type Type => typeof(TestCustomPropertyProvider);

		public ICustomProperty GetCustomProperty(string name)
		{
			if (Inner.ContainsKey(name))
			{
				return new TestCustomProperty<object>(name, self => ((TestCustomPropertyProvider)self).Inner[name]);
			}

			return null;
		}
		public ICustomProperty GetIndexedProperty(string name, Type type)
		{
			if (name == "Item")
			{
				return new TestCustomIndexerProperty<object>(name, (self, index) => ((TestCustomPropertyProvider)self).InnerIndexer[index]);
			}

			return null;
		}

		public string GetStringRepresentation() => ToString();
	}
	public record class TestCustomProperty<T>(string Name, Func<object, T> reader = null, Action<object, T> writer = null) : ICustomProperty
	{
		public Type Type => typeof(T);
		public bool CanRead => reader is { };
		public bool CanWrite => writer is { };

		public object GetValue(object target) => CanRead ? reader(target) : throw new InvalidOperationException();
		public void SetValue(object target, object value)
		{
			if (!CanWrite) throw new InvalidOperationException();

			writer(target, (T)value);
		}

		public void SetIndexedValue(object target, object value, object index) => throw new NotImplementedException();
		public object GetIndexedValue(object target, object index) => throw new NotImplementedException();
	}
	public record class TestCustomIndexerProperty<T>(string Name, Func<object, object, T> reader = null, Action<object, object, T> writer = null) : ICustomProperty
	{
		public Type Type => typeof(T);
		public bool CanRead => reader is { };
		public bool CanWrite => writer is { };

		public object GetValue(object target) => throw new NotImplementedException();
		public void SetValue(object target, object value) => throw new NotImplementedException();

		public object GetIndexedValue(object target, object index) => CanRead ? reader(target, index) : throw new InvalidOperationException();
		public void SetIndexedValue(object target, object value, object index)
		{
			if (!CanWrite) throw new InvalidOperationException();

			writer(target, index, (T)value);
		}

	}
}
#endif
