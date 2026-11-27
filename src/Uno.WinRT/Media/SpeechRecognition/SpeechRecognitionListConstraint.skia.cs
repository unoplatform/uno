#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.Media.SpeechRecognition;

public partial class SpeechRecognitionListConstraint : ISpeechRecognitionConstraint
{
	public SpeechRecognitionListConstraint(IEnumerable<string> commands)
		: this(commands, string.Empty)
	{
	}

	public SpeechRecognitionListConstraint(IEnumerable<string> commands, string tag)
	{
		Commands = new List<string>(commands ?? throw new ArgumentNullException(nameof(commands)));
		Tag = tag;
	}

	public IList<string> Commands { get; }

	public bool IsEnabled { get; set; } = true;

	public SpeechRecognitionConstraintProbability Probability { get; set; } = SpeechRecognitionConstraintProbability.Default;

	public string Tag { get; set; }

	public SpeechRecognitionConstraintType Type => SpeechRecognitionConstraintType.List;
}
