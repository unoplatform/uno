#nullable enable

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognitionTopicConstraint : ISpeechRecognitionConstraint
{
	public SpeechRecognitionTopicConstraint(SpeechRecognitionScenario scenario, string topicHint)
		: this(scenario, topicHint, string.Empty)
	{
	}

	public SpeechRecognitionTopicConstraint(SpeechRecognitionScenario scenario, string topicHint, string tag)
	{
		Scenario = scenario;
		TopicHint = topicHint;
		Tag = tag;
	}

	public SpeechRecognitionScenario Scenario { get; }

	public string TopicHint { get; }

	public bool IsEnabled { get; set; } = true;

	public SpeechRecognitionConstraintProbability Probability { get; set; } = SpeechRecognitionConstraintProbability.Default;

	public string Tag { get; set; }

	public SpeechRecognitionConstraintType Type => SpeechRecognitionConstraintType.Topic;
}
