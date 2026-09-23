(function (scope) {
	const isServiceWorker = typeof ServiceWorkerGlobalScope !== "undefined" && scope instanceof ServiceWorkerGlobalScope;
	if (!isServiceWorker) {
		globalThis.UnoAppNotificationsServiceWorkerUrl = document.currentScript?.src ?? "";
		return;
	}
	const appId = new URL(scope.location.href).searchParams.get("uno-app-id");

	const tryPostMessage = (client, message, ports = []) => {
		try {
			client.postMessage(message, ports);
			return true;
		}
		catch (error) {
			if (!(error instanceof DOMException)) {
				throw error;
			}
			console.warn("Unable to message an app notification client.", error);
			return false;
		}
	};

	const messageClients = async message => {
		const windows = await scope.clients.matchAll({ type: "window", includeUncontrolled: true });
		for (const client of windows) {
			tryPostMessage(client, message);
		}
		return windows;
	};

	scope.addEventListener("notificationclick", event => {
		const notification = event.notification;
		const data = notification.data?.unoAppNotification;
		if (!data || typeof appId !== "string" || data.appId !== appId) {
			return;
		}

		event.waitUntil((async () => {
			const action = Array.isArray(data.actions)
				? data.actions.find(candidate => candidate.id === event.action)
				: null;
			const token = event.action ? action?.activationToken : data.activationToken;
			if (typeof token !== "string") {
				return;
			}
			const activation = {
				type: "uno-app-notification-activated",
				id: data.id,
				appId,
				token,
			};
			try {
				const clientUrl = new URL(data.clientUrl || data.appBaseUrl);
				if (!clientUrl.href.startsWith(appId)) {
					return;
				}
				const windows = await scope.clients.matchAll({ type: "window", includeUncontrolled: true });
				for (const appWindow of windows.filter(client => client.url.startsWith(appId))) {
					if (await postActivation(appWindow, activation)) {
						await appWindow.focus();
						notification.close();
						return;
					}
				}

				const launchUrl = clientUrl;
				launchUrl.searchParams.set("uno-app-notification", token);
				const exactWindow = windows.find(client => client.url === data.clientUrl);
				if (exactWindow) {
					try {
						const navigated = await exactWindow.navigate(launchUrl.href);
						if (navigated) {
							await navigated.focus();
							notification.close();
							return;
						}
					}
					catch (error) {
						if (!(error instanceof DOMException)) {
							throw error;
						}
						console.warn("Unable to navigate an app notification client.", error);
					}
				}
				const opened = await scope.clients.openWindow(launchUrl.href);
				if (opened) {
					notification.close();
				}
			}
			catch (error) {
				console.warn("Unable to activate an app notification.", error);
			}
		})());
	});

	const postActivation = (client, activation) => new Promise((resolve, reject) => {
		const channel = new MessageChannel();
		let completed = false;
		const finish = (settle, value) => {
			if (!completed) {
				completed = true;
				scope.clearTimeout(timeout);
				channel.port1.onmessage = null;
				channel.port1.close();
				channel.port2.close();
				settle(value);
			}
		};
		const timeout = scope.setTimeout(() => finish(resolve, false), 750);
		channel.port1.onmessage = message => {
			finish(resolve, message.data?.accepted === true);
		};
		try {
			if (!tryPostMessage(client, activation, [channel.port2])) {
				finish(resolve, false);
			}
		}
		catch (error) {
			finish(reject, error);
		}
	});

	scope.addEventListener("notificationclose", event => {
		const data = event.notification.data?.unoAppNotification;
		if (data?.appId === appId) {
			event.waitUntil(messageClients({ type: "uno-app-notification-closed", id: data.id, appId }));
		}
	});
})(self);