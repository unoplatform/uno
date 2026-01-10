#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise.AutoSuggestBoxTests;

[TestClass]
public class AutoSuggestBoxIntegrationTests : BaseDxamlTestClass
{
	// Helper class matching SuggestionListObject from C++
	private class SuggestionListObject
	{
		public SuggestionListObject(string title)
		{
			Title = title;
		}

		public string Title { get; set; }

		public override string ToString() => Title;
	}

	// Test helper class
	private class TestItem
	{
		public string Title { get; set; }
		public string Description { get; set; }

		public override string ToString() => Title;
	}

	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	#region Test Cases

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanInstantiate()
	{
		var act = () => new AutoSuggestBox();
		act.Should().NotThrow();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var asb = new AutoSuggestBox();
		bool loaded = false;
		bool unloaded = false;
		asb.Loaded += (s, e) => loaded = true;
		asb.Unloaded += (s, e) => unloaded = true;
		TestServices.WindowHelper.WindowContent = asb;
		await TestServices.WindowHelper.WaitFor(() => loaded);

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitFor(() => unloaded);
	}

	[TestMethod]
	public async Task CanGetAndSetProperties()
	{
		var itemList = new List<string> { "Red", "Blue", "Yellow" };

		await RunOnUIThread(() =>
		{
			var autoSuggestBox = new AutoSuggestBox();

			VERIFY_IS_TRUE(autoSuggestBox.AutoMaximizeSuggestionArea);
			autoSuggestBox.AutoMaximizeSuggestionArea = false;
			VERIFY_IS_FALSE(autoSuggestBox.AutoMaximizeSuggestionArea);

			VERIFY_IS_NULL(autoSuggestBox.ItemsSource);
			autoSuggestBox.ItemsSource = itemList;
			VERIFY_IS_TRUE(autoSuggestBox.ItemsSource == itemList);

			VERIFY_IS_FALSE(autoSuggestBox.IsSuggestionListOpen);

			VERIFY_IS_TRUE(autoSuggestBox.UpdateTextOnSelect);
			autoSuggestBox.UpdateTextOnSelect = false;
			VERIFY_IS_FALSE(autoSuggestBox.UpdateTextOnSelect);

			VERIFY_IS_TRUE(autoSuggestBox.Text == "");
			autoSuggestBox.Text = "Auto Suggest Box";
			VERIFY_IS_TRUE(autoSuggestBox.Text == "Auto Suggest Box");

			VERIFY_IS_TRUE(autoSuggestBox.PlaceholderText == "");
			autoSuggestBox.PlaceholderText = "Placeholder Text";
			VERIFY_IS_TRUE(autoSuggestBox.PlaceholderText == "Placeholder Text");

			VERIFY_IS_TRUE(autoSuggestBox.TextMemberPath == "");
			autoSuggestBox.TextMemberPath = "Text Member Path";
			VERIFY_IS_TRUE(autoSuggestBox.TextMemberPath == "Text Member Path");
		});
	}

	[TestMethod]
	public async Task CanRaiseTextChangedEvent()
	{
		AutoSuggestBox autoSuggestBox = null;
		var textChangedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();

			autoSuggestBox.TextChanged += (s, e) =>
			{
				VERIFY_IS_TRUE(autoSuggestBox.Text == "Test String");
				textChangedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.Text = "Test String";
		});

		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		VERIFY_IS_TRUE(textChangedEvent.Task.IsCompleted);
	}

	[TestMethod]
	public async Task CanRaiseTextChangedEventInFlyout()
	{
		Button button = null;
		AutoSuggestBox autoSuggestBox = null;
		Flyout flyout = null;

		var textChangedEvent = new TaskCompletionSource<bool>();
		var flyoutOpenedEvent = new TaskCompletionSource<bool>();
		var flyoutClosedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)(XamlReader.Load(
				"""
				<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
				      x:Name='root' Background='SlateBlue' Width='400' Height='200' VerticalAlignment='Top' HorizontalAlignment='Left'>
				  <Button x:Name='button' Content='button.flyout' HorizontalAlignment='Left' FontSize='25' >
				    <Button.Flyout>
				      <Flyout Placement='Top'>
				        <AutoSuggestBox x:Name='ASB'/>
				      </Flyout>
				    </Button.Flyout>
				  </Button>
				</Grid>
				"""));

			autoSuggestBox = (AutoSuggestBox)(rootPanel.FindName("ASB"));
			button = (Button)(rootPanel.FindName("button"));
			flyout = (Flyout)(button.Flyout);

			autoSuggestBox.TextChanged += (s, e) =>
			{
				VERIFY_IS_TRUE(autoSuggestBox.Text == "Test String");
				textChangedEvent.TrySetResult(true);
			};

			flyout.Opened += (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Opened event is fired!");
				flyoutOpenedEvent.TrySetResult(true);
			};

			flyout.Closed += (s, e) =>
			{
				LOG_OUTPUT("PopupOpenClose: Flyout Closed event is fired!");
				flyoutClosedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Button Tap operation to show the Flyout.");
		TestServices.InputHelper.Tap(button);
		await Task.WhenAny(flyoutOpenedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
		await RunOnUIThread(() =>
		{
			autoSuggestBox.Text = "Test String";
		});
		await TestServices.WindowHelper.WaitForIdle();
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("RootPanel Tap operation to close the Flyout.");
			flyout.Hide();
		});
		await Task.WhenAny(flyoutClosedEvent.Task, Task.Delay(5000));
	}

	[TestMethod]
	public async Task ValidateTextBoxStyle()
	{
		AutoSuggestBox autoSuggestBox = null;
		TextBox textBox = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
				<StackPanel.Resources>
				  <Style x:Name='asbStyle' TargetType='TextBox'>
				      <Setter Property='BorderBrush' Value='Red' />
				      <Setter Property='Background' Value='Green' />
				      <Setter Property='Margin' Value='50' />
				  </Style>
				</StackPanel.Resources>
				  <AutoSuggestBox x:Name='autoSuggestBox' TextBoxStyle='{StaticResource asbStyle}' />
				  <Button x:Name='button1' Content='Button' />
				</StackPanel>
				"""));

			autoSuggestBox = (AutoSuggestBox)(rootPanel.FindName("autoSuggestBox"));
			VERIFY_IS_NOT_NULL(autoSuggestBox);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			VERIFY_IS_NOT_NULL(textBox);
		});

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(((SolidColorBrush)textBox.Background).Color, Microsoft.UI.Colors.Green);
		});
	}

	[TestMethod]
	public async Task CanRaiseSuggestionChosenEvent()
	{
		AutoSuggestBox autoSuggestBox = null;
		Button button1 = null;
		TextBox textBox = null;
		Popup popup = null;
		ListView listView = null;
		TextBlock textBlock1 = null;

		var gotFocusEvent = new TaskCompletionSource<bool>();
		var gotFocusButtonEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var textChanged2Event = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var suggestionChosenEvent = new TaskCompletionSource<bool>();

		var itemList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			var rootPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >
				  <Rectangle Fill='Blue' />
				  <AutoSuggestBox x:Name='autoSuggestBox' />
				  <Button x:Name='button1' Content='Button' />
				  <TextBlock x:Name='textBlock1' FontSize='20' />
				</StackPanel>
				"""));

			autoSuggestBox = (AutoSuggestBox)(rootPanel.FindName("autoSuggestBox"));
			VERIFY_IS_NOT_NULL(autoSuggestBox);

			textBlock1 = (TextBlock)(rootPanel.FindName("textBlock1"));
			VERIFY_IS_NOT_NULL(textBlock1);

			button1 = (Button)(rootPanel.FindName("button1"));
			VERIFY_IS_NOT_NULL(button1);

			autoSuggestBox.GotFocus += (s, e) =>
			{
				LOG_OUTPUT("CanRaiseSuggestionChosenEvent: GotFocus event fired on ASB!");
				gotFocusEvent.TrySetResult(true);
			};

			button1.GotFocus += (s, e) =>
			{
				LOG_OUTPUT("CanRaiseSuggestionChosenEvent: GotFocus event fired on Button!");
				gotFocusButtonEvent.TrySetResult(true);
			};

			autoSuggestBox.TextChanged += (s, e) =>
			{
				LOG_OUTPUT($"CanRaiseSuggestionChosenEvent: TextChanged event fired on ASB! Input Text = {autoSuggestBox.Text}");
				textChangedEvent.TrySetResult(true);
			};

			autoSuggestBox.SuggestionChosen += (s, e) =>
			{
				LOG_OUTPUT("CanRaiseSuggestionChosenEvent: SuggestionChosen event fired!!!");
				textBlock1.Text = (string)e.SelectedItem;
				VERIFY_IS_TRUE(textBlock1.Text == "Sapphire");
				suggestionChosenEvent.TrySetResult(true);
			};

			autoSuggestBox.Margin = new Thickness(20);
			autoSuggestBox.ItemsSource = itemList;

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			VERIFY_IS_NOT_NULL(textBox);
			popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			VERIFY_IS_NOT_NULL(popup);

			popup.Opened += (s, e) =>
			{
				LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Opened event fired!");
				popupOpenedEvent.TrySetResult(true);
			};
		});

		LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Tap on ASB.");
		TestServices.InputHelper.Tap(textBox);

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		// Keyboard input "R" to show the suggestion list.
		LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Keyboard Input - R");
		await TestServices.KeyboardHelper.PressKeySequence("$d$_shift#$d$_r#$u$_r#$u$_shift");

		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Get the listView object.");
			listView = (ListView)((FrameworkElement)popup.Child).FindName("SuggestionsList");
			if (listView == null)
			{
				// Try to find it in the visual tree
				listView = FindVisualChildByName<ListView>(popup.Child, "SuggestionsList");
			}
			VERIFY_IS_NOT_NULL(listView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Tap on the suggestion list.");
		TestServices.InputHelper.Tap(listView);

		await Task.WhenAny(suggestionChosenEvent.Task, Task.Delay(5000));

		// Ensure the close down the SIP and clear the content.
		LOG_OUTPUT("CanRaiseSuggestionChosenEvent: Tap on the button1.");
		TestServices.InputHelper.Tap(button1);

		await Task.WhenAny(gotFocusButtonEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
	}

	// TODO UNO: CanTraceTelemetryData test skipped - telemetry tracing not available in Uno Platform
	// Original C++:
	// void AutoSuggestBoxIntegrationTests::CanTraceTelemetryData()
	// {
	//     StartTracingTelemetry();
	//     CanRaiseSuggestionChosenEvent();
	//     StopTracingTelemetry();
	//     TraceConsumer::VerifyEventTraced("ASBSuggestionListOpened", 1);
	//     TraceConsumer::VerifyEventTraced("ASBSuggestionSelectionChanged", 1);
	// }

	[TestMethod]
	public async Task ValidateQuerySubmittedContainsCurrentText()
	{
		AutoSuggestBox autoSuggestBox = null;
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		string testString = "I am a string.";

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.QueryIcon = new SymbolIcon(Symbol.Find);

			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				VERIFY_ARE_EQUAL(testString, e.QueryText);
				VERIFY_IS_NULL(e.ChosenSuggestion);
				querySubmittedEvent.TrySetResult(true);
			};

			autoSuggestBox.Text = testString;

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();

		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		VERIFY_IS_TRUE(querySubmittedEvent.Task.IsCompleted);
	}

	[TestMethod]
	public async Task CanSetQueryButtonIcon()
	{
		AutoSuggestBox autoSuggestBox = null;
		SymbolIcon symbolIcon = null;

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.QueryIcon = new SymbolIcon(Symbol.Find);
			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			symbolIcon = new SymbolIcon();
			symbolIcon.Symbol = Symbol.Accept;
			autoSuggestBox.QueryIcon = symbolIcon;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var queryButton = (ButtonBase)autoSuggestBox.GetTemplateChild("QueryButton");
			var actualSymbolIcon = queryButton.Content as SymbolIcon;
			VERIFY_IS_NOT_NULL(actualSymbolIcon);
			VERIFY_ARE_EQUAL(symbolIcon, actualSymbolIcon);
		});
	}

	[TestMethod]
	public async Task ValidateQueryButtonIsCollapsedWithNoQueryIcon()
	{
		AutoSuggestBox autoSuggestBox = null;

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var queryButton = (ButtonBase)autoSuggestBox.GetTemplateChild("QueryButton");
			VERIFY_IS_NOT_NULL(queryButton);
			VERIFY_ARE_EQUAL(Visibility.Collapsed, queryButton.Visibility);
		});
	}

	[TestMethod]
	public async Task ValidateAutoSuggestBoxWorksWithoutQueryButton()
	{
		// TODO UNO: This test requires loading a custom XAML template file.
		// Original C++ uses LoadXamlFileOnUIThread to load AutoSuggestBoxWithoutQueryButton.xaml
		// For now, we'll create the ASB without a query button programmatically.

		AutoSuggestBox autoSuggestBox = null;
		Popup popup = null;
		var gotFocusEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();
		var suggestionChosenEvent = new TaskCompletionSource<bool>();
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		var itemList = new List<string> { "this", "that", "the other" };
		string testString = "this";

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			// No QueryIcon set, so QueryButton will be collapsed

			autoSuggestBox.GotFocus += (s, e) =>
			{
				gotFocusEvent.TrySetResult(true);
			};

			autoSuggestBox.TextChanged += (s, e) =>
			{
				textChangedEvent.TrySetResult(true);
			};

			autoSuggestBox.SuggestionChosen += (s, e) =>
			{
				var actualString = (string)e.SelectedItem;
				VERIFY_ARE_EQUAL(testString, actualString);
				suggestionChosenEvent.TrySetResult(true);
			};

			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				querySubmittedEvent.TrySetResult(true);
			};

			autoSuggestBox.ItemsSource = itemList;

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");

			popup.Opened += (s, e) =>
			{
				popupOpenedEvent.TrySetResult(true);
			};

			popup.Closed += (s, e) =>
			{
				popupClosedEvent.TrySetResult(true);
			};

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_t#$u$_t");

		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Down();

		await Task.WhenAny(suggestionChosenEvent.Task, Task.Delay(5000));

		// Reset for the second wait
		textChangedEvent = new TaskCompletionSource<bool>();

		await TestServices.KeyboardHelper.Enter();

		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateDataContextDoesNotPropagateIntoHeader()
	{
		AutoSuggestBox autoSuggestBox = null;
		StackPanel stackPanel = null;

		await RunOnUIThread(() =>
		{
			stackPanel = (StackPanel)(XamlReader.Load(
				"""
				<StackPanel xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				  <AutoSuggestBox x:Name='autoSuggestBox' />
				</StackPanel>
				"""));

			autoSuggestBox = (AutoSuggestBox)stackPanel.FindName("autoSuggestBox");
			TestServices.WindowHelper.WindowContent = stackPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			stackPanel.DataContext = stackPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// header content presenter has x:DeferLoadStrategy = "Lazy", therefore it does not exist unless Header property is set.
			var headerContentPresenter = FindVisualChildByName<ContentPresenter>(autoSuggestBox, "HeaderContentPresenter");
			VERIFY_IS_NULL(headerContentPresenter);
		});
	}

	[TestMethod]
	public async Task CanTapOnItemAfterSelectingItWithKeyboard()
	{
		AutoSuggestBox autoSuggestBox = null;
		Popup popup = null;
		var gotFocusEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		var itemList = new List<string> { "this", "that", "the other" };
		string testString = "this";

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();

			autoSuggestBox.GotFocus += (s, e) =>
			{
				gotFocusEvent.TrySetResult(true);
			};

			autoSuggestBox.TextChanged += (s, e) =>
			{
				textChangedEvent.TrySetResult(true);
			};

			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				VERIFY_ARE_EQUAL(testString, e.QueryText);
				VERIFY_IS_NOT_NULL(e.ChosenSuggestion);
				VERIFY_ARE_EQUAL(testString, (string)e.ChosenSuggestion);
				querySubmittedEvent.TrySetResult(true);
			};

			autoSuggestBox.ItemsSource = itemList;

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");

			popup.Opened += (s, e) =>
			{
				popupOpenedEvent.TrySetResult(true);
			};

			popup.Closed += (s, e) =>
			{
				popupClosedEvent.TrySetResult(true);
			};

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_t#$u$_t");

		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Down();

		await RunOnUIThread(() =>
		{
			var suggestionsList = (ListView)autoSuggestBox.GetTemplateChild("SuggestionsList");
			VERIFY_IS_NOT_NULL(suggestionsList);
			var firstSuggestion = (ListViewItem)suggestionsList.ContainerFromIndex(0);
			VERIFY_IS_NOT_NULL(firstSuggestion);
			TestServices.InputHelper.Tap(firstSuggestion);
		});

		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateCollectionUpdates()
	{
		var itemList = new ObservableCollection<string>();
		AutoSuggestBox asb = null;

		await RunOnUIThread(() =>
		{
			asb = new AutoSuggestBox();
			asb.ItemsSource = itemList;

			for (int i = 0; i < 100; ++i)
			{
				itemList.Add($"item {i}");
			}
			VERIFY_ARE_EQUAL(100u, (uint)asb.Items.Count);

			TestServices.WindowHelper.WindowContent = asb;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Remove first 50 elements
		for (int i = 0; i < 10; ++i)
		{
			await RunOnUIThread(() =>
			{
				for (int j = 0; j < 5; ++j)
				{
					itemList.RemoveAt(0);
				}
			});

			await TestServices.WindowHelper.WaitForIdle();
		}

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(50u, (uint)asb.Items.Count);
		});

		await RunOnUIThread(() =>
		{
			// Remove last 49 elements
			for (int i = 0; i < 49; ++i)
			{
				itemList.RemoveAt(itemList.Count - 1);
			}
			VERIFY_ARE_EQUAL(1u, (uint)asb.Items.Count);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < 100; ++i)
			{
				itemList.Add($"new item {i}");
			}
			VERIFY_ARE_EQUAL(101u, (uint)asb.Items.Count);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Remove every other
			for (int i = 0; i < 50; i++)
			{
				itemList.RemoveAt(i);
			}
			VERIFY_ARE_EQUAL(51u, (uint)asb.Items.Count);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// Remove all
			while (itemList.Count > 0)
			{
				itemList.RemoveAt(0);
			}
			VERIFY_ARE_EQUAL(0u, (uint)asb.Items.Count);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateSuggestionListChangeInTextChangedHandler()
	{
		AutoSuggestBox asb = null;
		var itemList = new ObservableCollection<string>();

		for (int i = 0; i < 200; ++i)
		{
			itemList.Add($"item {i}");
		}

		var textChangedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			asb = new AutoSuggestBox();
			asb.ItemsSource = itemList;

			asb.TextChanged += (s, e) =>
			{
				while (itemList.Count > 0)
				{
					// We shouldn't get an exception here
					itemList.RemoveAt(0);
				}
				textChangedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = asb;
		});

		await TestServices.WindowHelper.WaitForIdle();

		TestServices.InputHelper.Tap(asb);
		await TestServices.WindowHelper.WaitForIdle();

		// Type a key to trigger TextChanged callback
		await TestServices.KeyboardHelper.PressKeySequence("W");
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(0u, (uint)asb.Items.Count);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateUpdateItemsSource()
	{
		// We show the suggestions list and while it is visible we update ItemsSource
		// We validate that the open suggestion list gets updated with the new ItemsSource
		// We also validate that tapping on an item in the suggestion list causes the suggestion list to
		// close and to stay closed.

		uint numItemsBeforeChange = 5;
		uint numItemsAfterChange = 3;

		var itemList = new List<string>();
		for (uint i = 0; i < numItemsBeforeChange; ++i)
		{
			itemList.Add($"item {i}");
		}

		AutoSuggestBox autoSuggestBox = null;
		Popup popup = null;
		ListView suggestionsList = null;
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();
		bool popupClosedFired = false;
		bool popupOpenedFiredAgain = false;

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = itemList;
			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			suggestionsList = (ListView)autoSuggestBox.GetTemplateChild("SuggestionsList");

			popup.Opened += (s, e) =>
			{
				if (popupOpenedEvent.Task.IsCompleted)
				{
					popupOpenedFiredAgain = true;
				}
				popupOpenedEvent.TrySetResult(true);
			};

			popup.Closed += (s, e) =>
			{
				popupClosedFired = true;
				popupClosedEvent.TrySetResult(true);
			};
		});

		// Tap on the ASB to focus it, and type in the TextBox to bring up the suggestion list.
		TestServices.InputHelper.Tap(autoSuggestBox);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.PressKeySequence("a");

		// Wait for the suggestion list to appear.
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		// We expect 5 suggestions
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(numItemsBeforeChange, (uint)suggestionsList.Items.Count);
		});

		// Update ItemSource.
		await RunOnUIThread(() =>
		{
			var items = new List<string>();
			for (uint i = 0; i < numItemsAfterChange; ++i)
			{
				items.Add($"item {i}");
			}
			autoSuggestBox.ItemsSource = items;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// After updating this item source, we expect the suggestionList to get updated.
		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(numItemsAfterChange, (uint)suggestionsList.Items.Count);
		});
		VERIFY_IS_FALSE(popupClosedFired); // The Popup should not have closed at any point.

		TestServices.InputHelper.Tap(suggestionsList);
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		// The suggestion list should not re-appear after selecting an item.
		VERIFY_IS_FALSE(popupOpenedFiredAgain);
	}

	[TestMethod]
	public async Task ScrollWheelScrollsSuggestions()
	{
		await ScrollWheelScrollsSuggestionsWorker(VerticalAlignment.Top);
		await ScrollWheelScrollsSuggestionsWorker(VerticalAlignment.Bottom);
	}

	private async Task ScrollWheelScrollsSuggestionsWorker(VerticalAlignment alignment)
	{
		AutoSuggestBox asb = null;
		ListView suggestions = null;

		var itemList = new List<string>();
		for (int i = 0; i < 200; ++i)
		{
			itemList.Add($"item {i}");
		}

		var textChangedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.VerticalAlignment = alignment;

			asb = new AutoSuggestBox();
			asb.ItemsSource = itemList;

			asb.TextChanged += (s, e) =>
			{
				textChangedEvent.TrySetResult(true);
			};

			rootPanel.Children.Add(asb);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Type a key to trigger TextChanged callback
		TestServices.InputHelper.Tap(asb);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.PressKeySequence("asb scroll wheel");
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			suggestions = (ListView)asb.GetTemplateChild("SuggestionsList");
			VERIFY_IS_NOT_NULL(suggestions);
		});

		TestServices.InputHelper.ScrollMouseWheel(suggestions, alignment == VerticalAlignment.Top ? -100 : +100);
		await TestServices.WindowHelper.WaitForIdle();
		TestServices.InputHelper.Tap(suggestions);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var text = asb.Text;
			int itemNumber = 0;
			if (text.StartsWith("item ") && int.TryParse(text.Substring(5), out itemNumber))
			{
				// The scroll wheel should take us past the 50th item
				VERIFY_IS_GREATER_THAN(itemNumber, 50);
			}
		});

		await TestServices.WindowHelper.WaitForIdle();
	}

	// TODO UNO: ValidateUIElementTree test skipped - requires WinUI-specific ControlHelper.ValidateUIElementTree and PopupHelper
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateUIElementTree()

	// TODO UNO: ValidateSuggestionListNavigationUsingGamepad test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateSuggestionListNavigationUsingGamepad()

	[TestMethod]
	public async Task ValidateSuggestionListNavigationUsingKeyboard()
	{
		await PerformValidateSuggestionListNavigation(VerticalAlignment.Top, goDownFirst: true);
		await PerformValidateSuggestionListNavigation(VerticalAlignment.Top, goDownFirst: false);
		await PerformValidateSuggestionListNavigation(VerticalAlignment.Bottom, goDownFirst: true);
		await PerformValidateSuggestionListNavigation(VerticalAlignment.Bottom, goDownFirst: false);
	}

	private async Task PerformValidateSuggestionListNavigation(VerticalAlignment verticalAlign, bool goDownFirst)
	{
		AutoSuggestBox autoSuggestBox = null;
		Button button1 = null;
		var asbGotFocusEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var suggestionChosenEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();
		var button1GotFocusEvent = new TaskCompletionSource<bool>();

		var suggestionList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.VerticalAlignment = verticalAlign;

			var stackPanel = new StackPanel();

			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = suggestionList;

			autoSuggestBox.GotFocus += (s, e) => asbGotFocusEvent.TrySetResult(true);
			autoSuggestBox.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);
			autoSuggestBox.SuggestionChosen += (s, e) => suggestionChosenEvent.TrySetResult(true);

			button1 = new Button { Content = "Button" };
			button1.GotFocus += (s, e) => button1GotFocusEvent.TrySetResult(true);

			stackPanel.Children.Add(autoSuggestBox);
			stackPanel.Children.Add(button1);

			rootPanel.Children.Add(stackPanel);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
			popup.Closed += (s, e) => popupClosedEvent.TrySetResult(true);

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(asbGotFocusEvent.Task, Task.Delay(5000));

		// Type "R" to show the suggestion list
		string keySequence = "R";
		await TestServices.KeyboardHelper.PressKeySequence(keySequence);
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		textChangedEvent = new TaskCompletionSource<bool>();

		// Navigate in one direction
		if (goDownFirst)
		{
			await TestServices.KeyboardHelper.Down();
		}
		else
		{
			await TestServices.KeyboardHelper.Up();
		}
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));

		textChangedEvent = new TaskCompletionSource<bool>();

		// Navigate in opposite direction
		if (!goDownFirst)
		{
			await TestServices.KeyboardHelper.Down();
		}
		else
		{
			await TestServices.KeyboardHelper.Up();
		}
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));

		await TestServices.KeyboardHelper.Enter();
		await Task.WhenAny(suggestionChosenEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
	}

	// TODO UNO: CanCloseSuggestionListUsingGamepad test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::CanCloseSuggestionListUsingGamepad()

	[TestMethod]
	public async Task CanCloseSuggestionListUsingKeyboard()
	{
		await PerformCanCloseSuggestionList();
	}

	private async Task PerformCanCloseSuggestionList()
	{
		AutoSuggestBox autoSuggestBox = null;
		TextBox textBox = null;
		var gotFocusEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();

		var suggestionList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = suggestionList;

			autoSuggestBox.GotFocus += (s, e) => gotFocusEvent.TrySetResult(true);
			autoSuggestBox.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			textBox = (TextBox)autoSuggestBox.GetTemplateChild("TextBox");
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");

			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
			popup.Closed += (s, e) => popupClosedEvent.TrySetResult(true);

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		// Type "R" to show the suggestion list and then "Escape" to close it.
		string keySequence = "R";
		await TestServices.KeyboardHelper.PressKeySequence(keySequence);
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		await TestServices.KeyboardHelper.Escape();
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await RunOnUIThread(() =>
		{
			// Verify that the textBox has focus, its text from pressing the keySequence
			// has not changed and the caret is at the end of the text.
			VERIFY_ARE_EQUAL(textBox.SelectionStart, keySequence.Length);
			VERIFY_ARE_EQUAL(textBox.SelectionLength, 0);
			VERIFY_ARE_EQUAL(textBox.Text, keySequence);
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(textBox));
		});

		// Reset events
		textChangedEvent = new TaskCompletionSource<bool>();
		popupOpenedEvent = new TaskCompletionSource<bool>();
		popupClosedEvent = new TaskCompletionSource<bool>();

		// Type "e" to show the suggestion list, select a suggestion using "Down"
		// and then "Escape" to close the suggestion list.
		await TestServices.KeyboardHelper.PressKeySequence("e");
		keySequence += "e";
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		textChangedEvent = new TaskCompletionSource<bool>();

		await TestServices.KeyboardHelper.Down();
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));

		await TestServices.KeyboardHelper.Escape();
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await RunOnUIThread(() =>
		{
			// Verify that the textBox has focus, its text from from pressing the keySequence
			// has been set again and the caret is at the end of the text.
			VERIFY_ARE_EQUAL(textBox.SelectionStart, keySequence.Length);
			VERIFY_ARE_EQUAL(textBox.SelectionLength, 0);
			VERIFY_ARE_EQUAL(textBox.Text, keySequence);
			VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(textBox));
		});
	}

	// TODO UNO: CanRaiseQuerySubmittedUsingGamepad test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::CanRaiseQuerySubmittedUsingGamepad()

	[TestMethod]
	public async Task CanRaiseQuerySubmittedUsingKeyboard()
	{
		await PerformCanRaiseQuerySubmitted(goToFirstSuggestion: false);
		await PerformCanRaiseQuerySubmitted(goToFirstSuggestion: true);
	}

	[TestMethod]
	public async Task CanRaiseQuerySubmittedUsingMouse()
	{
		await PerformCanRaiseQuerySubmittedUsingMouse(goToFirstSuggestion: false);
		await PerformCanRaiseQuerySubmittedUsingMouse(goToFirstSuggestion: true);
	}

	private async Task PerformCanRaiseQuerySubmitted(bool goToFirstSuggestion)
	{
		AutoSuggestBox autoSuggestBox = null;
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		var suggestionList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.QueryIcon = new SymbolIcon(Symbol.Find);
			autoSuggestBox.ItemsSource = suggestionList;

			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				if (goToFirstSuggestion)
				{
					VERIFY_ARE_EQUAL(suggestionList[0], e.QueryText);
					VERIFY_IS_NOT_NULL(e.ChosenSuggestion);
				}
				else
				{
					VERIFY_ARE_EQUAL("R", e.QueryText);
					VERIFY_IS_NULL(e.ChosenSuggestion);
				}
				querySubmittedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
		});

		// Type "R" to show the suggestion list
		await TestServices.KeyboardHelper.PressKeySequence("R");
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		if (goToFirstSuggestion)
		{
			await TestServices.KeyboardHelper.Down();
			await TestServices.WindowHelper.WaitForIdle();
		}

		await TestServices.KeyboardHelper.Enter();
		await TestServices.WindowHelper.WaitForIdle();

		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		VERIFY_IS_TRUE(querySubmittedEvent.Task.IsCompleted);
	}

	private async Task PerformCanRaiseQuerySubmittedUsingMouse(bool goToFirstSuggestion)
	{
		AutoSuggestBox autoSuggestBox = null;
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		var suggestionList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.QueryIcon = new SymbolIcon(Symbol.Find);
			autoSuggestBox.ItemsSource = suggestionList;

			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				querySubmittedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Type "R" to show the suggestion list
		await TestServices.KeyboardHelper.PressKeySequence("R");
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();

		if (goToFirstSuggestion)
		{
			await RunOnUIThread(() =>
			{
				var suggestionsList = (ListView)autoSuggestBox.GetTemplateChild("SuggestionsList");
				VERIFY_IS_NOT_NULL(suggestionsList);
				var firstSuggestion = (ListViewItem)suggestionsList.ContainerFromIndex(0);
				VERIFY_IS_NOT_NULL(firstSuggestion);
				TestServices.InputHelper.Tap(firstSuggestion);
			});
		}
		else
		{
			await RunOnUIThread(() =>
			{
				var queryButton = (ButtonBase)autoSuggestBox.GetTemplateChild("QueryButton");
				TestServices.InputHelper.Tap(queryButton);
			});
		}

		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		VERIFY_IS_TRUE(querySubmittedEvent.Task.IsCompleted);
	}

	// TODO UNO: ValidateTraverseSuggestionListUsingGamepad test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateTraverseSuggestionListUsingGamepad()

	[TestMethod]
	public async Task ValidateTraverseSuggestionListUsingKeyboard()
	{
		await PerformValidateTraverseSuggestionList(VerticalAlignment.Top, goDownFirst: true);
		await PerformValidateTraverseSuggestionList(VerticalAlignment.Top, goDownFirst: false);
		await PerformValidateTraverseSuggestionList(VerticalAlignment.Bottom, goDownFirst: true);
		await PerformValidateTraverseSuggestionList(VerticalAlignment.Bottom, goDownFirst: false);
	}

	private async Task PerformValidateTraverseSuggestionList(VerticalAlignment verticalAlign, bool goDownFirst)
	{
		AutoSuggestBox autoSuggestBox = null;
		var gotFocusEvent = new TaskCompletionSource<bool>();
		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();

		var suggestionList = new List<string> { "Ruby", "Sapphire", "Emerald" };

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.VerticalAlignment = verticalAlign;

			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = suggestionList;

			autoSuggestBox.GotFocus += (s, e) => gotFocusEvent.TrySetResult(true);
			autoSuggestBox.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);

			rootPanel.Children.Add(autoSuggestBox);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
			popup.Closed += (s, e) => popupClosedEvent.TrySetResult(true);

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		// Type "R" to show the suggestion list.
		await TestServices.KeyboardHelper.PressKeySequence("R");
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		int textChangedCount = 0;
		textChangedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			autoSuggestBox.TextChanged += (s, e) =>
			{
				textChangedCount++;
				textChangedEvent.TrySetResult(true);
			};
		});

		// Go in one direction, for the length of the list, and wait for textChangedEvent
		for (int i = 0; i < suggestionList.Count; i++)
		{
			if (goDownFirst)
			{
				await TestServices.KeyboardHelper.Down();
			}
			else
			{
				await TestServices.KeyboardHelper.Up();
			}
			await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
			textChangedEvent = new TaskCompletionSource<bool>();
		}

		// Go in the same direction once more (for keyboard, this should loop)
		if (goDownFirst)
		{
			await TestServices.KeyboardHelper.Down();
		}
		else
		{
			await TestServices.KeyboardHelper.Up();
		}
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
		textChangedEvent = new TaskCompletionSource<bool>();

		// Go in the opposite direction, for the length of the list
		for (int i = 0; i < suggestionList.Count; i++)
		{
			if (!goDownFirst)
			{
				await TestServices.KeyboardHelper.Down();
			}
			else
			{
				await TestServices.KeyboardHelper.Up();
			}
			await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
			textChangedEvent = new TaskCompletionSource<bool>();
		}

		// Go in the opposite direction once more (for keyboard, this should loop)
		if (!goDownFirst)
		{
			await TestServices.KeyboardHelper.Down();
		}
		else
		{
			await TestServices.KeyboardHelper.Up();
		}
		await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));

		await TestServices.KeyboardHelper.Enter();
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));

		await TestServices.WindowHelper.WaitForIdle();
	}

	// TODO UNO: CanMoveAwayUsingGamepadWhenSuggestionListIsClosed test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::CanMoveAwayUsingGamepadWhenSuggestionListIsClosed()

	[TestMethod]
	public async Task CanMoveAwayUsingKeyboardWhenSuggestionListIsClosed()
	{
		await PerformMoveAwayFromAutoSuggestBox(suggestionListOpen: false);
	}

	// TODO UNO: CanNotMoveAwayUsingGamepadWhenSuggestionListIsOpen test skipped - gamepad input not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::CanNotMoveAwayUsingGamepadWhenSuggestionListIsOpen()

	private async Task PerformMoveAwayFromAutoSuggestBox(bool suggestionListOpen)
	{
		AutoSuggestBox asb = null;
		Button button1 = null;
		var gotFocusEvent = new TaskCompletionSource<bool>();
		var buttonGotFocusEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();

		var itemList = new List<string> { "Single Suggestion" };

		await RunOnUIThread(() =>
		{
			var rootPanel = new StackPanel();

			asb = new AutoSuggestBox();
			asb.ItemsSource = itemList;
			asb.GotFocus += (s, e) => gotFocusEvent.TrySetResult(true);

			button1 = new Button { Content = "Button" };
			button1.GotFocus += (s, e) => buttonGotFocusEvent.TrySetResult(true);

			rootPanel.Children.Add(asb);
			rootPanel.Children.Add(button1);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popup = (Popup)asb.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);

			asb.Focus(FocusState.Programmatic);
		});

		await Task.WhenAny(gotFocusEvent.Task, Task.Delay(5000));

		if (suggestionListOpen)
		{
			var textChangedEvent = new TaskCompletionSource<bool>();
			await RunOnUIThread(() =>
			{
				asb.TextChanged += (s, e) => textChangedEvent.TrySetResult(true);
			});

			await TestServices.KeyboardHelper.PressKeySequence("S");
			await Task.WhenAny(textChangedEvent.Task, Task.Delay(5000));
			await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));
		}

		// Try to tab away
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		if (!suggestionListOpen)
		{
			// Should be able to move away when suggestion list is closed
			await Task.WhenAny(buttonGotFocusEvent.Task, Task.Delay(5000));
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot).Equals(button1));
			});
		}
	}

	[TestMethod]
	public async Task ValidateDisplayMemberPathPropagatesToSuggestionsPopup()
	{
		AutoSuggestBox autoSuggestBox = null;
		ListView suggestionsList = null;

		var itemList = new List<TestItem>
		{
			new TestItem { Title = "Ruby", Description = "A red gem" },
			new TestItem { Title = "Sapphire", Description = "A blue gem" },
			new TestItem { Title = "Emerald", Description = "A green gem" }
		};

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = itemList;
			autoSuggestBox.DisplayMemberPath = "Title";
			autoSuggestBox.TextMemberPath = "Title";
			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			suggestionsList = (ListView)autoSuggestBox.GetTemplateChild("SuggestionsList");
			VERIFY_IS_NOT_NULL(suggestionsList);
			VERIFY_ARE_EQUAL("Title", suggestionsList.DisplayMemberPath);
		});
	}

	[TestMethod]
	public async Task ValidatePopupOpensAsSoonAsItemsSourceChanges()
	{
		AutoSuggestBox autoSuggestBox = null;
		var textChangedEvent = new TaskCompletionSource<bool>();
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			// No ItemsSource initially

			autoSuggestBox.TextChanged += (s, e) =>
			{
				// Set ItemsSource in TextChanged handler - popup should open immediately
				s.ItemsSource = new List<string> { "Ruby", "Sapphire", "Emerald" };
				VERIFY_IS_TRUE(s.IsSuggestionListOpen);
				textChangedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
			popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
			popup.Closed += (s, e) => popupClosedEvent.TrySetResult(true);

			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Type "t" to show the suggestion list.
		await TestServices.KeyboardHelper.PressKeySequence("$d$_t#$u$_t");
		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));

		// Hit escape to hide the suggestion list.
		await TestServices.KeyboardHelper.Escape();
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));
	}

	[TestMethod]
	public async Task ValidateIsSuggestionListOpenChangesWhenPopupOpens()
	{
		AutoSuggestBox autoSuggestBox = null;
		Popup popup = null;
		var popupOpenedEvent = new TaskCompletionSource<bool>();
		var popupClosedEvent = new TaskCompletionSource<bool>();

		var itemList = new List<string> { "Item 1", "Item 2", "Item 3" };

		int callbackOnOpenCount = 0;
		int callbackOnClosedCount = 0;

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = itemList;

			autoSuggestBox.RegisterPropertyChangedCallback(
				AutoSuggestBox.IsSuggestionListOpenProperty,
				(sender, prop) =>
				{
					if (autoSuggestBox.IsSuggestionListOpen)
					{
						callbackOnOpenCount++;
					}
					else
					{
						callbackOnClosedCount++;
					}
				});

			TestServices.WindowHelper.WindowContent = autoSuggestBox;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");

			popup.Opened += (s, e) =>
			{
				popupOpenedEvent.TrySetResult(true);
			};

			popup.Closed += (s, e) =>
			{
				popupClosedEvent.TrySetResult(true);
			};

			VERIFY_IS_FALSE(autoSuggestBox.IsSuggestionListOpen);
		});

		// Focus and type to open popup
		TestServices.InputHelper.Tap(autoSuggestBox);
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.KeyboardHelper.PressKeySequence("I");

		await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_TRUE(autoSuggestBox.IsSuggestionListOpen);
		});

		// Close the popup
		await TestServices.KeyboardHelper.Escape();
		await Task.WhenAny(popupClosedEvent.Task, Task.Delay(5000));
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_FALSE(autoSuggestBox.IsSuggestionListOpen);
			VERIFY_ARE_EQUAL(1, callbackOnOpenCount);
			VERIFY_ARE_EQUAL(1, callbackOnClosedCount);
		});
	}

	// TODO UNO: ValidateAutoSuggestBoxPosition test skipped - requires SetupAutoSuggestBoxTest with specific margin handling
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateAutoSuggestBoxPosition()

	// TODO UNO: ValidateFootprint test skipped - requires complex layout measurements
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateFootprint()

	// TODO UNO: ValidateSipClosedOnLostFocus test skipped - SIP (Soft Input Panel) handling not available in Uno Platform
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateSipClosedOnLostFocus()

	// TODO UNO: ValidateLightDismissOverlayMode test skipped - requires LightDismissOverlayMode API
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateLightDismissOverlayMode()

	// TODO UNO: IsAutoLightDismissOverlayModeVisibleOnXbox test skipped - Xbox-specific
	// Original C++: void AutoSuggestBoxIntegrationTests::IsAutoLightDismissOverlayModeVisibleOnXbox()

	// TODO UNO: ValidateOverlayBrush test skipped - requires overlay element access
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateOverlayBrush()

	// TODO UNO: ValidateOverlayUIETree test skipped - requires UI element tree comparison
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateOverlayUIETree()

	[TestMethod]
	public async Task CanTabOutWhileSuggestionListIsOpen()
	{
		AutoSuggestBox autoSuggestBox = null;
		Button button = null;

		var itemList = new List<string> { "item 1", "item 2", "item 3" };
		string expectedText = "S";

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.ItemsSource = itemList;

			button = new Button { Content = "Button" };

			var root = new StackPanel();
			root.Children.Add(autoSuggestBox);
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Test scenario: Tab forward
		await RunScenario(true /*moveForward*/);

		// Test scenario: Shift+Tab backward
		await RunScenario(false /*moveForward*/);

		async Task RunScenario(bool moveForward)
		{
			await RunOnUIThread(() =>
			{
				autoSuggestBox.Text = "";
				autoSuggestBox.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			var popupOpenedEvent = new TaskCompletionSource<bool>();
			await RunOnUIThread(() =>
			{
				var popup = (Popup)autoSuggestBox.GetTemplateChild("SuggestionsPopup");
				popup.Opened += (s, e) => popupOpenedEvent.TrySetResult(true);
			});

			// Type "S" to show the suggestion list
			await TestServices.KeyboardHelper.PressKeySequence(expectedText);
			await Task.WhenAny(popupOpenedEvent.Task, Task.Delay(5000));
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(autoSuggestBox.IsSuggestionListOpen, "The suggestion list should be open.");
			});

			// Press Down 2 times to select the second suggestion.
			await TestServices.KeyboardHelper.Down();
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.Down();
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(autoSuggestBox.Text, itemList[1], "The second item is not selected.");
			});

			if (moveForward)
			{
				await TestServices.KeyboardHelper.Tab();
			}
			else
			{
				await TestServices.KeyboardHelper.ShiftTab();
			}
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(button.Equals(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)));
				VERIFY_IS_FALSE(autoSuggestBox.IsSuggestionListOpen);
				VERIFY_ARE_EQUAL(expectedText, autoSuggestBox.Text);
			});
		}

		LOG_OUTPUT("Validate that you can tab out of an open AutoSuggestBox.");
		LOG_OUTPUT("Validate that you can shift-tab out of an open AutoSuggestBox.");
	}

	[TestMethod]
	public async Task DoesNotClearTextWhenTabbedPast()
	{
		AutoSuggestBox autoSuggestBox = null;
		Button beforeButton = null;
		Button afterButton = null;

		string expectedText = "Expected text!";

		await RunOnUIThread(() =>
		{
			autoSuggestBox = new AutoSuggestBox();
			autoSuggestBox.Text = expectedText;

			beforeButton = new Button { Content = "Before" };
			afterButton = new Button { Content = "After" };

			var root = new StackPanel();
			root.Children.Add(beforeButton);
			root.Children.Add(autoSuggestBox);
			root.Children.Add(afterButton);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			beforeButton.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Tab twice to move onto and then off of the AutoSuggestBox.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			VERIFY_ARE_EQUAL(expectedText, autoSuggestBox.Text);
		});
	}

	[TestMethod]
	public async Task ValidateUnloadAndReloadReregistersQuerySubmittedEvent()
	{
		Page page = null;
		StackPanel alphaPanel = null;
		StackPanel betaPanel = null;
		AutoSuggestBox autoSuggestBox = null;
		var querySubmittedEvent = new TaskCompletionSource<bool>();
		var pageLoadedEvent = new TaskCompletionSource<bool>();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Loading Page XAML.");
			page = (Page)(XamlReader.Load(
				"""
				<Page xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
				    <StackPanel>
				        <StackPanel x:Name='alpha'>
				            <TextBlock Text='alpha text' />
				            <AutoSuggestBox x:Name='autoSuggestBox' QueryIcon='Find'></AutoSuggestBox>
				        </StackPanel>
				        <StackPanel x:Name='beta'>
				            <TextBlock Text='beta text' />
				        </StackPanel>
				    </StackPanel>
				</Page>
				"""));

			autoSuggestBox = (AutoSuggestBox)page.FindName("autoSuggestBox");
			VERIFY_IS_NOT_NULL(autoSuggestBox);

			alphaPanel = (StackPanel)page.FindName("alpha");
			VERIFY_IS_NOT_NULL(alphaPanel);

			betaPanel = (StackPanel)page.FindName("beta");
			VERIFY_IS_NOT_NULL(betaPanel);

			LOG_OUTPUT("Registering ASB QuerySubmitted event.");
			autoSuggestBox.QuerySubmitted += (s, e) =>
			{
				LOG_OUTPUT("Enter ASB QuerySubmitted event.");
				querySubmittedEvent.TrySetResult(true);
			};

			LOG_OUTPUT("Registering Page Loaded event.");
			page.Loaded += (s, e) =>
			{
				LOG_OUTPUT("Enter Page Loaded event.");

				LOG_OUTPUT("Remove ASB from alpha.");
				alphaPanel.Children.RemoveAt(1);

				LOG_OUTPUT("Add ASB to beta.");
				betaPanel.Children.Add(autoSuggestBox);

				pageLoadedEvent.TrySetResult(true);
			};

			TestServices.WindowHelper.WindowContent = page;
		});

		LOG_OUTPUT("Waiting for Window Idle.");
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for Page Loaded event.");
		await Task.WhenAny(pageLoadedEvent.Task, Task.Delay(5000));

		LOG_OUTPUT("Page ready.");
		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Enter text into ASB.");
			autoSuggestBox.Text = "asb text";
		});

		LOG_OUTPUT("Waiting for Window Idle.");
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Focus ASB and press Enter to trigger QuerySubmitted.");
			autoSuggestBox.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Enter();

		LOG_OUTPUT("Waiting for ASB QuerySubmitted event.");
		await Task.WhenAny(querySubmittedEvent.Task, Task.Delay(5000));
		VERIFY_IS_TRUE(querySubmittedEvent.Task.IsCompleted);

		LOG_OUTPUT("Waiting for Window Idle.");
		await TestServices.WindowHelper.WaitForIdle();
	}

	// TODO UNO: ValidateSuggestionListFitsInWindow test skipped - requires complex window boundary calculations
	// Original C++: void AutoSuggestBoxIntegrationTests::ValidateSuggestionListFitsInWindow()

	#endregion

	#region Helper Methods

	// Helper method to find visual child by name
	private static T FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
	{
		if (parent == null)
			return null;

		int childCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childCount; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T typedChild && typedChild.Name == name)
			{
				return typedChild;
			}

			var result = FindVisualChildByName<T>(child, name);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	#endregion
}
#endif
