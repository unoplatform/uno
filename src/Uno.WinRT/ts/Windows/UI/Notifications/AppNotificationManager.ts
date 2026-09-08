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

interface UnoAppNotificationActivation {
	token: string;
	argument: string;
	protocolUri: string | null;
}

interface UnoAppNotificationActivationRecord {
	id: number;
	revision?: string;
	pending?: boolean;
	body: UnoAppNotificationActivation;
	actions: Record<string, UnoAppNotificationActivation>;
}

interface UnoAppNotificationActivationWriteResult {
	succeeded: boolean;
	previous: UnoAppNotificationActivationRecord | null;
}

interface UnoAppNotificationLockRecord {
	owner: string;
	choosing: boolean;
	ticket: number;
	expires: number;
}

namespace Windows.UI.Notifications {
	export class AppNotificationStatePersistence {
		public static isSupported(): boolean {
			try {
				const key = "uno.appnotifications.probe";
				globalThis.localStorage.setItem(key, key);
				globalThis.localStorage.removeItem(key);
				return true;
			}
			catch {
				return false;
			}
		}

		public static getItem(key: string): string | null {
			return globalThis.localStorage.getItem(this.getStorageKey(key));
		}

		public static setItem(key: string, value: string): void {
			globalThis.localStorage.setItem(this.getStorageKey(key), value);
		}

		public static getItems(prefix: string): string {
			const storagePrefix = this.getStorageKey(prefix);
			const entries: { key: string; value: string }[] = [];
			for (let index = 0; index < globalThis.localStorage.length; index++) {
				const storageKey = globalThis.localStorage.key(index);
				if (storageKey?.startsWith(storagePrefix)) {
					const value = globalThis.localStorage.getItem(storageKey);
					if (value !== null) {
						entries.push({ key: storageKey.substring(storagePrefix.length), value });
					}
				}
			}
			return JSON.stringify(entries);
		}

		public static removeItem(key: string): void {
			globalThis.localStorage.removeItem(this.getStorageKey(key));
		}

		public static removeItems(prefix: string): void {
			const storagePrefix = this.getStorageKey(prefix);
			const keys: string[] = [];
			for (let index = 0; index < globalThis.localStorage.length; index++) {
				const storageKey = globalThis.localStorage.key(index);
				if (storageKey?.startsWith(storagePrefix)) {
					keys.push(storageKey);
				}
			}
			keys.forEach(key => globalThis.localStorage.removeItem(key));
		}

		public static createNotificationId(): number {
			const values = new Uint32Array(1);
			globalThis.crypto.getRandomValues(values);
			return (values[0] & 0x7FFFFFFF) || 1;
		}

		public static acquireTransactionLock(
			lockName: string,
			owner: string,
			timeoutMilliseconds: number,
			leaseMilliseconds: number): boolean {
			if (lockName.length === 0 || !/^[0-9a-f]{32}$/i.test(owner) ||
				!Number.isFinite(timeoutMilliseconds) || timeoutMilliseconds < 0 ||
				!Number.isFinite(leaseMilliseconds) || leaseMilliseconds <= 0) {
				throw new Error("Invalid app notification transaction lock parameters.");
			}

			const key = this.getLockKey(lockName, owner);
			const deadline = Date.now() + timeoutMilliseconds;
			this.writeLockRecord(key, {
				owner,
				choosing: true,
				ticket: 0,
				expires: Date.now() + leaseMilliseconds,
			});
			const ticket = this.getLockRecords(lockName)
				.reduce((maximum, candidate) => Math.max(maximum, candidate.ticket), 0) + 1;
			if (!Number.isSafeInteger(ticket)) {
				this.releaseTransactionLock(lockName, owner);
				throw new Error("The app notification transaction lock ticket limit was exceeded.");
			}
			this.writeLockRecord(key, {
				owner,
				choosing: false,
				ticket,
				expires: Date.now() + leaseMilliseconds,
			});

			while (Date.now() <= deadline) {
				const own = this.readLockRecord(key);
				if (own === null || own.owner !== owner || own.expires <= Date.now()) {
					this.releaseTransactionLock(lockName, owner);
					return false;
				}
				if (!this.hasEarlierLockContender(lockName, own)) {
					this.spinWait(2);
					const verified = this.readLockRecord(key);
					if (verified !== null && verified.owner === owner && verified.expires > Date.now() &&
						!this.hasEarlierLockContender(lockName, verified)) {
						return true;
					}
				}
				this.spinWait(2);
			}

			this.releaseTransactionLock(lockName, owner);
			return false;
		}

		public static renewTransactionLock(
			lockName: string,
			owner: string,
			leaseMilliseconds: number): void {
			const key = this.getLockKey(lockName, owner);
			const record = this.readLockRecord(key);
			if (record === null || record.owner !== owner || record.choosing ||
				record.expires <= Date.now() || !Number.isFinite(leaseMilliseconds) || leaseMilliseconds <= 0) {
				throw new Error("The app notification transaction lock is no longer owned.");
			}
			this.writeLockRecord(key, {
				owner,
				choosing: false,
				ticket: record.ticket,
				expires: Date.now() + leaseMilliseconds,
			});
		}

		public static releaseTransactionLock(lockName: string, owner: string): void {
			const key = this.getLockKey(lockName, owner);
			const record = this.readLockRecord(key);
			if (record?.owner === owner) {
				globalThis.localStorage.removeItem(key);
			}
		}

		public static commitTransactionVersion(
			lockName: string,
			owner: string,
			versionKey: string,
			expectedVersion: string,
			nextVersion: string): boolean {
			const lock = this.readLockRecord(this.getLockKey(lockName, owner));
			if (lock === null || lock.owner !== owner || lock.choosing || lock.expires <= Date.now()) {
				throw new Error("The app notification transaction lock is no longer owned.");
			}
			const key = this.getStorageKey(versionKey);
			if ((globalThis.localStorage.getItem(key) ?? "") !== expectedVersion) {
				return false;
			}
			globalThis.localStorage.setItem(key, nextVersion);
			return true;
		}

		private static hasEarlierLockContender(lockName: string, own: UnoAppNotificationLockRecord): boolean {
			return this.getLockRecords(lockName).some(candidate =>
				candidate.owner !== own.owner &&
				(candidate.choosing ||
					candidate.ticket < own.ticket ||
					(candidate.ticket === own.ticket && candidate.owner < own.owner)));
		}

		private static getLockRecords(lockName: string): UnoAppNotificationLockRecord[] {
			const prefix = this.getStorageKey(`${lockName}.`);
			const now = Date.now();
			const records: UnoAppNotificationLockRecord[] = [];
			const expiredKeys: string[] = [];
			for (let index = 0; index < globalThis.localStorage.length; index++) {
				const key = globalThis.localStorage.key(index);
				if (!key?.startsWith(prefix)) {
					continue;
				}
				const record = this.readLockRecord(key);
				if (record === null || key.substring(prefix.length) !== record.owner) {
					throw new Error("Invalid app notification transaction lock state.");
				}
				if (record.expires <= now) {
					expiredKeys.push(key);
				}
				else {
					records.push(record);
				}
			}
			expiredKeys.forEach(key => globalThis.localStorage.removeItem(key));
			return records;
		}

		private static readLockRecord(key: string): UnoAppNotificationLockRecord | null {
			const value = globalThis.localStorage.getItem(key);
			if (value === null) {
				return null;
			}
			try {
				const record = <UnoAppNotificationLockRecord>JSON.parse(value);
				return record !== null && typeof record === "object" &&
					/^[0-9a-f]{32}$/i.test(record.owner) &&
					typeof record.choosing === "boolean" &&
					Number.isSafeInteger(record.ticket) && record.ticket >= 0 &&
					Number.isFinite(record.expires)
					? record
					: null;
			}
			catch {
				return null;
			}
		}

		private static writeLockRecord(key: string, record: UnoAppNotificationLockRecord): void {
			globalThis.localStorage.setItem(key, JSON.stringify(record));
		}

		private static getLockKey(lockName: string, owner: string): string {
			return this.getStorageKey(`${lockName}.${owner}`);
		}

		private static spinWait(milliseconds: number): void {
			const deadline = Date.now() + milliseconds;
			while (Date.now() < deadline) {
			}
		}

		private static getStorageKey(key: string): string {
			return `uno:${new URL("./", document.baseURI).pathname}:${key}`;
		}
	}

	export class AppNotificationManager {
		private static readonly tagPrefix = "uno.appnotifications.";
		private static readonly serviceWorkerScriptName = "Uno.AppNotifications.ServiceWorker.js";
		private static readonly serviceWorkerAppIdParameter = "uno-app-id";
		private static readonly appId = new URL("./", document.baseURI).href;
		private static readonly activationStoragePrefix = `uno:${new URL("./", document.baseURI).pathname}:uno.appnotifications.activation.`;
		private static readonly activationStateLockName = "uno.appnotification-state-lock";
		private static readonly activationStateLockTimeoutMilliseconds = 5000;
		private static readonly activationStateLockLeaseMilliseconds = 30000;
		private static readonly blockedProtocolSchemes = new Set<string>(["javascript:", "vbscript:", "data:", "blob:", "file:"]);
		private static readonly activeNotifications = new Map<string, Notification>();
		private static readonly activeIds = new Set<number>();
		private static readonly expirationTimers = new Map<string, number>();
		private static serviceWorkerRegistration: ServiceWorkerRegistration | null = null;
		private static persistentOperation: Promise<void> = Promise.resolve();
		private static registrationGeneration = 0;
		private static stateKnown = false;
		private static initialized = false;
		private static activationRegistered = false;
		private static useServiceWorker = false;
		private static dispatchActivation: ((argument: string) => number) | null = null;
		private static dispatchShowResult: ((operationCorrelation: string, id: number, succeeded: boolean) => number) | null = null;
		private static readonly serviceWorkerMessageHandler = (event: MessageEvent): void => {
			void AppNotificationManager.handleServiceWorkerMessage(event);
		};

		private static async handleServiceWorkerMessage(event: MessageEvent): Promise<void> {
			const message = event.data;
			if (!await this.isOwnedWorkerMessage(event, message)) {
				event.ports[0]?.postMessage({ accepted: false });
				return;
			}
			if (message.type === "uno-app-notification-activated") {
				AppNotificationManager.activeIds.delete(message.id);
				const accepted = AppNotificationManager.consumeAndActivate(message.id, message.token);
				event.ports[0]?.postMessage({ accepted });
			}
			else if (message.type === "uno-app-notification-closed") {
				void AppNotificationManager.refreshPersistentNotifications();
			}
		}

		public static isSupported(useServiceWorker: boolean = false): boolean {
			if (globalThis.isSecureContext !== true || !("Notification" in globalThis)) {
				return false;
			}
			if (!useServiceWorker) {
				return true;
			}
			const registrationPrototype = (<any>globalThis).ServiceWorkerRegistration?.prototype;
			return "serviceWorker" in navigator && registrationPrototype !== undefined &&
				typeof registrationPrototype.showNotification === "function" &&
				typeof registrationPrototype.getNotifications === "function" &&
				typeof globalThis.crypto?.getRandomValues === "function" &&
				AppNotificationStatePersistence.isSupported();
		}

		public static getPermission(): string {
			return this.isSupported() ? Notification.permission : "denied";
		}

		public static initialize(useServiceWorker: boolean = false): void {
			this.initializePosting(useServiceWorker);
			if (this.activationRegistered) {
				return;
			}

			const exports = (<any>globalThis).DotnetExports?.Uno?.Microsoft?.Windows?.AppNotifications?.Internal?.WebAssemblyAppNotificationActivation;
			if (exports?.Dispatch === undefined) {
				throw new Error("AppNotificationManager: Unable to find dotnet exports.");
			}
			this.dispatchActivation = exports.Dispatch;
			this.activationRegistered = true;
			if (useServiceWorker) {
				navigator.serviceWorker.addEventListener("message", this.serviceWorkerMessageHandler);
				this.dispatchPendingActivation();
			}
		}

		public static initializePosting(useServiceWorker: boolean = false): void {
			if (this.initialized) {
				if (this.useServiceWorker !== useServiceWorker) {
					throw new Error("AppNotificationManager: The notification posting mode cannot be changed.");
				}
				return;
			}

			const exports = (<any>globalThis).DotnetExports?.Uno?.Microsoft?.Windows?.AppNotifications?.Internal?.WebAssemblyAppNotificationActivation;
			if (useServiceWorker && exports?.DispatchShowResult === undefined) {
				throw new Error("AppNotificationManager: Unable to find the show-result export.");
			}
			this.dispatchShowResult = exports?.DispatchShowResult ?? null;
			this.useServiceWorker = useServiceWorker;
			this.initialized = true;
			if (!useServiceWorker) {
				if ("serviceWorker" in navigator) {
					void this.cleanupPreviousPersistentMode();
				}
				return;
			}

			const configuredWorkerUrl = (<any>globalThis).UnoAppNotificationsServiceWorkerUrl;
			if (typeof configuredWorkerUrl !== "string" || configuredWorkerUrl.length === 0) {
				this.initialized = false;
				throw new Error("AppNotificationManager: Unable to locate the notification service worker.");
			}
			const generation = ++this.registrationGeneration;
			const workerUrl = new URL(configuredWorkerUrl, document.baseURI);
			workerUrl.searchParams.set(this.serviceWorkerAppIdParameter, this.appId);
			void this.enqueuePersistentOperation(async () => {
				const registration = await navigator.serviceWorker.register(workerUrl.href, { updateViaCache: "none" });
				const activeRegistration = await this.waitForActiveWorker(registration);
				if (generation === this.registrationGeneration && this.initialized && this.useServiceWorker) {
					this.serviceWorkerRegistration = activeRegistration;
				}
			}).then(
				() => this.refreshPersistentNotifications(),
				error => {
					if (generation === this.registrationGeneration) {
						this.initialized = false;
						this.serviceWorkerRegistration = null;
					}
					console.warn("Unable to register the app notification service worker.", error);
				});
		}

		public static uninitialize(): void {
			if (this.useServiceWorker && this.activationRegistered) {
				navigator.serviceWorker.removeEventListener("message", this.serviceWorkerMessageHandler);
			}
			this.dispatchActivation = null;
			this.activationRegistered = false;
		}

		public static requestPermission(): void {
			if (this.isSupported() && Notification.permission === "default") {
				void Notification.requestPermission().catch(error => console.warn("Unable to request app notification permission.", error));
			}
		}

		public static show(commandJson: string, operationCorrelation: string): boolean {
			if (!this.isSupported() || Notification.permission !== "granted") {
				return false;
			}

			const command = this.parseCommand(commandJson);
			if (command === null) {
				return false;
			}
			if (this.useServiceWorker) {
				return this.showPersistent(command, operationCorrelation);
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

		public static async showAsync(commandJson: string, operationCorrelation: string): Promise<boolean> {
			if (!this.isSupported(this.useServiceWorker) || Notification.permission !== "granted") {
				return false;
			}
			const command = this.parseCommand(commandJson);
			if (command === null) {
				return false;
			}
			if (!this.useServiceWorker) {
				return this.show(commandJson, operationCorrelation);
			}
			if (!this.initialized || !this.isValidOperationCorrelation(operationCorrelation)) {
				return false;
			}
			return await this.showPersistentAsync(command, false, operationCorrelation);
		}

		public static close(tag: string): void {
			if (!tag.startsWith(this.tagPrefix)) {
				return;
			}
			if (this.useServiceWorker) {
				void this.closePersistent(tag);
				this.removeActiveId(tag);
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
			if (this.useServiceWorker) {
				void this.closeAllPersistent();
				this.activeIds.clear();
				this.stateKnown = true;
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

		public static async closeAsync(tag: string): Promise<boolean> {
			if (!tag.startsWith(this.tagPrefix)) {
				return false;
			}
			if (!this.useServiceWorker) {
				this.close(tag);
				return true;
			}
			return await this.closePersistent(tag);
		}

		public static async closeAllAsync(tagPrefix: string): Promise<boolean> {
			if (tagPrefix !== this.tagPrefix) {
				return false;
			}
			if (!this.useServiceWorker) {
				this.closeAll(tagPrefix);
				return true;
			}
			return await this.closeAllPersistent();
		}

		public static async unregisterAllAsync(tagPrefix: string): Promise<boolean> {
			if (tagPrefix !== this.tagPrefix || !this.useServiceWorker) {
				return false;
			}
			const generation = ++this.registrationGeneration;
			this.initialized = false;
			try {
				return await this.enqueuePersistentOperation(async () => {
					await this.closeAllPersistentCore();
					const registrations = await this.getPersistentRegistrations();
					for (const candidate of registrations) {
						if (this.isNotificationWorker(candidate)) {
							await candidate.unregister();
						}
					}
					if (generation === this.registrationGeneration) {
						this.serviceWorkerRegistration = null;
					}
					return true;
				});
			}
			catch (error) {
				console.warn("Unable to unregister persistent app notifications.", error);
				return false;
			}
		}

		public static unregisterAll(tagPrefix: string): void {
			void this.unregisterAllAsync(tagPrefix);
		}

		public static getActiveIds(tagPrefix: string): string | null {
			if (tagPrefix !== this.tagPrefix || this.useServiceWorker || !this.stateKnown) {
				return null;
			}
			return Array.from(this.activeIds).join(",");
		}

		public static async getActiveIdsAsync(tagPrefix: string): Promise<string | null> {
			if (tagPrefix !== this.tagPrefix) {
				return null;
			}
			if (this.useServiceWorker && !await this.refreshPersistentNotifications()) {
				return null;
			}
			return this.stateKnown ? Array.from(this.activeIds).join(",") : null;
		}

		private static showPersistent(command: UnoAppNotificationCommand, operationCorrelation: string): boolean {
			if (!this.initialized || !this.isValidOperationCorrelation(operationCorrelation)) {
				return false;
			}
			void this.showPersistentAsync(command, true, operationCorrelation);
			return true;
		}

		private static async showPersistentAsync(
			command: UnoAppNotificationCommand,
			dispatchCompletion: boolean,
			operationCorrelation: string): Promise<boolean> {
			const succeeded = await this.enqueuePersistentOperation(async () => {
				let activation: UnoAppNotificationActivationRecord | null = null;
				let activationWrite: UnoAppNotificationActivationWriteResult | null = null;
				try {
					const registration = this.serviceWorkerRegistration;
					activation = this.createActivationRecord(command);
					if (registration === null) {
						throw new Error("The notification service worker is unavailable.");
					}
					activationWrite = this.replaceActivationRecord(activation);
					if (!activationWrite.succeeded) {
						throw new Error("Unable to persist the app notification activation token.");
					}
					await registration.showNotification(command.title, this.createPersistentOptions(command, activation));
					this.commitActivationRecord(activation);
					try {
						const registrations = await this.getPersistentRegistrations();
						for (const candidate of registrations) {
							if (candidate.scope === registration.scope) {
								continue;
							}
							const notifications = await candidate.getNotifications({ tag: command.nativeTag });
							notifications
								.filter(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))
								.forEach(notification => notification.close());
						}
						await this.unregisterEmptyLegacyWorkers(registrations);
					}
					catch (error) {
						console.warn("Unable to clean up legacy app notifications.", error);
					}
					await this.refreshPersistentNotificationsCore();
					return true;
				}
				catch (error) {
					console.warn("Unable to show the persistent app notification.", error);
					if (activation !== null && activationWrite?.succeeded === true) {
						this.rollbackActivationRecord(activation, activationWrite.previous);
					}
					return false;
				}
			});
			if (dispatchCompletion) {
				this.dispatchShowResult?.(operationCorrelation, command.id, succeeded);
			}
			return succeeded;
		}

		private static async closePersistent(tag: string): Promise<boolean> {
			this.clearExpiration(tag);
			try {
				await this.enqueuePersistentOperation(async () => {
					const id = this.getId(tag);
					const expectedActivation = this.getActivationRecords()[id] ?? null;
					const registrations = await this.getPersistentRegistrations();
					for (const registration of registrations) {
						const notifications = await registration.getNotifications({ tag });
						notifications
							.filter(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))
							.forEach(notification => notification.close());
					}
					await this.unregisterEmptyLegacyWorkers(registrations);
					this.activeNotifications.delete(tag);
					this.removeActiveId(tag);
					if (expectedActivation !== null) {
						this.removeActivationRecord(id, expectedActivation);
					}
					this.stateKnown = true;
				});
				return true;
			}
			catch (error) {
				console.warn("Unable to close the persistent app notification.", error);
				return false;
			}
		}

		private static async closeAllPersistent(): Promise<boolean> {
			try {
				await this.enqueuePersistentOperation(() => this.closeAllPersistentCore());
				return true;
			}
			catch (error) {
				console.warn("Unable to close persistent app notifications.", error);
				return false;
			}
		}

		private static async closeAllPersistentCore(): Promise<void> {
			const expectedActivations = this.getActivationRecords();
			for (const tag of this.expirationTimers.keys()) {
				this.clearExpiration(tag);
			}
			const registrations = await this.getPersistentRegistrations();
			for (const registration of registrations) {
				const notifications = await registration.getNotifications();
				notifications
					.filter(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))
					.forEach(notification => notification.close());
			}
			await this.unregisterEmptyLegacyWorkers(registrations);
			this.activeNotifications.clear();
			this.activeIds.clear();
			this.clearActivationRecords(expectedActivations);
			this.stateKnown = true;
		}

		private static async refreshPersistentNotifications(): Promise<boolean> {
			return await this.enqueuePersistentOperation(() => this.refreshPersistentNotificationsCore());
		}

		private static async refreshPersistentNotificationsCore(): Promise<boolean> {
			try {
				const activationSnapshot = this.getActivationRecords();
				const registrations = await this.getPersistentRegistrations();
				const notifications: Notification[] = [];
				for (const registration of registrations) {
					notifications.push(...await registration.getNotifications());
				}
				for (const tag of this.expirationTimers.keys()) {
					this.clearExpiration(tag);
				}
				this.activeNotifications.clear();
				this.activeIds.clear();
				for (const notification of notifications) {
					if (this.isLegacyOwnedNotification(notification)) {
						notification.close();
						continue;
					}
					if (!this.isOwnedNotification(notification)) {
						continue;
					}
					const expirationTimestamp = notification.data?.unoAppNotification?.expirationTimestamp;
					if (typeof expirationTimestamp === "number" && expirationTimestamp > 0) {
						if (expirationTimestamp <= Date.now()) {
							notification.close();
							continue;
						}
						this.schedulePersistentExpiration(notification.tag, expirationTimestamp);
					}
					this.activeNotifications.set(notification.tag, notification);
					this.addActiveId(notification.tag);
				}
				this.stateKnown = true;
				this.pruneActivationRecords(this.activeIds, activationSnapshot);
				await this.unregisterEmptyLegacyWorkers(registrations);
				return true;
			}
			catch (error) {
				this.stateKnown = false;
				console.warn("Unable to query persistent app notifications.", error);
				return false;
			}
		}

		private static enqueuePersistentOperation<T>(operation: () => Promise<T>): Promise<T> {
			const result = this.persistentOperation.then(operation);
			this.persistentOperation = result.then(() => undefined, () => undefined);
			return result;
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

		private static createPersistentOptions(command: UnoAppNotificationCommand, activation: UnoAppNotificationActivationRecord): NotificationOptions {
			const options = this.createOptions(command);
			(<any>options).actions = command.actions.map(action => ({
				action: action.id,
				title: action.title,
				icon: this.resolveUrl(action.icon),
			}));
			options.icon = this.resolveUrl(command.icon);
			options.image = this.resolveUrl(command.image);
			options.data = {
				unoAppNotification: {
					id: command.id,
					appId: this.appId,
					activationToken: activation.body.token,
					actions: command.actions.map(action => ({
						id: action.id,
						activationToken: activation.actions[action.id].token,
					})),
					expirationTimestamp: command.expirationTimestamp,
					appBaseUrl: this.appId,
					clientUrl: this.getClientUrl(),
				},
			};
			return options;
		}

		private static waitForActiveWorker(registration: ServiceWorkerRegistration): Promise<ServiceWorkerRegistration> {
			if (registration.active !== null) {
				return Promise.resolve(registration);
			}

			const worker = registration.installing ?? registration.waiting;
			if (worker === null) {
				return Promise.reject(new Error("The notification service worker is unavailable."));
			}
			return new Promise((resolve, reject) => {
				worker.addEventListener("statechange", () => {
					if (worker.state === "activated") {
						resolve(registration);
					}
					else if (worker.state === "redundant") {
						reject(new Error("The notification service worker could not be activated."));
					}
				});
			});
		}

		private static async getPersistentRegistrations(): Promise<ServiceWorkerRegistration[]> {
			const current = this.serviceWorkerRegistration;
			const registrations: ServiceWorkerRegistration[] = [];
			for (const registration of await navigator.serviceWorker.getRegistrations()) {
				if (registration.scope === current?.scope || this.isNotificationWorker(registration)) {
					registrations.push(registration);
					continue;
				}
				if (this.isUnmarkedLegacyNotificationWorker(registration)) {
					try {
						const notifications = await registration.getNotifications();
						if (notifications.some(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))) {
							registrations.push(registration);
						}
					}
					catch {
					}
				}
			}
			if (current !== null && !registrations.some(registration => registration.scope === current.scope)) {
				registrations.push(current);
			}
			return registrations.filter((registration, index) =>
				registrations.findIndex(candidate => candidate.scope === registration.scope) === index);
		}

		private static isNotificationWorker(registration: ServiceWorkerRegistration): boolean {
			const scriptUrl = registration.active?.scriptURL ?? registration.waiting?.scriptURL ?? registration.installing?.scriptURL;
			if (scriptUrl === undefined) {
				return false;
			}
			try {
				const workerUrl = new URL(scriptUrl);
				return workerUrl.pathname.endsWith("/" + this.serviceWorkerScriptName) &&
					workerUrl.searchParams.get(this.serviceWorkerAppIdParameter) === this.appId;
			}
			catch {
				return false;
			}
		}

		private static isUnmarkedLegacyNotificationWorker(registration: ServiceWorkerRegistration): boolean {
			const scriptUrl = registration.active?.scriptURL ?? registration.waiting?.scriptURL ?? registration.installing?.scriptURL;
			if (scriptUrl === undefined) {
				return false;
			}
			try {
				const workerUrl = new URL(scriptUrl);
				return workerUrl.pathname.endsWith("/" + this.serviceWorkerScriptName) &&
					workerUrl.searchParams.get(this.serviceWorkerAppIdParameter) === null;
			}
			catch {
				return false;
			}
		}

		private static async unregisterEmptyLegacyWorkers(registrations: ServiceWorkerRegistration[]): Promise<void> {
			const current = this.serviceWorkerRegistration;
			for (const registration of registrations) {
				if (registration.scope === current?.scope) {
					continue;
				}
				const notifications = await registration.getNotifications();
				if (!notifications.some(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))) {
					if (this.isNotificationWorker(registration) || notifications.length === 0) {
						await registration.unregister();
					}
				}
			}
		}

		private static resolveUrl(value: string): string | undefined {
			if (value.length === 0) {
				return undefined;
			}
			try {
				return new URL(value, document.baseURI).href;
			}
			catch {
				return undefined;
			}
		}

		private static dispatchPendingActivation(): void {
			const url = new URL(globalThis.location.href);
			const token = url.searchParams.get("uno-app-notification");
			if (token === null) {
				return;
			}

			url.searchParams.delete("uno-app-notification");
			globalThis.history.replaceState(globalThis.history.state, "", url.href);
			globalThis.setTimeout(() => this.consumeAndActivate(0, token));
		}

		private static getClientUrl(): string {
			const url = new URL(globalThis.location.href);
			url.searchParams.delete("uno-app-notification");
			return url.href;
		}

		private static isOwnedNotification(notification: Notification): boolean {
			const data = notification.data?.unoAppNotification;
			return notification.tag.startsWith(this.tagPrefix) &&
				data?.appId === this.appId;
		}

		private static isLegacyOwnedNotification(notification: Notification): boolean {
			const data = notification.data?.unoAppNotification;
			return notification.tag.startsWith(this.tagPrefix) && data?.appId === undefined && data?.appBaseUrl === this.appId;
		}

		private static async isOwnedWorkerMessage(event: MessageEvent, message: any): Promise<boolean> {
			if (message?.appId !== this.appId || !Number.isInteger(message.id) || message.id <= 0) {
				return false;
			}
			const scriptUrl = (<ServiceWorker | null>event.source)?.scriptURL;
			if (typeof scriptUrl !== "string") {
				return false;
			}
			try {
				const registrations = await this.getPersistentRegistrations();
				const trustedWorker = registrations.some(registration =>
					registration.active?.scriptURL === scriptUrl ||
					registration.waiting?.scriptURL === scriptUrl ||
					registration.installing?.scriptURL === scriptUrl);
				return trustedWorker &&
					(message.type === "uno-app-notification-closed" ||
						(message.type === "uno-app-notification-activated" && this.isValidActivationToken(message.token)));
			}
			catch {
				return false;
			}
		}

		private static createActivationRecord(command: UnoAppNotificationCommand): UnoAppNotificationActivationRecord {
			const actions: Record<string, UnoAppNotificationActivation> = {};
			for (const action of command.actions) {
				actions[action.id] = {
					token: this.createActivationToken(),
					argument: action.argument,
					protocolUri: action.protocolUri,
				};
			}
			return {
				id: command.id,
				revision: this.createActivationRevision(),
				pending: true,
				body: {
					token: this.createActivationToken(),
					argument: command.launchArgument,
					protocolUri: command.protocolUri,
				},
				actions,
			};
		}

		private static createActivationRevision(): string {
			const bytes = new Uint8Array(16);
			globalThis.crypto.getRandomValues(bytes);
			return Array.from(bytes, value => value.toString(16).padStart(2, "0")).join("");
		}

		private static createActivationToken(): string {
			const bytes = new Uint8Array(24);
			globalThis.crypto.getRandomValues(bytes);
			return btoa(String.fromCharCode(...bytes))
				.replace(/\+/g, "-")
				.replace(/\//g, "_")
				.replace(/=/g, "");
		}

		private static isValidActivationToken(value: unknown): value is string {
			return typeof value === "string" && /^[A-Za-z0-9_-]{32}$/.test(value);
		}

		private static consumeAndActivate(id: number, token: string): boolean {
			if (!this.isValidActivationToken(token)) {
				return false;
			}
			let activation: UnoAppNotificationActivation | null;
			try {
				activation = this.withActivationStateLock(() => {
					const records = this.getActivationRecords();
					const record = id > 0
						? records[id]
						: Object.values(records).find(candidate =>
							candidate.body.token === token ||
							Object.values(candidate.actions).some(action => action.token === token));
					if (!this.isValidActivationRecord(record)) {
						return null;
					}
					const selected = record.body.token === token
						? record.body
						: Object.values(record.actions).find(action => action.token === token);
					if (selected === undefined) {
						return null;
					}
					globalThis.localStorage.removeItem(this.getActivationStorageKey(record.id));
					return selected;
				});
			}
			catch (error) {
				console.warn("Unable to consume the app notification activation token.", error);
				return false;
			}
			if (activation === null) {
				return false;
			}
			this.activate(activation.argument, activation.protocolUri);
			return true;
		}

		private static getActivationRecords(): Record<number, UnoAppNotificationActivationRecord> {
			const records: Record<number, UnoAppNotificationActivationRecord> = {};
			for (let index = 0; index < globalThis.localStorage.length; index++) {
				const key = globalThis.localStorage.key(index);
				if (!key?.startsWith(this.activationStoragePrefix)) {
					continue;
				}
				try {
					const id = Number(key.substring(this.activationStoragePrefix.length));
					const serialized = globalThis.localStorage.getItem(key);
					const value = serialized === null ? null : JSON.parse(serialized);
					if (Number.isInteger(id) && id > 0 && this.isValidActivationRecord(value) && value.id === id) {
						records[id] = value;
					}
				}
				catch {
				}
			}
			return records;
		}

		private static isValidActivationRecord(value: unknown): value is UnoAppNotificationActivationRecord {
			if (value === null || typeof value !== "object") {
				return false;
			}
			const record = <UnoAppNotificationActivationRecord>value;
			return Number.isInteger(record.id) && record.id > 0 &&
				(record.revision === undefined || this.isValidActivationRevision(record.revision)) &&
				(record.pending === undefined || typeof record.pending === "boolean") &&
				this.isValidActivation(record.body) &&
				record.actions !== null && typeof record.actions === "object" && !Array.isArray(record.actions) &&
				Object.keys(record.actions).length <= 5 &&
				Object.values(record.actions).every(action => this.isValidActivation(action));
		}

		private static isValidActivationRevision(value: unknown): value is string {
			return typeof value === "string" && /^[0-9a-f]{32}$/i.test(value);
		}

		private static isValidActivation(value: unknown): value is UnoAppNotificationActivation {
			if (value === null || typeof value !== "object") {
				return false;
			}
			const activation = <UnoAppNotificationActivation>value;
			return this.isValidActivationToken(activation.token) &&
				typeof activation.argument === "string" && activation.argument.length <= 5120 &&
				(activation.protocolUri === null || (typeof activation.protocolUri === "string" && activation.protocolUri.length <= 2048));
		}

		private static replaceActivationRecord(record: UnoAppNotificationActivationRecord): UnoAppNotificationActivationWriteResult {
			try {
				return this.withActivationStateLock(() => {
					const previous = this.getActivationRecord(record.id);
					globalThis.localStorage.setItem(this.getActivationStorageKey(record.id), JSON.stringify(record));
					return { succeeded: true, previous };
				});
			}
			catch (error) {
				console.warn("Unable to persist app notification activation tokens.", error);
				return { succeeded: false, previous: null };
			}
		}

		private static rollbackActivationRecord(
			failed: UnoAppNotificationActivationRecord,
			previous: UnoAppNotificationActivationRecord | null): void {
			try {
				this.withActivationStateLock(() => {
					const current = this.getActivationRecord(failed.id);
					if (!this.activationRecordsMatch(current, failed)) {
						return;
					}
					const key = this.getActivationStorageKey(failed.id);
					if (previous === null) {
						globalThis.localStorage.removeItem(key);
					}
					else {
						globalThis.localStorage.setItem(key, JSON.stringify(previous));
					}
				});
			}
			catch (error) {
				console.warn("Unable to roll back the app notification activation token.", error);
			}
		}

		private static commitActivationRecord(record: UnoAppNotificationActivationRecord): void {
			try {
				this.withActivationStateLock(() => {
					if (this.activationRecordsMatch(this.getActivationRecord(record.id), record)) {
						globalThis.localStorage.setItem(
							this.getActivationStorageKey(record.id),
							JSON.stringify({ ...record, pending: false }));
					}
				});
			}
			catch (error) {
				console.warn("Unable to commit the app notification activation token.", error);
			}
		}

		private static removeActivationRecord(id: number, expected: UnoAppNotificationActivationRecord): void {
			if (!Number.isInteger(id) || id <= 0 || expected.id !== id) {
				return;
			}
			try {
				this.withActivationStateLock(() => {
					if (this.activationRecordsMatch(this.getActivationRecord(id), expected)) {
						globalThis.localStorage.removeItem(this.getActivationStorageKey(id));
					}
				});
			}
			catch (error) {
				console.warn("Unable to remove the app notification activation token.", error);
			}
		}

		private static clearActivationRecords(
			records: Record<number, UnoAppNotificationActivationRecord> = this.getActivationRecords()): void {
			Object.values(records).forEach(record => this.removeActivationRecord(record.id, record));
		}

		private static pruneActivationRecords(
			activeIds: ReadonlySet<number>,
			records: Record<number, UnoAppNotificationActivationRecord> = this.getActivationRecords()): void {
			for (const id of Object.keys(records).map(Number)) {
				if (!activeIds.has(id) && records[id].pending !== true) {
					this.removeActivationRecord(id, records[id]);
				}
			}
		}

		private static getActivationRecord(id: number): UnoAppNotificationActivationRecord | null {
			const serialized = globalThis.localStorage.getItem(this.getActivationStorageKey(id));
			if (serialized === null) {
				return null;
			}
			const value = JSON.parse(serialized);
			return this.isValidActivationRecord(value) && value.id === id ? value : null;
		}

		private static activationRecordsMatch(
			current: UnoAppNotificationActivationRecord | null,
			expected: UnoAppNotificationActivationRecord): boolean {
			if (current === null) {
				return false;
			}
			const currentRevision = current.revision ?? current.body.token;
			const expectedRevision = expected.revision ?? expected.body.token;
			return currentRevision === expectedRevision && current.body.token === expected.body.token;
		}

		private static withActivationStateLock<T>(action: () => T): T {
			const owner = this.createActivationRevision();
			if (!AppNotificationStatePersistence.acquireTransactionLock(
				this.activationStateLockName,
				owner,
				this.activationStateLockTimeoutMilliseconds,
				this.activationStateLockLeaseMilliseconds)) {
				throw new Error("Unable to acquire the app notification activation-state lock.");
			}
			try {
				return action();
			}
			finally {
				AppNotificationStatePersistence.releaseTransactionLock(this.activationStateLockName, owner);
			}
		}

		private static getActivationStorageKey(id: number): string {
			return this.activationStoragePrefix + id;
		}

		private static async cleanupPreviousPersistentMode(): Promise<void> {
			try {
				await this.enqueuePersistentOperation(async () => {
					const expectedActivations = this.getActivationRecords();
					const registrations = await this.getPersistentRegistrations();
					for (const registration of registrations) {
						const notifications = await registration.getNotifications();
						notifications
							.filter(notification => this.isOwnedNotification(notification) || this.isLegacyOwnedNotification(notification))
							.forEach(notification => notification.close());
					}
					await this.unregisterEmptyLegacyWorkers(registrations);
					this.clearActivationRecords(expectedActivations);
					AppNotificationStatePersistence.removeItems("uno.appnotifications.");
				});
			}
			catch (error) {
				console.warn("Unable to clean up persistent app notification state.", error);
			}
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
				if (this.blockedProtocolSchemes.has(uri.protocol.toLowerCase())) {
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
				typeof command.title === "string" &&
				typeof command.body === "string" &&
				Array.isArray(command.actions) && command.actions.length <= 5;
		}

		private static isValidOperationCorrelation(value: string): boolean {
			return /^[0-9a-f]{32}$/i.test(value);
		}

		private static parseCommand(commandJson: string): UnoAppNotificationCommand | null {
			try {
				const command = JSON.parse(commandJson) as UnoAppNotificationCommand;
				return this.isValidCommand(command) ? command : null;
			}
			catch {
				return null;
			}
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

		private static schedulePersistentExpiration(tag: string, expirationTimestamp: number): void {
			this.clearExpiration(tag);
			const delay = expirationTimestamp - Date.now();
			if (delay <= 0) {
				void this.closePersistent(tag);
			}
			else if (delay <= 2147483647) {
				const timer = globalThis.setTimeout(() => {
					this.expirationTimers.delete(tag);
					void this.closePersistent(tag);
				}, delay);
				this.expirationTimers.set(tag, timer);
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
			const value = this.getId(tag);
			if (Number.isInteger(value)) {
				this.activeIds.delete(value);
			}
		}

		private static addActiveId(tag: string): void {
			const value = this.getId(tag);
			if (Number.isInteger(value)) {
				this.activeIds.add(value);
			}
		}

		private static getId(tag: string): number {
			return tag.startsWith(this.tagPrefix) ? Number(tag.substring(this.tagPrefix.length)) : Number.NaN;
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