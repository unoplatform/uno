#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DoubleCollection : global::System.Collections.Generic.IList<double>,global::System.Collections.Generic.IEnumerable<double>
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DoubleCollection.Size is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public DoubleCollection() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.DoubleCollection", "DoubleCollection.DoubleCollection()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.DoubleCollection()
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.GetAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.Size.get
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.GetView()
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.IndexOf(double, out uint)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.SetAt(uint, double)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.InsertAt(uint, double)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.RemoveAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.Append(double)
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.RemoveAtEnd()
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.Clear()
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.GetMany(uint, double[])
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.ReplaceAll(double[])
		// Forced skipping of method Windows.UI.Xaml.Media.DoubleCollection.First()
		// Processing: System.Collections.Generic.IList<double>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<double>
		[global::Uno.NotImplemented]
		public int IndexOf( double item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<double>
		[global::Uno.NotImplemented]
		public void Insert( int index,  double item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<double>
		[global::Uno.NotImplemented]
		public void RemoveAt( int index)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public double this[int index]
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.ICollection<double>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<double>
		[global::Uno.NotImplemented]
		public void Add( double item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<double>
		[global::Uno.NotImplemented]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<double>
		[global::Uno.NotImplemented]
		public bool Contains( double item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<double>
		[global::Uno.NotImplemented]
		public void CopyTo( double[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<double>
		[global::Uno.NotImplemented]
		public bool Remove( double item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public bool IsReadOnly
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<double>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IEnumerable<double>
		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IEnumerator<double> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if false || false || false || false
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
