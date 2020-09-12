using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Performance
{
    class Program
    {
		static void Main(string[] args)
		{
			DataContextOnly_GraphPropagation();
			Thread.Sleep(1000);
			GC.Collect(2, GCCollectionMode.Forced);
			DataContextOnly_GraphPropagation();

			// InheritedNamedProperty();

			//DataContextOnly_GraphPropagation();
			//InheritedNamedProperty();
		}

		private static void InheritedNamedProperty()
		{
			var root = new MyNamedPropertyClass();
			BuildGraph02(root);

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < 1000; i++)
			{
				root.FontSize = i;
			}
			sw.Stop();

			Validate02(root);

			Console.WriteLine($"Elapsed {sw.Elapsed} ({MyClass.Counter} elements)");
		}

		private static void Validate02(MyNamedPropertyClass root)
		{
			foreach (var item in root.Flatten(v => v.Children))
			{
				if (item.FontSize != root.FontSize)
				{
					throw new Exception();
				}
			}
		}

		private static void BuildGraph02(MyNamedPropertyClass element, int level = 0)
		{
			if (level < 2)
			{
				for (int i = 0; i < 30; i++)
				{
					var e = new MyNamedPropertyClass();
					element.Children.Add(e);

					BuildGraph02(e, level + 1);
				}
			}
		}

		private static void DataContextOnly_GraphPropagation()
		{
			var root = new MyClass();
			BuildGraph01(root);

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < 1000; i++)
			{
				root.DataContext = i;
			}
			sw.Stop();

			Validate01(root);

			Console.WriteLine($"Elapsed {sw.Elapsed} ({MyClass.Counter} elements)");
		}

		private static void Validate01(MyClass root)
		{
			foreach(var item in root.Flatten(v => v.Children))
			{
				if(item.DataContext != root.DataContext)
				{
					throw new Exception();
				}
			}
		}

		private static void BuildGraph01(MyClass element, int level = 0)
		{
			if (level < 2)
			{
				for (int i = 0; i < 30; i++)
				{
					var e = new MyClass();
					element.Children.Add(e);

					BuildGraph01(e, level + 1);
				}
			}
		}
	}

	public partial class MyClass : DependencyObject
	{
		public static int Counter { get; set; }

		public MyClass()
		{
			Counter++;
			Children = new DependencyObjectCollection<MyClass>(this);
		}

		#region Children DependencyProperty

		public DependencyObjectCollection<MyClass> Children
		{
			get { return (DependencyObjectCollection<MyClass>)GetValue(ChildrenProperty); }
			set { SetValue(ChildrenProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Children.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ChildrenProperty =
			DependencyProperty.Register("Children", typeof(DependencyObjectCollection<MyClass>), typeof(MyClass), new FrameworkPropertyMetadata(null, (s, e) => ((MyClass)s)?.OnChildrenChanged(e)));

		private void OnChildrenChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion
	}

	public partial class MyNamedPropertyClass : DependencyObject
	{
		public MyNamedPropertyClass()
		{
			Children = new DependencyObjectCollection<MyNamedPropertyClass>(this);
		}

		#region Children DependencyProperty

		public DependencyObjectCollection<MyNamedPropertyClass> Children
		{
			get { return (DependencyObjectCollection<MyNamedPropertyClass>)GetValue(ChildrenProperty); }
			set { SetValue(ChildrenProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Children.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ChildrenProperty =
			DependencyProperty.Register("Children", typeof(DependencyObjectCollection<MyNamedPropertyClass>), typeof(MyNamedPropertyClass), new FrameworkPropertyMetadata(null, (s, e) => ((MyNamedPropertyClass)s)?.OnChildrenChanged(e)));

		private void OnChildrenChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion


		#region FontSize DependencyProperty

		public int FontSize
		{
			get { return (int)GetValue(FontSizeProperty); }
			set { SetValue(FontSizeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty FontSizeProperty =
			DependencyProperty.Register(
				name: "FontSize", 
				propertyType: typeof(int), 
				ownerType: typeof(MyNamedPropertyClass),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 0, 
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((MyNamedPropertyClass)s)?.OnFontSizeChanged(e)
				)
			);


		private void OnFontSizeChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion
	}
}
