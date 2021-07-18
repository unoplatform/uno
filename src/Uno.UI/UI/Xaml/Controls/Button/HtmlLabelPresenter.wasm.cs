using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls
{
	public partial class HtmlLabelPresenter : ContentControl
	{
		public HtmlLabelPresenter() : base("label")
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
		}

		public static readonly DependencyProperty RelatedElementProperty = DependencyProperty.Register(
			"RelatedElement", typeof(UIElement), typeof(HtmlLabelPresenter), new PropertyMetadata(default(UIElement)));

		public UIElement RelatedElement
		{
			get => (UIElement)GetValue(RelatedElementProperty);
			set => SetValue(RelatedElementProperty, value);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == ContentProperty)
			{
				OnContentChanged(args.OldValue as UIElement, args.NewValue as UIElement);
			}
			else if(property == RelatedElementProperty)
			{
				if (args.NewValue is UIElement relatedElement)
				{
					SetAttribute("for", "" + relatedElement.HtmlId);
				}
				else
				{
					RemoveAttribute("for");
				}
			}
		}
		private void OnContentChanged(UIElement? oldElement, UIElement? newElement)
		{
			if (oldElement is { })
			{
				RemoveChild(oldElement);
			}

			if (newElement is { })
			{
				AddChild(newElement);
			}
		}
	}
}
