using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.Enterprise
{
	public abstract class BaseDxamlTestClass
	{

		internal SafeEventRegistration<TElement, TDelegate> CreateSafeEventRegistration<TElement, TDelegate>(string eventName)
			where TElement : class
			where TDelegate : Delegate
		{
			return new SafeEventRegistration<TElement, TDelegate>(eventName);
		}

		protected static Task RunOnUIThread(Action action)
		{
			return TestServices.RunOnUIThread(action);
		}

		protected static void VERIFY_IS_TRUE(bool value, string message = null)
		{
			TestServices.VERIFY_IS_TRUE(value, message);
		}

		protected static void VERIFY_IS_FALSE(bool value, string message = null)
		{
			TestServices.VERIFY_IS_FALSE(value, message);
		}

		protected static void VERIFY_ARE_EQUAL<T>(T actual, T expected, string message = null)
		{
			TestServices.VERIFY_ARE_EQUAL(actual, expected, message);
		}

		protected static void VERIFY_ARE_NOT_EQUAL<T>(T actual, T unexpected, string message = null)
		{
			TestServices.VERIFY_ARE_NOT_EQUAL(actual, unexpected, message);
		}
		protected static void VERIFY_IS_NULL(object value)
		{
			TestServices.VERIFY_IS_NULL(value);
		}

		protected static void LOG_OUTPUT(string log, params object[] arguments)
		{
			TestServices.LOG_OUTPUT(log, arguments);
		}

		protected static void VERIFY_THROWS_WINRT(Action action, Type exceptionType)
		{
			TestServices.VERIFY_THROWS_WINRT(action, exceptionType);
		}

		protected static void VERIFY_THROWS_WINRT<TExpectedException>(Action action, string text)
			where TExpectedException : Exception
		{
			TestServices.VERIFY_THROWS_WINRT<TExpectedException>(action, text);
		}

		protected static int ARRAYSIZE(Array array)
		{
			return array.Length;
		}

		protected static FrameworkElement FrameworkElement(object o) => o as FrameworkElement;
		protected static Control Control(object o) => o as Control;
		protected static Grid Grid(object o) => o as Grid;
		protected static Flyout Flyout(object o) => o as Flyout;
		protected static StackPanel StackPanel(object o) => o as StackPanel;
		protected static ScrollViewer ScrollViewer(object o) => o as ScrollViewer;
		protected static Border Border(object o) => o as Border;
		protected static Button Button(object o) => o as Button;
		protected static TextBlock TextBlock(object o) => o as TextBlock;
		protected static string String(object o) => o as string;
		protected static SolidColorBrush SolidColorBrush(object o) => o as SolidColorBrush;

		protected static CalendarView CalendarView(object o) => o as CalendarView;
		protected static CalendarDatePicker CalendarDatePicker(object o) => o as CalendarDatePicker;
		protected static CalendarPanel CalendarPanel(object o) => o as CalendarPanel;
		protected static CalendarViewDayItem CalendarViewDayItem(object o) => o as CalendarViewDayItem;
	}
}
