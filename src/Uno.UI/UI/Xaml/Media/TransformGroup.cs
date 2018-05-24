using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// TransformGroup :  Based on the WinRT TransformGroup
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.transformgroup(v=vs.110).aspx
	/// </summary>
	public partial class TransformGroup : Transform
	{

		protected override void OnAttachedToView()
		{
			base.OnAttachedToView();
			if (View != null)
			{
				foreach (var item in Children)
				{
					item.View = View;
				}
			}
		}

		internal override void OnViewSizeChanged(Size oldSize, Size newSize)
		{
			base.OnViewSizeChanged(oldSize, newSize);

			foreach (var item in Children)
			{
				item.OnViewSizeChanged(oldSize, newSize);
			}
		}

		internal override Foundation.Point Origin
        {
            get { return base.Origin; }
            set
            {
                base.Origin = value;
                foreach (var item in Children)
                {
                    item.Origin = value;
                }
            }
        }     

        public TransformCollection Children
		{
			get
			{
				var collection = (TransformCollection)this.GetValue(ChildrenProperty);
				if (collection == null)
				{
					this.SetValue(ChildrenProperty, collection = new TransformCollection());
				}

				return collection;
			}
			set { this.SetValue(ChildrenProperty, value); }
		}

		public static readonly DependencyProperty ChildrenProperty =
			DependencyProperty.Register("Children", typeof(TransformCollection), typeof(TransformGroup), new PropertyMetadata(null));

	}

}

