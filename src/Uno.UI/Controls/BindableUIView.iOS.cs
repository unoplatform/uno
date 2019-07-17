using Uno.Collections;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI.DataBinding;
using System.Runtime.CompilerServices;
using System.Drawing;
using Uno.Disposables;
using Windows.UI.Xaml;
using System.ComponentModel;
using Windows.UI.Xaml.Media;

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
		private List<UIView> _shadowChildren = new List<UIView>(0);

		IReadOnlyList<UIView> IShadowChildrenProvider.ChildrenShadow => _shadowChildren;

		internal IReadOnlyList<UIView> ChildrenShadow => _shadowChildren;

		public override void SubviewAdded(UIView uiview)
		{
			base.SubviewAdded(uiview);

			// Reference the list as we don't know where
			// the items has been added other than by getting the complete list.
			// Subviews materializes a new array at every call, which makes it safe to
			// reference.
			_shadowChildren = Subviews.ToList();
		}

		internal IEnumerator<UIView> GetChildrenEnumerator() => _shadowChildren.GetEnumerator();

		public override void WillRemoveSubview(UIView uiview)
		{
			base.WillRemoveSubview(uiview);

			var position = _shadowChildren.IndexOf(uiview, ReferenceEqualityComparer<UIView>.Default);

			if(position != -1)
			{
				var newShadow = _shadowChildren.ToList();
				newShadow.RemoveAt(position);
				_shadowChildren = newShadow;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public BindableUIView()
		{
			Initialize();

			if (FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				ClipsToBounds = true;
			}
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
			var newShadow = _shadowChildren.ToList();

			var view = newShadow[oldIndex];

			newShadow.RemoveAt(oldIndex);
			newShadow.Insert(newIndex, view);

			var reorderIndex = Math.Min(oldIndex, newIndex);

			for (int i = reorderIndex; i < newShadow.Count; i++)
			{
				BringSubviewToFront(newShadow[i]);
			}

			_shadowChildren = newShadow;
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
