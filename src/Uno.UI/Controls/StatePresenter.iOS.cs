using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Uno.Collections;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

using Foundation;
using UIKit;

namespace Uno.UI.Controls
{
	public class StateChangedEventArgs : RoutedEventArgs
	{
		public StateChangedEventArgs(object originalSource, UIControlState state)
			: base(originalSource)
		{
			State = state;
		}

		public UIControlState State { get; set; }
	}

	public partial class StatePresenter : UIControl, DependencyObject
	{
		internal Action Loaded;
		internal Action Unloaded;

		public RoutedEventHandler<StateChangedEventArgs> StateChanged;

		private bool _highlighted;
		private bool _selected;
		private bool _enabled;

		public StatePresenter()
		{
			InitializeBinder();
		}

		public override void MovedToWindow()
		{
			base.MovedToWindow();

			if (this.Window != null)
			{
				Loaded?.Invoke();
			}
			else
			{
				Unloaded?.Invoke();
			}
		}

		public override bool Enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				RaiseStateChanged();
			}
		}

		public override bool Selected
		{
			get { return _selected; }
			set
			{
				_selected = value;
				RaiseStateChanged();
			}
		}

		public override bool Highlighted
		{
			get { return _highlighted; }
			set
			{
				_highlighted = value;
				RaiseStateChanged();
			}
		}

		private void RaiseStateChanged()
		{
			if (StateChanged != null)
			{
				StateChanged(this, new StateChangedEventArgs(this, State));
			}
		}

		public UIView Child
		{
			get { return Subviews.FirstOrDefault(); }
			set
			{
				foreach (var subview in Subviews)
				{
					subview.RemoveFromSuperview();
				}
				value.Frame = Frame;
				value.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
				Add(value);
			}
		}
	}

	public static class StatePresenterExtensions
	{
		private static readonly UnsafeWeakAttachedDictionary<StatePresenter, string> _managers = new UnsafeWeakAttachedDictionary<StatePresenter, string>();

		public static StatePresenter AddVisualState(this StatePresenter presenter, UIControlState state, Action apply, Action restore)
		{
			var manager = _managers.GetValue(presenter, "Manager", () => new VisualStateManager(presenter));
			manager.Add(state, new ObjectAnimation(apply, restore));
			return presenter;
		}

		private class ObjectAnimation
		{
			private readonly Action _apply;
			private readonly Action _restore;

			public ObjectAnimation(Action apply, Action restore)
			{
				_apply = apply;
				_restore = restore;
			}

			public void Start()
			{
				_apply();
			}

			public void RollBack()
			{
				_restore();
			}
		}

		private class VisualStateManager
		{
			private readonly IDictionary<UIControlState, List<ObjectAnimation>> _visualStates = new Dictionary<UIControlState, List<ObjectAnimation>>();
			private List<ObjectAnimation> _currentState;

			public VisualStateManager(StatePresenter presenter)
			{
				if (presenter.Window != null)
				{
					presenter.StateChanged += GoToState;
				}

				presenter.Loaded += () => presenter.StateChanged += GoToState;
				presenter.Unloaded += () => presenter.StateChanged -= GoToState;
			}

			private void GoToState(object sender, StateChangedEventArgs e)
			{
				if (_currentState != null)
				{
					foreach (var animation in _currentState)
					{
						animation.RollBack();
					}
				}

				if (_visualStates.TryGetValue(e.State, out _currentState))
				{
					foreach (var animation in _currentState)
					{
						animation.Start();
					}
				}
			}

			public void Add(UIControlState state, ObjectAnimation animation)
			{
				_visualStates
					.FindOrCreate(state, () => new List<ObjectAnimation>())
					.Add(animation);
			}
		}
	}
}
