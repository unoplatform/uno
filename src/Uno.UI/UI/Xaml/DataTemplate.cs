#nullable enable

using System;
using System.ComponentModel;

using View = Microsoft.UI.Xaml.UIElement;
// The template factory is exposed as a plain Func so no Uno-specific delegate type leaks into the public API.
using Builder = System.Func<object?, Microsoft.UI.Xaml.TemplateMaterializationSettings, Microsoft.UI.Xaml.UIElement?>;

namespace Microsoft.UI.Xaml
{
	public partial class DataTemplate : FrameworkTemplate
	{
		public DataTemplate() : base(null, (Builder?)null) { }

		/// <summary>
		/// Build a DataTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the DataTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public DataTemplate(object? owner, Builder? factory)
			: base(owner, factory)
		{
		}

		public View? LoadContent() => ((IFrameworkTemplateInternal)this).LoadContent(templatedParent: null);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public View? LoadContent(DependencyObject templatedParent) => ((IFrameworkTemplateInternal)this).LoadContent(templatedParent);

		internal View? LoadContentCached(DependencyObject? templatedParent = null) => base.LoadContentCachedCore(templatedParent);
	}
}

