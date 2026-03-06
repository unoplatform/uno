using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	// ── Data model ──────────────────────────────────────────────────────────────

	public enum SessionItemType
	{
		SubHeader,
		HighPriority,
		Adaptive,
		Badge,
		Maintenance
	}

	public class SessionItem
	{
		public string Label { get; set; } = string.Empty;
		public string SubText { get; set; } = string.Empty;
		public string Badge { get; set; } = string.Empty;
		public SessionItemType ItemType { get; set; }
		public Visibility HasSubText => string.IsNullOrEmpty(SubText) ? Visibility.Collapsed : Visibility.Visible;
	}

	/// <summary>A named group of <see cref="SessionItem"/> objects usable with CollectionViewSource.</summary>
	public class SessionGroup : List<SessionItem>
	{
		public string Key { get; }
		public string SubTitle { get; }

		public SessionGroup(string key, string subTitle, IEnumerable<SessionItem> items)
			: base(items)
		{
			Key = key;
			SubTitle = subTitle;
		}
	}

	// ── Template selector ────────────────────────────────────────────────────────

	public class SessionItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate SubHeaderTemplate { get; set; }
		public DataTemplate HighPriorityTemplate { get; set; }
		public DataTemplate AdaptiveTemplate { get; set; }
		public DataTemplate BadgeTemplate { get; set; }
		public DataTemplate MaintenanceTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			if (item is SessionItem si)
			{
				return si.ItemType switch
				{
					SessionItemType.SubHeader => SubHeaderTemplate,
					SessionItemType.HighPriority => HighPriorityTemplate,
					SessionItemType.Adaptive => AdaptiveTemplate,
					SessionItemType.Badge => BadgeTemplate,
					SessionItemType.Maintenance => MaintenanceTemplate,
					_ => base.SelectTemplateCore(item)
				};
			}
			return base.SelectTemplateCore(item);
		}

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
			=> SelectTemplateCore(item);
	}

	// ── View ────────────────────────────────────────────────────────────────────

	[Sample("ListView", Name = "ListView_Multiple_Templates",
		IsManualTest = true,
		Description = "Grouped ListView with 5 DataTemplates and a DataTemplateSelector. 100+ mock items across 5 groups.")]
	public sealed partial class ListView_Multiple_Templates : UserControl
	{
		public ListView_Multiple_Templates()
		{
			this.InitializeComponent();
			GroupedSource.Source = BuildMockData();
		}

		private static List<SessionGroup> BuildMockData() => new()
		{
			new SessionGroup("HIGH PRIORITY", "Active goals — requires daily data", new[]
			{
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "1.2.9" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Count Skill Acquisition" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Prompt Workflow" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Fade Message" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Will maintain age-appropriate hygiene routines" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Independently comb hair" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "IT Count" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Requests to use the bathroom (COUNT)" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Will explain own emotions Mac" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Task Analysis" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Behavior Incidents", Badge = "3" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Prompt Level Tracking", Badge = "7" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Communication skills" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Mand Training" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Tact Training" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Echoics" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Trial count", Badge = "12" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Social interactions" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Eye contact during greeting" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Respond to name" },
			}),

			new SessionGroup("ADAPTIVE", "---", new[]
			{
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "---" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Duration_Copy1" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "1.2.7.8" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "C" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "10" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Skill Target" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "auto fade test" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "auto fade test" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "count" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Count Behavior", Badge = "5" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Interval Recording" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Whole Interval" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Partial Interval", Badge = "2" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Self-care routines" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Brush teeth independently" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Dress with minimal prompts" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Wash hands correctly" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Fine motor" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Use scissors correctly" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Pencil grasp trials", Badge = "8" },
			}),

			new SessionGroup("MAINTENANCE", "Review previously mastered skills", new[]
			{
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Mastered — monthly probe" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Identify colors (basic)", SubText = "Probe: 3 trials" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Identify shapes", SubText = "Probe: 3 trials" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Match identical objects", SubText = "Mastered: Jan 2025" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Follow 1-step directions" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Follow 2-step directions" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Mastered — quarterly probe" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Identify numbers 1–10", SubText = "Mastered: Oct 2024" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Identify letters A–Z", SubText = "Mastered: Nov 2024" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Count objects 1–5" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Name body parts" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Sort by size" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Sort by color" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Simple puzzles (4-piece)" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Identify common animals" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Say full name" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "State age when asked" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Probe completed", Badge = "16" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Point to requested item" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Complete simple intraverbals" },
			}),

			new SessionGroup("SOCIAL SKILLS", "Peer interaction goals", new[]
			{
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Turn-taking" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Wait for turn in game" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Share toy with peer" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Sharing incidents", Badge = "4" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Greetings" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Wave hello to peer" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Say goodbye when leaving" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Initiate interaction" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Play skills" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Parallel play (5 min)" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Interactive play with peer" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Unprompted initiations", Badge = "2" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Conversation" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Maintain topic (3 exchanges)" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Ask peer a question" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Respond to peer question" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Use appropriate volume" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Group activities" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Participate in circle time" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "On-task intervals", Badge = "9" },
			}),

			new SessionGroup("ACADEMIC", "Pre-academic and academic readiness", new[]
			{
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Reading readiness" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Phonemic awareness — rhyming" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Letter-sound correspondence" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Correct responses", Badge = "14" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Sight word identification" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Math readiness" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Number identification 1–20" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "One-to-one correspondence" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Simple addition (objects)" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Math trials", Badge = "6" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Writing" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Trace first name", SubText = "3 trials" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Copy horizontal line" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Copy vertical line" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Copy circle" },
				new SessionItem { ItemType = SessionItemType.SubHeader, Label = "Attention & task completion" },
				new SessionItem { ItemType = SessionItemType.Adaptive,   Label = "Remain seated (10 min)" },
				new SessionItem { ItemType = SessionItemType.HighPriority, Label = "Complete worksheet independently" },
				new SessionItem { ItemType = SessionItemType.Badge,      Label = "Task completion rate", Badge = "75%" },
				new SessionItem { ItemType = SessionItemType.Maintenance, Label = "Follow classroom routine", SubText = "Daily probe" },
			}),
		};
	}
}
