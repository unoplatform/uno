using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Controls.Primitives;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Child")]
	public partial class Popover : NativePopupBase
	{
		private const double DefaultPopoverWidth = 320;

		private readonly SerialDisposable _popoverSubscription = new SerialDisposable();
		private UIPopoverController _popover;
		private Panel _host;

		public Popover()
		{
		}

		private void OnPopoverDidDismiss(object sender, EventArgs e)
		{
			this.IsOpen = false;
		}

		protected override void OnChildChanged(UIElement oldChild, UIElement newChild)
		{
			base.OnChildChanged(oldChild, newChild);

			if (_host != null)
			{
				if (oldChild != null)
				{
					_host.RemoveChild(oldChild);
				}

				if (newChild != null)
				{
					_host.AddSubview(newChild);
				}
			}
		}

		protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
		{
			base.OnIsOpenChanged(oldIsOpen, newIsOpen);

			if (newIsOpen)
			{
				Open();
			}
			else
			{
				Close();
			}
		}

		private void Close()
		{
			_popover?.Dismiss(true);
		}

		private void Open()
		{
			CreatePopoverController();

			var size = _popover
				.ContentViewController
				.View
				.Subviews[0]
				.SizeThatFits(
					new CGSize(
						Math.Min(DefaultPopoverWidth, this.MaxWidth),
						this.MaxHeight
					)
				);

			var preferredSize = new CGSize(DefaultPopoverWidth, size.Height);
			_popover.ContentViewController.PreferredContentSize = preferredSize;

			if (preferredSize.Height <= 0)
			{
				this.Log().Warn($"Did not present Popover with preferred size of {preferredSize}");
			}
			else
			{
				_popover.PresentFromRect(
					(Anchor ?? this).Bounds,
					(Anchor ?? this),
					UIPopoverArrowDirection.Down | UIPopoverArrowDirection.Up, // TODO: Expose parameters
					true
				);
			}
		}

		private void CreatePopoverController()
		{
			if (_popover != null)
			{
				return;
			}

			_popover = new UIPopoverController(new UIViewController());

			_host = new Grid()
			{
				Frame = _popover.ContentViewController.View.Bounds,
				AutoresizingMask = UIViewAutoresizing.All,
				TemplatedParent = this.TemplatedParent,
			};

			_popover.ContentViewController.View.AddSubview(_host);

			if (Child != null)
			{
				_host.AddSubview(Child);
			}

			_popover.DidDismiss += OnPopoverDidDismiss;

			_popoverSubscription.Disposable = Disposable.Create(() =>
			{
				if (Child != null)
				{
					Child.RemoveFromSuperview();
				}

				_popover?.Dismiss(false);

				_popover.DidDismiss -= OnPopoverDidDismiss;
				_host.RemoveFromSuperview();
				_popover = null;
				_host = null;
			});
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_popoverSubscription.Disposable = null;
		}

		public UIKit.UIView Anchor
		{
			get => (UIKit.UIView)this.GetValue(AnchorProperty);
			set => SetValue(AnchorProperty, value);
		}

		public static DependencyProperty AnchorProperty { get; } =
			DependencyProperty.Register(
				name: "Anchor",
				propertyType: typeof(UIKit.UIView),
				ownerType: typeof(Popover),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: (UIKit.UIView)null,
					options: FrameworkPropertyMetadataOptions.None)
			);
	}
}
