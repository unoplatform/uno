using System;
using System.Collections.Generic;
using System.ComponentModel;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Android.Widget;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Android.Views;

namespace Uno.UI.Controls
{
	public partial class BindableSearchView : SearchView, DependencyObject, INotifyPropertyChanged
	{
		private readonly SerialDisposable _queryTextChangedSubscription = new SerialDisposable();

		private ICommand _submitCommand;
		private TimeSpan _searchTermUpdateMinDelay = TimeSpan.FromMilliseconds(250);

		public BindableSearchView()
			: base(ContextHelper.Current)
		{
			InitializeBinder();

			QueryTextSubmit += (sdn, e) =>
			{
				var command = SubmitCommand;
				if (command != null && command.CanExecute(e.Query))
				{
					command.Execute(e.Query);
				}
			};
		}

		partial void OnAttachedToWindowPartial()
		{
			UpdateSearchTermChangedSubscription();
		}

		partial void OnDetachedFromWindowPartial()
		{
			_queryTextChangedSubscription.Disposable = null;
        }

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			// Force using the Exactly measure spec mode because the SearchView doesn't take the full width when using AtMost
			var exactlyWidthMeasureSpec = ViewHelper.MakeMeasureSpec(ViewHelper.MeasureSpecGetSize(widthMeasureSpec), MeasureSpecMode.Exactly);
			base.OnMeasure(exactlyWidthMeasureSpec, heightMeasureSpec);
		}

		#region SearchTerm
		/// <summary>
		/// Gets or sets the search term
		/// </summary>
		public string SearchTerm
		{
			get { return Query; }
			set
			{
				if (Query != value)
				{
					SetQuery(value, false);
					SetBindingValue(value);
					RaisePropertyChanged();
				}
			}
		}
		#endregion

		#region SearchTermUpdateMinDelay
		/// <summary>
		/// Min delay to wait between 2 updates of SearchTerm
		/// </summary>
		public TimeSpan SearchTermUpdateMinDelay
		{
			get { return _searchTermUpdateMinDelay; }
			set
			{
				if (_searchTermUpdateMinDelay != value)
				{
					_searchTermUpdateMinDelay = value;
					SetBindingValue(value);
					RaisePropertyChanged();

					UpdateSearchTermChangedSubscription();
				}
			}
		}
		#endregion

		#region Watermark
		private string _queryHint;
		/// <summary>
		/// Gets or sets the text to display when the search term is empty.
		/// </summary>
		public string Watermark
		{
			// API 4.4 : get { return QueryHint; }
			get { return _queryHint; }
			set
			{
				// API 4.4 : if (QueryHint != value)
				if (_queryHint != value)
				{
					_queryHint = value;
					SetQueryHint(value);
					SetBindingValue(value);
					RaisePropertyChanged();
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
					RaisePropertyChanged();
				}
			}
		}
		#endregion

		#region SubmitButtonEnabled
		public override bool SubmitButtonEnabled
		{
			get { return base.SubmitButtonEnabled; }
			set
			{
				if (base.SubmitButtonEnabled != value)
				{
					base.SubmitButtonEnabled = value;
					SetBindingValue(value);
					RaisePropertyChanged();
				}
			}
		}
		#endregion

		#region Iconified
		public override bool Iconified
		{
			get { return base.Iconified; }
			set
			{
				if (base.Iconified != value)
				{
					base.Iconified = value;
					SetBindingValue(value);
					RaisePropertyChanged();
				}
			}
		}
		#endregion

		#region QueryRefinementEnabled
		public override bool QueryRefinementEnabled
		{
			get { return base.QueryRefinementEnabled; }
			set
			{
				if (base.QueryRefinementEnabled != value)
				{
					base.QueryRefinementEnabled = value;
					SetBindingValue(value);
					RaisePropertyChanged();
				}
			}
		}
		#endregion

		private void UpdateSearchTermChangedSubscription()
		{
			var text = default(string);
			var timer = new DispatcherTimer { Interval = SearchTermUpdateMinDelay };

			var tickHandler = new EventHandler<object>((snd, args) =>
			{
				((DispatcherTimer)snd).Stop();

				SetBindingValue(text, "SearchTerm");
				RaisePropertyChanged("SearchTerm");
			});
			var textChangedHandler = new EventHandler<QueryTextChangeEventArgs>((snd, args) =>
			{
				timer.Stop();

				text = args.NewText;

				timer.Start();
			});

			timer.Tick += tickHandler;
			QueryTextChange += textChangedHandler;

			_queryTextChangedSubscription.Disposable = Disposable.Create(() =>
			{
				timer.Tick -= tickHandler;
				QueryTextChange -= textChangedHandler;

				timer.Stop();
			});
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		} 
		#endregion

		private Style _style;
		public virtual Style Style
		{
			get
			{
				return _style;
			}
			set
			{
				_style = value ?? Style.DefaultStyleForType(typeof(BindableSearchView));
				_style.ApplyTo(this);
			}
		}
	}
}
