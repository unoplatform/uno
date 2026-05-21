using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

[Sample(
	"ItemsRepeater",
	IsManualTest = true,
	Description = "ScrollViewer + ItemsRepeater chat conversation mimicking an agentic-assistant UI (user bubbles, " +
		"assistant replies, reasoning expanders, tool-call rows, outcome cards, thinking/benchmark lines, plus nested " +
		"image-attachment repeaters) on a cooking topic. Message height varies widely (cooking copy + lorem ipsum + " +
		"image grids) and auto-scroll-on-add is enabled. Generation is seeded, so reloading the sample and" +
		"clicking \"Add\" produce identical results every time.")]
public sealed partial class ItemsRepeaterChatConversation : Page
{
	// A fixed seed makes the conversation fully reproducible: a freshly created page (reload)
	// always re-seeds to the same value, and every "Add"/"Reset" walks the same RNG sequence.
	private const int Seed = 20260520;
	private const int DefaultMessageCount = 120;
	private const int AddMessageCount = 20;

	private readonly ObservableCollection<ChatMessage> _items = new();

	// Generation state. The RNG and pending buffer persist for the lifetime of the page so that
	// consecutive "Add" clicks deterministically continue the same conversation. Whole turns are
	// generated into _pending and dequeued one message at a time to satisfy exact item counts.
	private readonly Queue<ChatMessage> _pending = new();
	private Random _rng = new(Seed);
	private DateTime _clock;
	private int _nextIndex;

	public ItemsRepeaterChatConversation()
	{
		InitializeComponent();
		ResetState();
		AddMessages(DefaultMessageCount);
		Repeater.ItemsSource = _items;
		UpdateStatus();
	}

	private void ResetState()
	{
		_rng = new Random(Seed);
		_clock = new DateTime(2026, 5, 20, 9, 0, 0);
		_nextIndex = 0;
		_pending.Clear();
	}

	private void AddMessages(int count)
	{
		for (var i = 0; i < count; i++)
		{
			if (_pending.Count == 0)
			{
				GenerateTurn();
			}

			_items.Add(_pending.Dequeue());
		}
	}

	// --- Conversation flow ---------------------------------------------------

	private void GenerateTurn()
	{
		// A turn loosely follows the agentic pattern: the user asks, the assistant optionally
		// thinks/reasons, runs a few tools, replies, and occasionally posts a benchmark or outcome.
		_pending.Enqueue(MakeUser());

		if (_rng.NextDouble() < 0.15)
		{
			_pending.Enqueue(MakeThinking());
		}
		else if (_rng.NextDouble() < 0.55)
		{
			_pending.Enqueue(MakeReasoning());
		}

		var toolCount = _rng.Next(1, 5);
		for (var i = 0; i < toolCount; i++)
		{
			_pending.Enqueue(MakeToolCall());
		}

		_pending.Enqueue(MakeAssistant());

		if (_rng.NextDouble() < 0.08)
		{
			_pending.Enqueue(MakeBenchmark());
		}

		if (_rng.NextDouble() < 0.30)
		{
			_pending.Enqueue(MakeOutcome());
		}
	}

	private ChatMessage MakeUser()
	{
		var parts = _rng.Next(1, 4);
		var sb = new StringBuilder();
		for (var i = 0; i < parts; i++)
		{
			if (i > 0)
			{
				sb.Append(' ');
			}

			sb.Append(Pick(UserPrompts));
		}

		// Occasionally pad a user message with lorem so some bubbles wrap to several lines.
		if (_rng.NextDouble() < 0.2)
		{
			sb.Append(' ').Append(BuildText(1, 3, loremBias: 0.8));
		}

		return new ChatMessage
		{
			Index = _nextIndex++,
			Kind = ChatMessageKind.User,
			Text = sb.ToString(),
			Images = MaybeImages(probability: 0.22, min: 1, max: 3),
			Timestamp = NextTimestamp(),
		};
	}

	private ChatMessage MakeAssistant() => new()
	{
		Index = _nextIndex++,
		Kind = ChatMessageKind.Assistant,
		// Wide range of sentence counts (and optional image grids) gives the large height
		// variance the sample is built to surface in the repeater's virtualization.
		Text = BuildText(1, 10, loremBias: 0.4),
		Images = MaybeImages(probability: 0.16, min: 2, max: 6),
		Timestamp = NextTimestamp(),
	};

	private ChatMessage MakeReasoning() => new()
	{
		Index = _nextIndex++,
		Kind = ChatMessageKind.Reasoning,
		ReasoningHeader = Pick(ReasoningHeaders),
		Text = BuildText(2, 7, loremBias: 0.5),
		IsReasoningExpanded = _rng.NextDouble() < 0.4,
		Timestamp = NextTimestamp(),
	};

	private ChatMessage MakeThinking() => new()
	{
		Index = _nextIndex++,
		Kind = ChatMessageKind.Thinking,
		Text = Pick(ThinkingTexts),
		Timestamp = NextTimestamp(),
	};

	private ChatMessage MakeBenchmark() => new()
	{
		Index = _nextIndex++,
		Kind = ChatMessageKind.Benchmark,
		Text = $"Generated in {_rng.Next(8, 120) / 10.0:0.0} s · {_rng.Next(120, 4000):N0} tokens · {_rng.Next(1, 24)} tool calls",
		Timestamp = NextTimestamp(),
	};

	private ChatMessage MakeToolCall()
	{
		string status;
		string fileName = null;
		string addedText = null;
		string removedText = null;
		string entryCountText = null;

		switch (_rng.Next(4))
		{
			case 0:
				status = "Searching the recipe index";
				entryCountText = $"{_rng.Next(8, 120)} matches";
				break;
			case 1:
				status = "Reading";
				fileName = Pick(RecipeFiles);
				entryCountText = $"{_rng.Next(20, 400)} lines";
				break;
			case 2:
				status = "Updating";
				fileName = Pick(RecipeFiles);
				addedText = $"+{_rng.Next(1, 40)}";
				if (_rng.NextDouble() < 0.7)
				{
					removedText = $"-{_rng.Next(1, 20)}";
				}

				break;
			default:
				status = Pick(ToolMiscActions);
				break;
		}

		var roll = _rng.NextDouble();
		var state = roll < 0.82 ? ToolCallState.Success
			: roll < 0.94 ? ToolCallState.Failure
			: ToolCallState.InProgress;

		return new ChatMessage
		{
			Index = _nextIndex++,
			Kind = ChatMessageKind.ToolCall,
			ToolState = state,
			ToolStatusText = status,
			ToolFileName = fileName,
			ToolDiffAddedText = addedText,
			ToolDiffRemovedText = removedText,
			ToolEntryCountText = entryCountText,
			Timestamp = NextTimestamp(),
		};
	}

	private ChatMessage MakeOutcome()
	{
		var roll = _rng.NextDouble();
		var severity = roll < 0.5 ? OutcomeSeverity.Success
			: roll < 0.8 ? OutcomeSeverity.Warning
			: OutcomeSeverity.Critical;

		var (title, subtitle) = severity switch
		{
			OutcomeSeverity.Success => (Pick(SuccessTitles), Pick(SuccessSubtitles)),
			OutcomeSeverity.Warning => (Pick(WarningTitles), Pick(WarningSubtitles)),
			_ => (Pick(CriticalTitles), Pick(CriticalSubtitles)),
		};

		return new ChatMessage
		{
			Index = _nextIndex++,
			Kind = ChatMessageKind.Outcome,
			Severity = severity,
			OutcomeTitle = title,
			OutcomeSubtitle = subtitle,
			Timestamp = NextTimestamp(),
		};
	}

	// --- Text / image helpers ------------------------------------------------

	private string BuildText(int minSentences, int maxSentences, double loremBias)
	{
		var count = _rng.Next(minSentences, maxSentences + 1);
		var sb = new StringBuilder();
		for (var i = 0; i < count; i++)
		{
			var pool = _rng.NextDouble() < loremBias ? LoremSentences : CookingSentences;
			sb.Append(Pick(pool));

			if (i < count - 1)
			{
				// Occasionally break into a new paragraph for taller, more varied bodies.
				sb.Append(_rng.NextDouble() < 0.18 ? "\n\n" : " ");
			}
		}

		return sb.ToString();
	}

	private ChatImageAttachment[] MaybeImages(double probability, int min, int max)
	{
		if (_rng.NextDouble() >= probability)
		{
			return null;
		}

		var count = _rng.Next(min, max + 1);
		var images = new ChatImageAttachment[count];
		for (var i = 0; i < count; i++)
		{
			images[i] = new ChatImageAttachment { Fill = Pick(ImagePalette) };
		}

		return images;
	}

	private string NextTimestamp()
	{
		_clock = _clock.AddMinutes(_rng.Next(1, 4));
		return _clock.ToString("h:mm tt");
	}

	private T Pick<T>(T[] pool) => pool[_rng.Next(pool.Length)];

	// --- Button handlers -----------------------------------------------------

	private void OnTopClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, 0, null, disableAnimation: true);

	private void OnBottomClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);

	private void OnAddItemsClick(object sender, RoutedEventArgs e)
	{
		AddMessages(AddMessageCount);
		UpdateStatus();
	}

	private void OnRemoveExpandersClick(object sender, RoutedEventArgs e)
	{
		// Reasoning messages are the only Expander-based items in this conversation.
		for (var i = _items.Count - 1; i >= 0; i--)
		{
			if (_items[i].Kind == ChatMessageKind.Reasoning)
			{
				_items.RemoveAt(i);
			}
		}

		UpdateStatus();
	}

	private void OnClearClick(object sender, RoutedEventArgs e)
	{
		_items.Clear();
		UpdateStatus();
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		_items.Clear();
		ResetState();
		AddMessages(DefaultMessageCount);
		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);
		UpdateStatus();
	}

	private void UpdateStatus() =>
		StatusText.Text = $"{_items.Count} messages";

	// --- Mock data pools -----------------------------------------------------

	private static readonly Brush[] ImagePalette =
	[
		new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
		new SolidColorBrush(Color.FromArgb(255, 66, 133, 244)),
		new SolidColorBrush(Color.FromArgb(255, 255, 152, 0)),
		new SolidColorBrush(Color.FromArgb(255, 229, 57, 53)),
		new SolidColorBrush(Color.FromArgb(255, 156, 39, 176)),
		new SolidColorBrush(Color.FromArgb(255, 0, 150, 136)),
	];

	private static readonly string[] ThinkingTexts =
	[
		"Thinking…",
		"Working out the best approach…",
		"Planning the next few steps…",
		"Reviewing the recipe constraints…",
	];

	private static readonly string[] UserPrompts =
	[
		"How do I stop my risotto from turning gluey?",
		"Can you scale this recipe up for eight guests?",
		"What can I use instead of buttermilk?",
		"Why did my bread come out so dense this time?",
		"Suggest a side dish that goes with roast lamb.",
		"How long should I rest a steak after searing?",
		"My sauce keeps splitting, what am I doing wrong?",
		"Can we make this dessert without refined sugar?",
		"What's the trick to a really crisp chicken skin?",
		"Give me a quick weeknight pasta idea.",
		"How do I know when the caramel is ready?",
		"Is there a vegetarian version of this stew?",
	];

	private static readonly string[] CookingSentences =
	[
		"Start by browning the aromatics gently so they sweeten rather than scorch.",
		"Add the stock a ladle at a time, stirring until each addition is absorbed.",
		"A splash of acid right at the end will brighten the whole dish.",
		"Rest the meat under loose foil so the juices redistribute before carving.",
		"Toast the spices in a dry pan until they smell fragrant, then grind them.",
		"Keep the heat moderate; rushing the sear will steam the surface instead.",
		"Season in layers as you go rather than all at once at the very end.",
		"Whisk constantly while you drizzle in the oil so the emulsion holds together.",
		"Let the dough prove somewhere warm until it has roughly doubled in size.",
		"Reduce the sauce until it coats the back of a spoon, then taste again.",
		"Salt the pasta water generously; it should taste like the sea.",
		"Deglaze the pan with a little wine to lift all the caramelised bits.",
		"Chill the butter so it stays in distinct flakes through the pastry.",
		"Finish with a knob of cold butter to give the sauce a glossy sheen.",
	];

	// Classic lorem ipsum lines, used to introduce realistic length variability.
	private static readonly string[] LoremSentences =
	[
		"Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
		"Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
		"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
		"Duis aute irure dolor in reprehenderit in voluptate velit esse cillum.",
		"Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia.",
		"Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit.",
		"Neque porro quisquam est qui dolorem ipsum quia dolor sit amet consectetur.",
		"Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse.",
		"At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis.",
		"Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus.",
	];

	private static readonly string[] ReasoningHeaders =
	[
		"Planning the menu",
		"Thinking about substitutions",
		"Working out the timings",
		"Balancing the flavours",
		"Choosing a cooking method",
		"Checking the technique",
	];

	private static readonly string[] ToolMiscActions =
	[
		"Scaling the ingredient quantities",
		"Converting grams to cups",
		"Calculating the total cook time",
		"Validating the shopping list",
		"Checking the allergen list",
		"Estimating oven preheat time",
	];

	private static readonly string[] RecipeFiles =
	[
		"risotto.md",
		"pantry.json",
		"menu.yaml",
		"timings.txt",
		"shopping-list.md",
		"allergens.md",
		"oven-log.txt",
	];

	private static readonly string[] SuccessTitles =
	[
		"Dish ready to serve", "Timer finished", "All steps complete",
	];

	private static readonly string[] SuccessSubtitles =
	[
		"Everything is plated and still warm",
		"The roast has reached its target temperature",
		"The bread has finished its second proof",
	];

	private static readonly string[] WarningTitles =
	[
		"Oven needs attention", "Ingredient running low", "Taste and adjust",
	];

	private static readonly string[] WarningSubtitles =
	[
		"The temperature has drifted above the target",
		"Only a handful of fresh basil leaves left",
		"Season the soup before the final simmer",
	];

	private static readonly string[] CriticalTitles =
	[
		"The sauce has split", "Burnt the base", "Step failed",
	];

	private static readonly string[] CriticalSubtitles =
	[
		"Take it off the heat and whisk in a little cold water",
		"Decant the unburnt portion into a clean pan immediately",
		"The previous tool call could not be completed",
	];
}

public enum ChatMessageKind
{
	User,
	Assistant,
	Reasoning,
	ToolCall,
	Outcome,
	Thinking,
	Benchmark,
}

public enum ToolCallState
{
	InProgress,
	Success,
	Failure,
}

public enum OutcomeSeverity
{
	Success,
	Warning,
	Critical,
}

public sealed class ChatImageAttachment
{
	public Brush Fill { get; init; }
}

public sealed class ChatMessage : INotifyPropertyChanged
{
	private bool _isReasoningExpanded;

	public int Index { get; init; }

	public ChatMessageKind Kind { get; init; }

	public string Text { get; init; } = string.Empty;

	public string Timestamp { get; init; } = string.Empty;

	public ChatImageAttachment[] Images { get; init; }

	// Reasoning
	public string ReasoningHeader { get; init; } = string.Empty;

	public bool IsReasoningExpanded
	{
		get => _isReasoningExpanded;
		set
		{
			if (_isReasoningExpanded != value)
			{
				_isReasoningExpanded = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReasoningExpanded)));
			}
		}
	}

	// Tool call
	public ToolCallState ToolState { get; init; }

	public string ToolStatusText { get; init; } = string.Empty;

	public string ToolFileName { get; init; }

	public string ToolDiffAddedText { get; init; }

	public string ToolDiffRemovedText { get; init; }

	public string ToolEntryCountText { get; init; }

	// Outcome
	public OutcomeSeverity Severity { get; init; }

	public string OutcomeTitle { get; init; } = string.Empty;

	public string OutcomeSubtitle { get; init; } = string.Empty;

	// Computed visibilities, kept on the model so the templates stay binding-only.
	public Visibility ImagesVisibility => Vis(Images is { Length: > 0 });

	public Visibility ToolProgressVisibility => Vis(ToolState == ToolCallState.InProgress);

	public Visibility ToolSuccessVisibility => Vis(ToolState == ToolCallState.Success);

	public Visibility ToolFailureVisibility => Vis(ToolState == ToolCallState.Failure);

	public Visibility ToolFileNameVisibility => Vis(!string.IsNullOrEmpty(ToolFileName));

	public Visibility ToolDiffVisibility => Vis(ToolDiffAddedText is not null);

	public Visibility ToolDiffRemovedVisibility => Vis(ToolDiffRemovedText is not null);

	public Visibility ToolEntryCountVisibility => Vis(ToolEntryCountText is not null);

	public Visibility OutcomeSuccessVisibility => Vis(Severity == OutcomeSeverity.Success);

	public Visibility OutcomeWarningVisibility => Vis(Severity == OutcomeSeverity.Warning);

	public Visibility OutcomeCriticalVisibility => Vis(Severity == OutcomeSeverity.Critical);

	public event PropertyChangedEventHandler PropertyChanged;

	private static Visibility Vis(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
}

public sealed class ChatMessageTemplateSelector : DataTemplateSelector
{
	public DataTemplate UserTemplate { get; set; }

	public DataTemplate AssistantTemplate { get; set; }

	public DataTemplate ReasoningTemplate { get; set; }

	public DataTemplate ToolCallTemplate { get; set; }

	public DataTemplate OutcomeTemplate { get; set; }

	public DataTemplate ThinkingTemplate { get; set; }

	public DataTemplate BenchmarkTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is ChatMessage message)
		{
			return message.Kind switch
			{
				ChatMessageKind.User => UserTemplate,
				ChatMessageKind.Reasoning => ReasoningTemplate,
				ChatMessageKind.ToolCall => ToolCallTemplate,
				ChatMessageKind.Outcome => OutcomeTemplate,
				ChatMessageKind.Thinking => ThinkingTemplate,
				ChatMessageKind.Benchmark => BenchmarkTemplate,
				_ => AssistantTemplate,
			};
		}

		return AssistantTemplate;
	}
}
