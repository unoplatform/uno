using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace XamlGenerationTests.Shared.Controls
{
	[ContentProperty(Name = "Content")]
	public sealed partial class ImplicitStyleControl : Control
	{
		public ImplicitStyleControl()
		{
		}

		public object Content
		{
			get { return (object)this.GetValue(ContentProperty); }
			set { this.SetValue(ContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register("Content", typeof(object), typeof(ControlWithContent), new FrameworkPropertyMetadata(0));
	}
}
