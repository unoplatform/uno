#nullable enable

using System;
using System.ComponentModel;

using View = Microsoft.UI.Xaml.UIElement;
using FrameworkTemplateBuilder = Uno.UI.FrameworkTemplateBuilder;

namespace Microsoft.UI.Xaml
{
	public partial class DataTemplate : FrameworkTemplate
	{
		public DataTemplate() : base(null, (FrameworkTemplateBuilder?)null) { }

		/// <summary>
		/// Build a DataTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the DataTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		internal DataTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public View? LoadContent() => ((IFrameworkTemplateInternal)this).LoadContent(templatedParent: null);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public View? LoadContent(DependencyObject templatedParent) => ((IFrameworkTemplateInternal)this).LoadContent(templatedParent);

		internal View? LoadContentCached(DependencyObject? templatedParent = null) => base.LoadContentCachedCore(templatedParent);
	}
}

