using System.Windows.Input;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Samples.Helper;

#if NETFX_CORE
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif XAMARIN || NETSTANDARD2_0
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#else
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
#endif

namespace Uno.UI.Samples.Behaviors
{
	// NOTE: ListViewBaseCommand had to be used locally since both ListViewBaseCommand and
	//       SelectorCommand from Umbrella were having problems with the Command Parameter

	public static class ListViewBaseCommand
	{
		private static readonly ConditionalWeakTable<ListViewBase, ItemClickEventHandler> _registeredItemClickEventHandler = new ConditionalWeakTable<ListViewBase, ItemClickEventHandler>();

		#region Attached Properties

		public static DependencyProperty CommandProperty { get ; } =
			DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ListViewBaseCommand), new FrameworkPropertyMetadataHelper(new PropertyChangedCallback(OnCommandChanged)));

		public static DependencyProperty CommandParameterProperty { get ; } =
			DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(ListViewBaseCommand), new FrameworkPropertyMetadata(null));

		public static ICommand GetCommand(ListViewBase obj)
		{
			return (ICommand)obj.GetValue(CommandProperty);
		}

		public static void SetCommand(ListViewBase obj, ICommand value)
		{
			obj.SetValue(CommandProperty, value);
		}

		public static object GetCommandParameter(ListViewBase obj)
		{
			return obj.GetValue(CommandParameterProperty);
		}

		public static void SetCommandParameter(ListViewBase obj, object value)
		{
			obj.SetValue(CommandParameterProperty, value);
		}

		#endregion

		private static void OnCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var listView = sender as ListViewBase;
			if (listView != null)
			{
				if (!listView.IsItemClickEnabled)
				{
					sender.Log().Warn("IsItemClickEnabled is not enabled on the associated list. This must be enabled to make this behavior work");
				}

				// Be sure to not have multiples handlers for the same listview command if command change.
				TryClearCurrentItemCommandEventHandler(listView);

				var hasCommand = e.NewValue as ICommand != null;
				if (hasCommand)
				{
					var eventHandler = new ItemClickEventHandler(ListViewItemClick);

					_registeredItemClickEventHandler.Add(listView, eventHandler);
					listView.ItemClick += eventHandler;
				}
			}
		}

		private static void TryClearCurrentItemCommandEventHandler(ListViewBase listView)
		{
			ItemClickEventHandler eventHandler;
			if (_registeredItemClickEventHandler.TryGetValue(listView, out eventHandler))
			{
				listView.ItemClick -= eventHandler;
				_registeredItemClickEventHandler.Remove(listView);
			}
		}

		private static void ListViewItemClick(object sender, ItemClickEventArgs e)
		{
			RunItemCommand((ListViewBase)sender, e);
		}

		private static void RunItemCommand(ListViewBase listView, ItemClickEventArgs e)
		{
			// Invoke the GetCommand in a static method will help avoid memory leak by not keeping a reference to command in the handler directly, but getting it on demand.
			var command = GetCommand(listView);
			var param = GetCommandParameter(listView) ?? e.ClickedItem;

			if (command.SelectOrDefault(cmd => cmd.CanExecute(param)))
			{
				command.Execute(param);
			}
		}
	}
}
