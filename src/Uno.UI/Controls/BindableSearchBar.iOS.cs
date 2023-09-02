using System;
using System.ComponentModel;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Uno.Client;
using CoreGraphics;
using ObjCRuntime;

using Foundation;
using UIKit;

namespace Uno.UI.Controls
{
	public partial class BindableSearchBar : UISearchBar, DependencyObject, System.ComponentModel.INotifyPropertyChanged
	{
		private const string _defaultTextChangedMinDelayLiteral = "0:0:0.250";
		private static readonly TimeSpan _defaultTextChangedMinDelay = TimeSpan.FromMilliseconds(250);
		private const bool _defaultIsAutoLostFocusEnabled = true;

		//private readonly SerialDisposable _textChangedSubscription = new SerialDisposable();

		private ICommand _submitCommand;
		private TimeSpan _textUpdateMinDelay = _defaultTextChangedMinDelay;
		private bool _isAutoLostFocusEnabled = _defaultIsAutoLostFocusEnabled;

		public BindableSearchBar()
		{
			Initialize();
		}

		public BindableSearchBar(CGRect frame)
			: base(frame)
		{
			Initialize();
		}

		public BindableSearchBar(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public BindableSearchBar(NSObjectFlag t)
			: base(t)
		{
			Initialize();
		}

		public BindableSearchBar(NativeHandle handle)
			: base(handle)
		{
			Initialize();
		}


		private void Initialize()
		{
			InitializeBinder();

			UpdateTextChangedSubscription();
			SearchButtonClicked += SubmitQuery;
		}

		#region Placeholder
		/// <inherits />
		public override string Placeholder
		{
			get { return base.Placeholder; }
			set
			{
				if (base.Placeholder != value)
				{
					base.Placeholder = value;
					SetBindingValue(value);
				}
			}
		}
		#endregion

		#region Text
		/// <inherits />
		public override string Text
		{
			get { return base.Text; }
			set
			{
				if (base.Text != value)
				{
					base.Text = value;
					SetBindingValue(value);
				}
			}
		}
		#endregion

		#region TextUpdateMinDelay
		/// <summary>
		/// Min delay to wait between 2 updates of Text
		/// </summary>
		[DefaultValue(typeof(TimeSpan), _defaultTextChangedMinDelayLiteral)]
		public TimeSpan TextUpdateMinDelay
		{
			get { return _textUpdateMinDelay; }
			set
			{
				if (_textUpdateMinDelay != value)
				{
					_textUpdateMinDelay = value;
					SetBindingValue(value);

					UpdateTextChangedSubscription();
				}
			}
		}
		#endregion

		#region SubmitCommand
		/// <summary>
		/// Command to execute when user submit the search query
		/// </summary>
		public ICommand SubmitCommand
		{
			get { return _submitCommand; }
			set
			{
				if (_submitCommand != value)
				{
					_submitCommand = value;
					SetBindingValue(value);
				}
			}
		}
		#endregion

		#region EnablesReturnKeyAutomatically
		/// <inherits />
		public override bool EnablesReturnKeyAutomatically
		{
			get { return base.EnablesReturnKeyAutomatically; }
			set
			{
				if (base.EnablesReturnKeyAutomatically != value)
				{
					base.EnablesReturnKeyAutomatically = value;
					SetBindingValue(value);
				}
			}
		}
		#endregion

		#region IsAutoLostFocusEnabled
		/// <summary>
		/// Gets or sets a boolean which indicates if the search box should lost focus when query is submitted
		/// </summary>
		[DefaultValue(_defaultIsAutoLostFocusEnabled)]
		public bool IsAutoLostFocusEnabled
		{
			get { return _isAutoLostFocusEnabled; }
			set
			{
				if (_isAutoLostFocusEnabled != value)
				{
					_isAutoLostFocusEnabled = value;
					SetBindingValue(value);
				}
			}
		}
		#endregion

		private void UpdateTextChangedSubscription()
		{
			//_textChangedSubscription.Disposable = Uno.UI.Extensions.FrameworkElementExtensions
			//	.FromEventPattern<EventHandler<UISearchBarTextChangedEventArgs>, UISearchBarTextChangedEventArgs>(
			//		h => TextChanged += h,
			//		h => TextChanged -= h,
			//		this,
			//		FrameworkElementExtensions.UiEventSubscriptionsOptions.Default)
			//	.Throttle(TextUpdateMinDelay, this.GetDispatcherScheduler())
			//	.Subscribe(
			//		args =>
			//		{
			//			SetBindingValue(args.EventArgs.SearchText, "Text");
			//			RaisePropertyChanged("Text");
			//		},
			//		e => this.Log().Error("TextChanged subscription failed", e));
		}

		private void SubmitQuery(object sender, EventArgs eventArgs)
		{
			SubmitCommand.ExecuteIfPossible(Text);
			this.EndEditing(true);
		}

		#region INotifyPropertyChanged
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion
	}
}
