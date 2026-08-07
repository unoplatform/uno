interface UnoAppNotificationActionCommand {
	id: string;
	title: string;
	icon: string;
	argument: string;
	protocolUri: string | null;
}

interface UnoAppNotificationCommand {
	id: number;
	nativeTag: string;
	title: string;
	body: string;
	language: string;
	direction: NotificationDirection;
	icon: string;
	image: string;
	timestamp: number | null;
	expirationTimestamp: number | null;
	silent: boolean;
	requireInteraction: boolean;
	launchArgument: string;
	protocolUri: string | null;
	actions: UnoAppNotificationActionCommand[];
}

namespace Windows.UI.Notifications {
	export class AppNotificationManager {
		private static readonly tagPrefix = "uno.appnotifications.";
		private static readonly activeNotifications = new Map<string, Notification>();
		private static readonly activeIds = new Set<number>();
		private static readonly expirationTimers = new Map<string, number>();
		private static stateKnown = false;
		private static initialized = false;
		private static dispatchActivation: ((argument: string) => number) | null = null;

		public static isSupported(): boolean {
			return globalThis.isSecureContext === true && "Notification" in globalThis;
		}

		public static getPermission(): string {
			return this.isSupported() ? Notification.permission : "denied";
		}

		public static initialize(): void {
			if (this.initialized) {
				return;
			}

			const exports = (<any>globalThis).DotnetExports?.Uno?.Microsoft?.Windows?.AppNotifications?.Internal?.WebAssemblyAppNotificationActivation;
			if (exports?.Dispatch === undefined) {
				throw new Error("AppNotificationManager: Unable to find dotnet exports.");
			}
			this.dispatchActivation = exports.Dispatch;
			this.initialized = true;
		}

		public static uninitialize(): void {
			this.dispatchActivation = null;
			this.initialized = false;
		}

		public static requestPermission(): void {
			if (this.isSupported() && Notification.permission === "default") {
				void Notification.requestPermission().catch(error => console.warn("Unable to request app notification permission.", error));
			}
		}

		public static show(commandJson: string): boolean {
			if (!this.isSupported() || Notification.permission !== "granted") {
				return false;
			}

			let command: UnoAppNotificationCommand;
			try {
				command = JSON.parse(commandJson) as UnoAppNotificationCommand;
			}
			catch {
				return false;
			}
			if (!this.isValidCommand(command)) {
				return false;
			}

			const options = this.createOptions(command);
			try {
				const notification = new Notification(command.title, options);
				this.clearExpiration(command.nativeTag);
				this.activeNotifications.get(command.nativeTag)?.close();
				this.activeNotifications.set(command.nativeTag, notification);
				this.activeIds.add(command.id);
				this.stateKnown = true;
				notification.onclick = event => {
					event.preventDefault();
					this.activate(command.launchArgument, command.protocolUri);
					notification.close();
				};
				notification.onclose = () => this.removeActive(command, notification);
				notification.onerror = () => {
					console.warn("The browser could not display an app notification.");
					this.removeActive(command, notification);
				};
				this.scheduleExpiration(command, notification);
				return true;
			}
			catch (error) {
				console.warn("Unable to show the app notification.", error);
				return false;
			}
		}

		public static close(tag: string): void {
			if (!tag.startsWith(this.tagPrefix)) {
				return;
			}
			this.clearExpiration(tag);
			this.activeNotifications.get(tag)?.close();
			this.activeNotifications.delete(tag);
			this.removeActiveId(tag);
		}

		public static closeAll(tagPrefix: string): void {
			if (tagPrefix !== this.tagPrefix) {
				return;
			}
			for (const [tag, notification] of this.activeNotifications) {
				if (tag.startsWith(tagPrefix)) {
					this.clearExpiration(tag);
					notification.close();
				}
			}
			this.activeNotifications.clear();
			this.activeIds.clear();
			this.stateKnown = true;
		}

		public static getActiveIds(tagPrefix: string): string | null {
			if (tagPrefix !== this.tagPrefix || !this.stateKnown) {
				return null;
			}
			return Array.from(this.activeIds).join(",");
		}

		private static createOptions(command: UnoAppNotificationCommand): NotificationOptions {
			const options: NotificationOptions = {
				body: command.body,
				tag: command.nativeTag,
				lang: command.language,
				dir: command.direction,
				silent: command.silent,
				requireInteraction: command.requireInteraction,
				timestamp: command.timestamp ?? undefined,
				icon: command.icon || undefined,
				image: command.image || undefined,
				data: {
					unoAppNotification: true,
					id: command.id,
					tag: command.nativeTag,
					launchArgument: command.launchArgument,
					protocolUri: command.protocolUri,
				},
			};
			return options;
		}

		private static activate(argument: string, protocolUri: string | null): void {
			if (protocolUri !== null) {
				this.navigateProtocol(protocolUri);
			}
			else if (argument.length <= 5120) {
				this.dispatchActivation?.(argument);
			}
		}

		private static navigateProtocol(value: string): void {
			if (value.length === 0 || value.length > 2048) {
				return;
			}
			try {
				const uri = new URL(value, globalThis.location.href);
				if (uri.protocol === "javascript:" || uri.protocol === "data:") {
					return;
				}
				globalThis.location.assign(uri.href);
			}
			catch {
			}
		}

		private static isValidCommand(command: UnoAppNotificationCommand): boolean {
			return Number.isInteger(command.id) && command.id > 0 &&
				command.nativeTag === this.tagPrefix + command.id &&
				typeof command.title === "string" && command.title.length <= 5120 &&
				typeof command.body === "string" && command.body.length <= 5120 &&
				Array.isArray(command.actions) && command.actions.length <= 5;
		}

		private static scheduleExpiration(command: UnoAppNotificationCommand, notification: Notification): void {
			if (command.expirationTimestamp === null) {
				return;
			}
			const delay = command.expirationTimestamp - Date.now();
			if (delay <= 0) {
				notification.close();
			}
			else if (delay <= 2147483647) {
				this.clearExpiration(command.nativeTag);
				const timer = globalThis.setTimeout(() => {
					this.expirationTimers.delete(command.nativeTag);
					if (this.activeNotifications.get(command.nativeTag) === notification) {
						notification.close();
					}
				}, delay);
				this.expirationTimers.set(command.nativeTag, timer);
			}
		}

		private static removeActive(command: UnoAppNotificationCommand, notification: Notification): void {
			if (this.activeNotifications.get(command.nativeTag) !== notification) {
				return;
			}
			this.clearExpiration(command.nativeTag);
			this.activeNotifications.delete(command.nativeTag);
			this.activeIds.delete(command.id);
		}

		private static removeActiveId(tag: string): void {
			const value = tag.startsWith(this.tagPrefix) ? Number(tag.substring(this.tagPrefix.length)) : Number.NaN;
			if (Number.isInteger(value)) {
				this.activeIds.delete(value);
			}
		}

		private static clearExpiration(tag: string): void {
			const timer = this.expirationTimers.get(tag);
			if (timer !== undefined) {
				globalThis.clearTimeout(timer);
				this.expirationTimers.delete(tag);
			}
		}
	}
}