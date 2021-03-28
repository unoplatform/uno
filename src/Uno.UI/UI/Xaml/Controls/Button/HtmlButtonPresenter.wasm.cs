#nullable enable
using System;
using Windows.Foundation;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// This control is designed to be used in the ControlTemplate of a button for a native styling.
	/// </summary>
	public class HtmlButtonPresenter : ContentControl
	{
		public HtmlButtonPresenter() : base("button")
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// Measure child control
			var contentSize = base.MeasureOverride(availableSize);

			Console.WriteLine($"ContentSize: {contentSize}");

			MeasureView(availableSize);

			// Measure the native HTML control
			return MeasureContainerView(contentSize, availableSize);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == BackgroundProperty || property == ForegroundProperty)
			{
				OnRenderChanged();
			}
			else if (property == ContentProperty)
			{
				OnContentChanged(args.OldValue as UIElement, args.NewValue as UIElement);
			}
			else if (property == IsEnabledProperty)
			{
				OnEnabilityChanged((bool)args.NewValue);
			}
		}

		private void OnRenderChanged()
		{
			// Set Foreground & Background here
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

		private void OnEnabilityChanged(bool isEnabled)
		{
			var disabledTxt = isEnabled ? "false" : "true";
			SetProperty("disabled", disabledTxt);
		}
	}
}
