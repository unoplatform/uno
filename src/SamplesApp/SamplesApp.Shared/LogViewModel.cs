#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace CheckBoxPointer.ViewModels
{
	public class CheckBoxViewModel : ViewModelBase
	{
		Guid _id = Guid.NewGuid();
		bool _isChecked;
		string _label;

		public Action<CheckBoxViewModel>? OnCheckedChanged;

		public Guid Id => _id;

		public bool IsChecked
		{
			get { return _isChecked; }
			set
			{
				SetAndRaiseIfChanged(ref _isChecked, value);
				OnCheckedChanged?.Invoke(this);
			}
		}

		public string Label
		{
			get { return _label; }
			set
			{
				SetAndRaiseIfChanged(ref _label, value);
			}
		}

		public CheckBoxViewModel(bool isChecked, string label)
		{
			IsChecked = isChecked;
			_label = label ?? throw new ArgumentNullException(nameof(label));
		}
	}
#nullable disable

	public class LogViewModel : ViewModelBase
	{
		ObservableCollection<CheckBoxViewModel> _checkBoxes = new ObservableCollection<CheckBoxViewModel>();
		List<CheckBoxViewModel> _selectedCheckBoxes = new List<CheckBoxViewModel>();

		public ObservableCollection<CheckBoxViewModel> CheckBoxes => _checkBoxes;

		public bool IsMultiSelect => _selectedCheckBoxes.Count > 1;
		public bool IsSingleSelect => _selectedCheckBoxes.Count == 1;

		public LogViewModel() { }

		public void Init(int itemCount = 20)
		{
			_selectedCheckBoxes.Clear();
			_checkBoxes.Clear();
			for (int i = 0; i < itemCount; i++)
			{
				var cb = new CheckBoxViewModel(false, $"CheckBox Item {i + 1}")
				{
					OnCheckedChanged = UpdateSelection
				};
				_checkBoxes.Add(cb);
			}
		}

		private void UpdateSelection(CheckBoxViewModel model)
		{
			if (model != null)
			{
				LoadHelper.DoWork();

				if (model.IsChecked && !_selectedCheckBoxes.Contains(model))
				{
					_selectedCheckBoxes.Add(model);
				}
				else if (!model.IsChecked && _selectedCheckBoxes.Contains(model))
				{
					_selectedCheckBoxes.Remove(model);
				}
			}
			RaisePropertyChanged(nameof(IsMultiSelect));
			RaisePropertyChanged(nameof(IsSingleSelect));
		}
	}

	public static class LoadHelper
	{
		public static void DoWork()
		{
			int i = 0;
			int j = 0;
			var sb = new StringBuilder();
			while (i++ < 1000)
			{
				while (j++ < 10000)
				{
					sb.Append("A");
				}

				j = 0;
			}
		}
	}

	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetAndRaiseIfChanged<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
		{
			if (!EqualityComparer<T>.Default.Equals(backingField, value))
			{
				backingField = value;
				RaisePropertyChanged(propertyName);
			}
		}

		internal void RaisePropertyChanged(string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public abstract class SafeBehaviorBase<T> : Behavior<T> where T : FrameworkElement
	{
		protected bool IsSetup { get; private set; }

		protected override void OnAttached()
		{
			base.OnAttached();

			AssociatedObject.Unloaded += associatedObjectUnloaded;
			AssociatedObject.Loaded += associatedObjectLoaded;

			if (AssociatedObject.IsLoaded)
			{
				setup();
			}
		}

		protected override void OnDetaching()
		{
			AssociatedObject.Unloaded -= associatedObjectUnloaded;
			AssociatedObject.Loaded -= associatedObjectLoaded;

			cleanup();

			base.OnDetaching();
		}

		void associatedObjectLoaded(object sender, RoutedEventArgs e)
		{
			setup();
		}

		void associatedObjectUnloaded(object sender, RoutedEventArgs e)
		{
			cleanup();
		}

		void cleanup()
		{
			if (IsSetup)
			{
				OnCleanup();
				IsSetup = false;
			}
		}

		void setup()
		{
			if (IsSetup == false)
			{
				OnSetup();
				IsSetup = true;
			}
		}

		protected virtual void OnCleanup()
		{

		}

		protected virtual void OnSetup()
		{
		}
	}

	public class ButtonDisableDataGridRowSelectOnClickBehavior : SafeBehaviorBase<ButtonBase>
	{
		protected override void OnSetup()
		{
			base.OnSetup();

			AssociatedObject.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Button_PointerPressed), true);
			AssociatedObject.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(Button_PointerReleased), true);
		}

		protected override void OnCleanup()
		{
			AssociatedObject.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(Button_PointerPressed));
			AssociatedObject.RemoveHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(Button_PointerReleased));

			base.OnCleanup();
		}

		private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			LoadHelper.DoWork();
		}



		private void Button_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			//LoadHelper.DoWork();
		}
	}
}


