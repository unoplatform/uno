#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SampleControl.Presentation;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace SamplesApp.Samples.Help
{
	[Sample(
		"_None",
		Name = "Help",
		Description = "Help and keyboard shortcuts for the Samples app",
		UsesFrame = false,
		IgnoreInSnapshotTests = true)]
	public sealed partial class HelpPage : Page
	{
		private const double NarrowWidth = 600;

		// Keys that are not catalogue accelerators but do the same thing.
		private static readonly Dictionary<string, string> HelpOnlyAlternatives = new()
		{
			[ShellCommands.FocusSearch] = ShellCommands.SearchCharacter.ToString(),
		};

		// What to use instead when a browser takes every shortcut of a command.
		private static readonly Dictionary<string, string> BrowserRoutes = new()
		{
			[ShellCommands.ReloadSample] = "the header buttons",
			[ShellCommands.PreviousSample] = "the header buttons",
			[ShellCommands.NextSample] = "the header buttons",
			[ShellCommands.OpenRuntimeTests] = "the rail or menu",
			[ShellCommands.OpenPlayground] = "the rail or menu",
			[ShellCommands.OpenHelp] = "the rail or menu",
			[ShellCommands.ShowSettings] = "the rail or menu",
			[ShellCommands.ShowHome] = "the rail or menu",
			[ShellCommands.ToggleFocusMode] = "the more menu",
		};

		public HelpPage()
		{
			Shortcuts = BuildShortcutRows(ShellCommands.All);
			CommandLine = BuildCommandLineRows();

			this.InitializeComponent();

			SizeChanged += (_, e) => ApplyPadding(e.NewSize.Width);
		}

		internal IReadOnlyList<HelpShortcutRow> Shortcuts { get; }

		internal IReadOnlyList<HelpCommandLineRow> CommandLine { get; }

		internal Visibility NewWindowVisibility => SampleChooserViewModel.CanCreateNewWindow ? Visibility.Visible : Visibility.Collapsed;

		private void ApplyPadding(double width)
			=> HelpScroller.Padding = width < NarrowWidth ? new Thickness(16, 12, 16, 12) : new Thickness(24, 16, 24, 16);

		/// <summary>One row per catalogue entry; aliases and Help-only keys show up as "or" alternatives on their entry's row.</summary>
		internal static IReadOnlyList<HelpShortcutRow> BuildShortcutRows(IEnumerable<ShellCommand> commands)
		{
			var all = commands.ToList();
			List<HelpShortcutRow> rows = new();
			foreach (var command in all.Where(c => !c.IsAlias))
			{
				List<(string Shortcut, bool WasmUnsafe)> alternatives = new() { (command.Shortcut, command.WasmUnsafe) };
				if (HelpOnlyAlternatives.TryGetValue(command.Id, out var helpOnly))
				{
					alternatives.Add((helpOnly, false));
				}

				alternatives.AddRange(all.Where(c => c.AliasOf == command.Id).Select(c => (c.Shortcut, c.WasmUnsafe)));

				rows.Add(new HelpShortcutRow(command.Id, command.Label, alternatives.Select(a => a.Shortcut).ToList(), GetBrowserNote(command.Id, alternatives)));
			}

			return rows;
		}

		private static string GetBrowserNote(string id, IReadOnlyList<(string Shortcut, bool WasmUnsafe)> alternatives)
		{
			if (!alternatives.Any(a => a.WasmUnsafe))
			{
				return "";
			}

			var safe = alternatives.Where(a => !a.WasmUnsafe).Select(a => a.Shortcut).ToList();
			if (safe.Count > 0)
			{
				return $"In a browser, use {string.Join(" or ", safe)}";
			}

			return BrowserRoutes.TryGetValue(id, out var route) ? $"In a browser, use {route}" : "Not available in a browser";
		}

		private static IReadOnlyList<HelpCommandLineRow> BuildCommandLineRows() => new HelpCommandLineRow[]
		{
			new(
				new[] { "sample=Category/Name" },
				"Open a sample on launch. A bare sample name or the full type name works too. On desktop, in a browser and on Android:",
				new[] { "-- sample=Buttons/Button_Events", "?sample=Buttons/Button_Events", "--es UnoArguments sample=Buttons/Button_Events" }),
			new(
				new[] { "theme=Light|Dark|System" },
				"Start with that theme, without saving it. Join it to sample= with &, in one argument:",
				new[] { "\"sample=Buttons/Button_Events&theme=Dark\"" }),
			new(
				new[] { "--runtime-tests=<file>" },
				"Run the runtime tests, write the NUnit results to the file and exit."),
			new(
				new[] { "--runtime-test-filter=<base64>" },
				"Only run the tests named in a pipe-separated list of type or method names, UTF-8 and base64 encoded."),
			new(
				new[] { "--runtime-tests-group=<n>", "--runtime-tests-group-count=<total>" },
				"Run one slice of the tests."),
			new(
				new[] { "UITEST_RUNTIME_AUTOSTART_RESULT_FILE", "UITEST_RUNTIME_TESTS_FILTER", "UITEST_RUNTIME_TEST_GROUP", "UITEST_RUNTIME_TEST_GROUP_COUNT" },
				"Environment variables that do the same as the runtime test arguments."),
			new(
				new[] { "--auto-screenshots=<dir>", "--total-groups=<n>", "--current-group-index=<i>" },
				"Screenshot every sample into the folder and exit. The group arguments split the work."),
			new(
				new[] { "--FeatureConfiguration.<Class>.<Prop>=<value>" },
				"Set a FeatureConfiguration flag on desktop. Pass each flag as its own, space-separated argument, not joined with &:",
				new[] { "--FeatureConfiguration.ToolTip.UseToolTips=false" }),
			new(
				new[] { "UNO_SHOW_FPS=1", "UNO_LOG_FPS=1" },
				"Show the frame counter on the surface, or log it."),
			new(
				new[] { "UNO_PERF_CYCLE=<seconds>", "UNO_PERF_SCROLL", "UNO_PERF_OPEN_MENU", "UNO_PERF_MAXIMIZE" },
				"Visit every sample for that long, for FPS sweeps. The others tune the benchmarks."),
		};
	}

	/// <summary>Label beside the keycaps, or the keycaps under the label when the row is too narrow for both.</summary>
	public sealed partial class HelpShortcutRowPanel : Panel
	{
		private const double ColumnSpacing = 16;
		private const double RowSpacing = 4;
		// Narrower labels wrap notes like "use Alt+R" inside the shortcut (WinUI breaks at the +).
		private const double MinLabelWidth = 160;

		internal bool IsStacked { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			if (Children.Count < 2)
			{
				return default;
			}

			var label = Children[0];
			var keys = Children[1];
			keys.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			var labelWidth = availableSize.Width - keys.DesiredSize.Width - ColumnSpacing;
			IsStacked = labelWidth < Math.Min(label.DesiredSize.Width, MinLabelWidth);

			if (IsStacked)
			{
				label.Measure(new Size(availableSize.Width, double.PositiveInfinity));
				return new Size(
					Math.Max(label.DesiredSize.Width, keys.DesiredSize.Width),
					label.DesiredSize.Height + RowSpacing + keys.DesiredSize.Height);
			}

			label.Measure(new Size(Math.Max(0, labelWidth), double.PositiveInfinity));
			return new Size(
				label.DesiredSize.Width + ColumnSpacing + keys.DesiredSize.Width,
				Math.Max(label.DesiredSize.Height, keys.DesiredSize.Height));
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Children.Count < 2)
			{
				return finalSize;
			}

			var label = Children[0].DesiredSize;
			var keys = Children[1].DesiredSize;
			if (IsStacked)
			{
				var top = Math.Max(0, (finalSize.Height - label.Height - RowSpacing - keys.Height) / 2);
				Children[0].Arrange(new Rect(0, top, finalSize.Width, label.Height));
				Children[1].Arrange(new Rect(0, top + label.Height + RowSpacing, keys.Width, keys.Height));
			}
			else
			{
				var labelWidth = Math.Max(0, finalSize.Width - keys.Width - ColumnSpacing);
				Children[0].Arrange(new Rect(0, (finalSize.Height - label.Height) / 2, labelWidth, label.Height));
				Children[1].Arrange(new Rect(finalSize.Width - keys.Width, (finalSize.Height - keys.Height) / 2, keys.Width, keys.Height));
			}

			return finalSize;
		}
	}

	internal sealed class HelpKey
	{
		public HelpKey(string text, bool isSeparator)
		{
			Text = text;
			CapVisibility = isSeparator ? Visibility.Collapsed : Visibility.Visible;
			SeparatorVisibility = isSeparator ? Visibility.Visible : Visibility.Collapsed;
		}

		public string Text { get; }

		public Visibility CapVisibility { get; }

		public Visibility SeparatorVisibility { get; }
	}

	internal sealed class HelpShortcutRow
	{
		public HelpShortcutRow(string id, string label, IReadOnlyList<string> alternatives, string note)
		{
			Id = id;
			Label = label;
			Shortcut = string.Join(" or ", alternatives);
			Note = note;
			NoteVisibility = note.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

			List<HelpKey> keys = new();
			for (var i = 0; i < alternatives.Count; i++)
			{
				if (i > 0)
				{
					keys.Add(new HelpKey("or", isSeparator: true));
				}

				keys.AddRange(alternatives[i].Split('+', StringSplitOptions.RemoveEmptyEntries).Select(k => new HelpKey(k, isSeparator: false)));
			}

			Keys = keys;
		}

		public string Id { get; }

		public string Label { get; }

		public string Shortcut { get; }

		public IReadOnlyList<HelpKey> Keys { get; }

		public string Note { get; }

		public Visibility NoteVisibility { get; }

		/// <summary>The keycaps are hidden from screen readers, so the label carries the keys and the note.</summary>
		public string AutomationName => Note.Length > 0 ? $"{Label}, {Shortcut}. {Note}" : $"{Label}, {Shortcut}";
	}

	internal sealed class HelpCommandLineRow
	{
		public HelpCommandLineRow(IReadOnlyList<string> syntax, string description, IReadOnlyList<string>? examples = null)
		{
			Syntax = string.Join("\n", syntax);
			Description = description;
			Examples = examples is null ? "" : string.Join("\n", examples);
			ExamplesVisibility = Examples.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>One argument per line; the page never wraps it, so it reads and copies as typed.</summary>
		public string Syntax { get; }

		public string Description { get; }

		public string Examples { get; }

		public Visibility ExamplesVisibility { get; }
	}
}
