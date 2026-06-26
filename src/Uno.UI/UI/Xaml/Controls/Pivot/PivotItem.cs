using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.UI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
#if __ANDROID__
using Android.Views;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PivotItem : ContentControl
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new PivotItemAutomationPeer(this);

		public PivotItem()
		{
			this.HorizontalAlignment = HorizontalAlignment.Stretch;
			this.VerticalAlignment = VerticalAlignment.Stretch;

			this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			this.VerticalContentAlignment = VerticalAlignment.Stretch;

			DefaultStyleKey = typeof(PivotItem);
		}

		public PivotItem(string header) : this()
		{
			Header = header;
		}

		internal PivotHeaderItem PivotHeaderItem { get; set; }

		protected override bool CanCreateTemplateWithoutParent => true;

		public object Header
		{
			get { return this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register("Header", typeof(object), typeof(PivotItem), new FrameworkPropertyMetadata(null, propertyChangedCallback: OnHeaderChanged));

		private static void OnHeaderChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is PivotItem item && item.PivotHeaderItem != null)
			{
				item.PivotHeaderItem.Content = args.NewValue;
			}
		}
	}
}
