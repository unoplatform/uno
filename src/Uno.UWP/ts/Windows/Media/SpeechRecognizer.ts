interface Window {
	SpeechRecognition: any;
	webkitSpeechRecognition: any;
}

namespace Windows.Media {
	export class SpeechRecognizer {

		private static instanceMap: { [managedId: string]: SpeechRecognizer } = {};

		private managedId: string;
		private culture: string;
		private recognition: any;

		// Guards the single-shot completion so a trailing "end" event cannot double-dispatch
		// after a final result or an error has already been reported.
		private completed: boolean = false;

		private constructor(managedId: string, culture: string) {
			this.managedId = managedId;
			this.culture = culture;

			// Prefer the standard, unprefixed SpeechRecognition interface; fall back to the
			// webkit-prefixed implementation only on engines that do not expose the standard one.
			const ctor: any = window.SpeechRecognition || window.webkitSpeechRecognition;
			if (ctor) {
				this.recognition = new ctor();

				// The constructor takes no arguments — the recognition language is set here.
				if (culture) {
					this.recognition.lang = culture;
				}

				this.recognition.addEventListener("result", this.onResult);
				this.recognition.addEventListener("speechstart", this.onSpeechStart);
				this.recognition.addEventListener("error", this.onError);
				this.recognition.addEventListener("end", this.onEnd);
			}
		}

		public static initialize(managedId: string, culture: string) {
			const recognizer = new SpeechRecognizer(managedId, culture);
			SpeechRecognizer.instanceMap[managedId] = recognizer;
		}

		public static recognize(managedId: string): boolean {
			const recognizer = SpeechRecognizer.instanceMap[managedId];
			if (!recognizer || !recognizer.recognition) {
				return false;
			}

			// Configure and start asynchronously so on-device availability can be probed before
			// listening. Results and errors keep flowing through the dispatch callbacks, so the
			// synchronous return value only signals that a recognizer instance exists.
			recognizer.startAsync().catch(() => { /* errors are surfaced via dispatch below */ });
			return true;
		}

		public static removeInstance(managedId: string) {
			const recognizer = SpeechRecognizer.instanceMap[managedId];
			if (recognizer && recognizer.recognition) {
				recognizer.recognition.removeEventListener("result", recognizer.onResult);
				recognizer.recognition.removeEventListener("speechstart", recognizer.onSpeechStart);
				recognizer.recognition.removeEventListener("error", recognizer.onError);
				recognizer.recognition.removeEventListener("end", recognizer.onEnd);

				// Stop any in-flight recognition so the native session is released deterministically.
				try { recognizer.recognition.abort(); } catch { /* already stopped */ }
			}
			delete SpeechRecognizer.instanceMap[managedId];
		}

		private async startAsync(): Promise<void> {
			const recognition = this.recognition;
			recognition.continuous = false;
			recognition.interimResults = true;
			this.completed = false;

			await this.tryEnableOnDeviceRecognition();

			try {
				recognition.start();
			} catch (e) {
				this.completed = true;
				SpeechRecognizer.getExports().DispatchError(this.managedId, `start-failed: ${(e as any)?.message ?? e}`);
			}
		}

		// Opt into on-device recognition when the engine supports it and the language pack is
		// installed. On-device processing keeps audio off the cloud transcription service, which is
		// what surfaces as a "network" error on some origins/builds. When on-device is unsupported or
		// the pack is not yet installed, this leaves the default (cloud) engine in place.
		private async tryEnableOnDeviceRecognition(): Promise<void> {
			try {
				const recognition: any = this.recognition;
				const ctor: any = window.SpeechRecognition || window.webkitSpeechRecognition;
				if (!ctor || !("processLocally" in recognition) || typeof ctor.available !== "function") {
					return;
				}

				const langs = [this.culture || recognition.lang].filter((l: string) => !!l);
				const status = await ctor.available({ langs: langs, processLocally: true });
				if (status === "available") {
					recognition.processLocally = true;
				} else if (status === "downloadable" || status === "downloading") {
					// Kick off a background install so a later session can run on-device; this session
					// continues with the default engine.
					const install = ctor.install || ctor.installOnDevice;
					if (typeof install === "function") {
						try { install.call(ctor, { langs: langs }); } catch { /* best effort */ }
					}
				}
			} catch {
				// Probing failed (older engine / experimental API absent); use the default engine.
			}
		}

		private onResult = (event: any) => {
			const result = event.results[0];
			const alternative = result[0];
			if (result.isFinal) {
				this.completed = true;
				SpeechRecognizer.getExports().DispatchResult(this.managedId, alternative.transcript, alternative.confidence);
			} else {
				SpeechRecognizer.getExports().DispatchHypothesis(this.managedId, alternative.transcript);
			}
		}

		private onSpeechStart = () => {
			SpeechRecognizer.getExports().DispatchStatus(this.managedId, "SpeechDetected");
		}

		private onError = (event: any) => {
			this.completed = true;
			SpeechRecognizer.getExports().DispatchError(this.managedId, event.error);
		}

		private onEnd = () => {
			// If recognition ended without a final result or an error (e.g. silence / no match),
			// complete the pending managed operation with an empty result so it does not hang.
			if (!this.completed) {
				this.completed = true;
				SpeechRecognizer.getExports().DispatchResult(this.managedId, "", 0);
			}
		}

		// Resolves the source-generated managed JSExports for the recognizer.
		private static getExports(): any {
			const exports = (<any>globalThis).DotnetExports;
			if (exports === undefined) {
				throw `SpeechRecognizer: Unable to find dotnet exports`;
			}
			return exports.Uno.Windows.Media.SpeechRecognition.SpeechRecognizer;
		}
	}
}
