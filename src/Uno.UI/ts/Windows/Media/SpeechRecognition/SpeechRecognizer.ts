namespace Windows.Media.SpeechRecognition {
	export class SpeechRecognizer {
		private static dispatchMessage:
			(instanceId: string, serializedMessage: string, timestamp: number) => number;

		private static instanceMap: { [managedId: string]: SpeechRecognizer } = {};

		private managedId: string;

		private constructor(managedId: string) {
			this.managedId = managedId;
		}

		public static initialize(managedId: string) {
			SpeechRecognizer.instanceMap[managedId] = new SpeechRecognizer(managedId);			
		}
	}
}
