module Uno.UI {
	export interface IContentDefinition {
		id: string;
		tagName: string;
		handle: number;
		uiElementRegistrationId: number;
		isSvg: boolean;
		isFocusable: boolean;
	}
}
