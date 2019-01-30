module Uno.UI {
	export interface IContentDefinition {
		id: number;
		tagName: string;
		handle: number;
		type: string;
		isSvg: boolean;
		isFrameworkElement: boolean;
		isFocusable: boolean;
		classes?: string[];
	}
}
