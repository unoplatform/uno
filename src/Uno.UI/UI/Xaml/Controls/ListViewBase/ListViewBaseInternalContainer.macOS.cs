using System;
using System.Collections.Generic;
using System.Text;
using AppKit;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ListViewBaseInternalContainer : NSCollectionViewItem
	{
		public ContentControl Content
		{
			get => (View as Border)?.Child as ContentControl;
			set
			{
				(View as Border).Child = value;
			}
		}
		public NativeListViewBase Owner { get; internal set; } //Stub

		/// <summary>
		/// Native constructor, do not use explicitly.
		/// </summary>
		/// <remarks>
		/// Used by the Xamarin Runtime to materialize native 
		/// objects that may have been collected in the managed world.
		/// </remarks>
		public ListViewBaseInternalContainer(IntPtr handle) : base(handle) { }

		public ListViewBaseInternalContainer()
		{

		}

		public override void LoadView()
		{
			View = new Border();
		}

		internal IDisposable InterceptSetNeedsLayout()
		{
			return null;
		}

		internal void ClearMeasuredSize()
		{
			// Stub
		}
	}
}
