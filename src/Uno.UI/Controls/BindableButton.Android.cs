using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Windows.Input;
using Uno.Disposables;
using Java.Lang;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.Extensions;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
	public partial class BindableButton : AndroidX.AppCompat.Widget.AppCompatButton, DependencyObject
	{
		private SerialDisposable _commandCanExecute = new SerialDisposable();
		private CompositeDisposable _subscriptions = new CompositeDisposable();

		public BindableButton(Android.Content.Context context)
			: base(context)
		{
			Initialize(context, null);
		}

		public BindableButton(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			Initialize(context, attrs);
		}

		private void Initialize(Android.Content.Context context, IAttributeSet attrs)
		{
			InitializeBinder();

			DisableWhenNoCommand = true; // Default
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			_subscriptions = new CompositeDisposable();

			UpdateEnabledState();
			SetClickListeners();
		}

		protected override void OnDetachedFromWindow()
		{
			base.OnDetachedFromWindow();

			_subscriptions.Dispose();
			_commandCanExecute.Disposable = null;
		}

		private void SetClickListeners()
		{
			this.RegisterClick(BindableButton_Click).DisposeWith(_subscriptions);
			this.RegisterLongClick(BindableButton_LongClick).DisposeWith(_subscriptions);
		}

		void BindableButton_LongClick(object sender, View.LongClickEventArgs e)
		{
			ExecuteCommand(LongClickCommand, LongClickCommandParameter);
		}

		void BindableButton_Click(object sender, EventArgs e)
		{
			ExecuteCommand(ClickCommand, ClickCommandParameter);
		}

		private ICommand _clickCommand;
		public ICommand ClickCommand
		{
			get
			{
				return _clickCommand;
			}
			set
			{
				_clickCommand = value;
				OnCommandChanged(value);
			}
		}

		private void OnCommandChanged(ICommand newCommand)
		{
			_commandCanExecute.Disposable = null;

			if (newCommand != null)
			{
				EventHandler handler = (s, e) => UpdateEnabledState();

				newCommand.CanExecuteChanged += handler;

				_commandCanExecute.Disposable = Disposable
					.Create(() =>
					{
						newCommand.CanExecuteChanged -= handler;
					}
				);
			}

			UpdateEnabledState();
		}

		public bool DisableWhenNoCommand { get; set; }

		private void UpdateEnabledState()
		{
			this.Enabled =
				(!DisableWhenNoCommand && ClickCommand == null)
				||
				(ClickCommand != null && ClickCommand.CanExecute(ClickCommandParameter));
		}

		public object ClickCommandParameter { get; set; }

		public ICommand LongClickCommand { get; set; }
		public object LongClickCommandParameter { get; set; }

		private void ExecuteCommand(ICommand command, object parameter)
		{
			if (command == null)
			{
				return;
			}

			if (!command.CanExecute(parameter ?? default(object)))
			{
				return;
			}

			command.Execute(parameter ?? default(object));
		}
	}
}
