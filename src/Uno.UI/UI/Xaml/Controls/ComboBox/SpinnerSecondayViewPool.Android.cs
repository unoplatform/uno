using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang.Reflect;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A secondary view pool implementation for the Spinner control.
	/// </summary>
	internal class SpinnerViewPool : ISecondaryViewPool
	{
		private static Field _mRecyclerField;
		private static Field _mScrapField;

		private SparseArray _mScrap;
		private HashSet<View> _views = new HashSet<View>();

		public SpinnerViewPool(SpinnerEx owner)
		{
			Owner = owner;
			InitializeScrap();
		}

		public void RegisterForRecycled(View container, Action action)
			=> throw new NotSupportedException("SpinnerViewPool does not support recycled notification");

		private void InitializeScrap()
		{
			// To be able to determine if we can safely reuse a view, we need to have access to
			// the inner recycler. In normal layouting, the views get recycled properly, but in
			// the case of measuring, (see http://grepcode.com/file/repository.grepcode.com/java/ext/com.google.android/android/5.1.1_r1/android/widget/Spinner.java#734)
			// a view is not created through the recycler, and is created for *every* call to View.measure.
			// This way, if a view is in the poo, has no parent and is not in the spinner recycler this 
			// means we can reuse it properly.

			if (_mRecyclerField == null)
			{
				_mRecyclerField = Owner.Class.Superclass.Superclass.GetDeclaredField("mRecycler");

				if (_mRecyclerField == null)
				{
					throw new NotSupportedException("Unable to find field mRecycler in Spinner class. The current OS version may not be supported.");
				}

				// Make the private field accessible via reflection, so we can call Get on it.
				_mRecyclerField.Accessible = true;
			}

			var mRecycler = _mRecyclerField.Get(Owner);

			if (_mScrapField == null)
			{
				_mScrapField = mRecycler.Class.GetDeclaredField("mScrapHeap");

				if (_mRecyclerField == null)
				{
					throw new NotSupportedException("Unable to find field mRecycler in SpinnerEx class. The current OS version may not be supported.");
				}

				// Make the private field accessible via reflection, so we can call Get on it.
				_mScrapField.Accessible = true;
			}

			_mScrap = _mScrapField.Get(mRecycler) as SparseArray;
		}

		public SpinnerEx Owner { get; }

		public View GetView(int position)
		{
			var nativeRecyclerViews = new List<View>();

			var size = _mScrap.Size();

			for (int i = 0; i < size; i++)
			{
				nativeRecyclerViews.Add(_mScrap.Get(i) as View);
			}

			// The spinner creates temporary view when measuring its size.
			// This item is not placed in the recycler so we can find it this way.
			var view = _views
				.Except(nativeRecyclerViews)
				.Where(v => !v.HasParent())
				.FirstOrDefault();

			if (view != null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("Reusing unassigned view {0}", view);
				}
			}

			return view;
		}

		public void SetActiveView(int position, View view)
		{
			_views.Add(view);
		}

		public View[] GetAllViews()
		{
			return _views.ToArray();
		}
	}
}
