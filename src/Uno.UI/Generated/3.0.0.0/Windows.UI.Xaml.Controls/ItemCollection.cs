#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemCollection : global::Windows.Foundation.Collections.IObservableVector<object>,global::System.Collections.Generic.IList<object>,global::System.Collections.Generic.IEnumerable<object>
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ItemCollection.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.VectorChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.VectorChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.GetAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.Size.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.GetView()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.IndexOf(object, out uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.SetAt(uint, object)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.InsertAt(uint, object)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.RemoveAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.Append(object)
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.RemoveAtEnd()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.Clear()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.GetMany(uint, object[])
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.ReplaceAll(object[])
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemCollection.First()
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.Collections.VectorChangedEventHandler<object> VectorChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemCollection", "event VectorChangedEventHandler<object> ItemCollection.VectorChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemCollection", "event VectorChangedEventHandler<object> ItemCollection.VectorChanged");
			}
		}
		#endif
		// Processing: Windows.Foundation.Collections.IObservableVector<object>
		// Processing: System.Collections.Generic.IList<object>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<object>
		[global::Uno.NotImplemented]
		public int IndexOf( object item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<object>
		[global::Uno.NotImplemented]
		public void Insert( int index,  object item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IList<object>
		[global::Uno.NotImplemented]
		public void RemoveAt( int index)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public object this[int index]
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
		// Processing: System.Collections.Generic.ICollection<object>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<object>
		[global::Uno.NotImplemented]
		public void Add( object item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<object>
		[global::Uno.NotImplemented]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<object>
		[global::Uno.NotImplemented]
		public bool Contains( object item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<object>
		[global::Uno.NotImplemented]
		public void CopyTo( object[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.ICollection<object>
		[global::Uno.NotImplemented]
		public bool Remove( object item)
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
		#if false || false || false || false
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
		// Processing: System.Collections.Generic.IEnumerable<object>
		#if false || false || false || false
		// DeclaringType: System.Collections.Generic.IEnumerable<object>
		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IEnumerator<object> GetEnumerator()
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
