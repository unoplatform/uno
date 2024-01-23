#if XAMARIN_IOS_UNIFIED
using UIKit;

namespace Microsoft.UI.Xaml.Controls;

public abstract class NativePage : UIViewController
{
	private UIView _content;

	public NativePage()
	{
		AutomaticallyAdjustsScrollViewInsets = false;
		InitializeComponent();
	}

	/// <summary>
	/// Initializes the content of the current UIViewController.
	/// </summary>
	protected abstract void InitializeComponent();

	public UIView Content
	{
		get => _content;
		set
		{
			if (_content != value)
			{
				if (_content != null)
				{
					_content.RemoveFromSuperview();
				}

				_content = value;
				if (_content != null)
				{

					_content.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
					_content.Frame = View.Frame;

					View.AddSubview(_content);
					View.SetNeedsLayout();
				}
			}
		}
	}
}
