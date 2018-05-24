module Uno.UI.Interop {
	export interface IWebAssemblyApp {
		main_module: Interop.IMonoAssemblyHandle;
		main_class: Interop.IMonoClassHandle;
	}
}