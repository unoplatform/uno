namespace Windows.Media {
	export class SpeechRecognizer {

		private static dispatchMessage:
			(instanceId: string, serializedMessage: string, timestamp: number) => number;

		private static instanceMap: { [managedId: string]: SpeechRecognizer } = {};

		private managedId: string;
		private recognition: SpeechRecognition;

		private constructor(managedId: string) {
			this.managedId = managedId;
			this.recognition = new window.SpeechRecognition();
			this.recognition.addEventListener("result", this.onResult);
			this.recognition.addEventListener("speechstart", this.onSpeechStart);
			this.recognition.addEventListener("speechend", this.onSpeechEnd);
			this.recognition.addEventListener("error", this.onError);
		}

		public static initialize(managedId: string) {
			const recognizer = new SpeechRecognizer(managedId);
			SpeechRecognizer.instanceMap[managedId] = recognizer;
			
		}

		private onResult(event: SpeechRecognitionEvent) {
			
			var color = event.results[0][0].transcript;
		}

		private onSpeechStart() {

		}

		private onSpeechEnd() {

		}

		private onError(event: SpeechSynthesisErrorEvent) {

		}
	}
}
