using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Drawing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Uno.Collections;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.DataBinding;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Uno.UI.Controls
{
	public partial class BindableUIView : UIView, INotifyPropertyChanged, DependencyObject, IShadowChildrenProvider
	{
		private MaterializableList<UIView> _shadowChildren = new MaterializableList<UIView>();

		List<UIView> IShadowChildrenProvider.ChildrenShadow => _shadowChildren.Materialized;

		internal IReadOnlyList<UIView> ChildrenShadow => _shadowChildren;

		public override void SubviewAdded(UIView uiview)
		{
			base.SubviewAdded(uiview);

			// Reference the list as we don't know where
			// the items has been added other than by getting the complete list.
			// Subviews materializes a new array at every call, which makes it safe to
			// reference.
			_shadowChildren = new MaterializableList<UIView>(Subviews);
		}

		internal List<UIView>.Enumerator GetChildrenEnumerator() => _shadowChildren.Materialized.GetEnumerator();

		public override void WillRemoveSubview(UIView uiview)
		{
			base.WillRemoveSubview(uiview);

			var position = _shadowChildren.IndexOf(uiview, ReferenceEqualityComparer<UIView>.Default);

			if(position != -1)
			{
				_shadowChildren.RemoveAt(position);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public BindableUIView()
		{
			Initialize();
			ClipsToBounds = false;
		}

		public BindableUIView(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}

		public BindableUIView(RectangleF frame)
			: base(frame)
		{
			Initialize();
		}

		public BindableUIView(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public BindableUIView(NSObjectFlag t)
			: base(t)
		{
			Initialize();
		}

		private void Initialize()
		{
			InitializeBinder();
		}

		/// <summary>
		/// Moves a view from one position to another position, without unloading it.
		/// </summary>
		/// <param name="oldIndex">The old index of the item</param>
		/// <param name="newIndex">The new index of the item</param>
		/// <remarks>
		/// The trick for this method is to move the child from one position to the other
		/// without calling RemoveView and AddView. In this context, the only way to do this is
		/// to call BringSubviewToFront, which is the only available method on UIView that manipulates
		/// the index of a view, even if it does not allow for specifying an index.
		/// </remarks>
		internal void MoveViewTo(int oldIndex, int newIndex)
		{
			var view = _shadowChildren[oldIndex];

			_shadowChildren.RemoveAt(oldIndex);
			_shadowChildren.Insert(newIndex, view);

			var reorderIndex = Math.Min(oldIndex, newIndex);

			for (int i = reorderIndex; i < _shadowChildren.Count; i++)
			{
				BringSubviewToFront(_shadowChildren[i]);
			}
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
