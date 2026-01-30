# Philosophy of Uno Platform

## Mission

**To empower developers to build pixel-perfect, single-codebase applications for Windows, WebAssembly, iOS, macOS, Android, and Linux.**

Uno Platform is committed to providing tools for developers to efficiently create high-quality, native-feeling applications across all modern platforms from a unified codebase.

## Vision

Uno Platform strives to be the leading cross-platform UI framework enabling .NET developers to reuse their skills and code, thereby shrinking development time and cost while maximizing reach. Our vision is to make true cross-platform UI/UX parity possible—whether deploying to mobile, web, or desktop—with full support for the latest device capabilities, modern tooling including AI, and next-generation development experiences.

---

## Core Philosophy & Principles

### 1. Leverage Existing Tools and Skills

Uno Platform is built on the foundation of well-established Microsoft technologies—including XAML and C#—so developers can apply the skills they already know instead of learning entirely new paradigms. And because Uno Platform supports development from any operating system, teams can build and ship cross-platform applications using the tools and workflows that fit their environment best.

**Key Benefits:**

- Maximize productivity with cross-OS tooling like C# and XAML Hot Reload, Hot Design for real-time visual editing, AI-powered assistance through Docs MCP and App MCP, and streamlined CLI tools such as uno-check and project templates.
- Build apps using proven Microsoft technologies and deploy to multiple platforms: WebAssembly, iOS, Android, macOS, Windows, and Linux
- Reduce learning curve for .NET developers transitioning to cross-platform development
- Day-0 support for the latest .NET and Visual Studio ecosystems

### 2. AI-Powered Agentic Development

As of 2024-2026, Uno Platform has embraced AI-powered development workflows to dramatically increase developer productivity while maintaining full developer control.

**Innovation Highlights:**

- **Hot Design:** The only visual designer that enables real-time, on-app visual editing and AI-assisted UI creation.
- **AI Agents in Development:** Uno Platform Studio 2.0 (an AI-powered development tool suite) introduces AI assistants like the Hot Design Agent, an AI-powered design assistant that can understand your code, context, and even control the running app to suggest and implement UI changes in real time
- **Human-in-the-Loop:** Developers preview and approve changes, ensuring quality while benefiting from AI assistance
- **MCP Servers:** AI interacts with documentation, APIs, app states, and UI elements live via Model Context Protocol (MCP) servers, providing context-aware assistance
- **App Live Testing:** AI agents can interact with running apps, trigger UI events, and automate testing/debugging with human approval

This approach represents a fundamental shift in how developers work—enabling faster iterations, better design fidelity, and reduced time-to-market without sacrificing code quality or control.

### 3. Pixel-Perfect, Rich, and Responsive UIs

We believe developers shouldn't have to choose between productivity and great design. Uno Platform prioritizes both.

**Our Commitment:**

- Support sophisticated animations, templating, and visual effects
- Achieve responsive, pixel-perfect interfaces that adapt across devices
- Enable designers and developers to work together seamlessly with tools like Figma integration (one-click export from Figma to C# or XAML)
- Provide visual designers with hot reload capabilities on running apps for instant feedback

### 4. Separation of Concerns

Uno Platform advocates a clear separation between model, view, and presentation logic, promoting MVVM (Model-View-ViewModel) principles.

**Architecture Benefits:**

- Clean, maintainable code using features like data binding and attached properties
- Support for both classic MVVM and modern state management approaches (MVUX)
- Scalable application architecture that grows with your team and project
- Testable code with clear boundaries between business logic and UI

### 5. Native Inter-compatibility

While promoting maximum code reuse as the ideal approach, Uno recognizes the need for platform-specific functionality.

**Flexibility Features:**

- Support "escape hatches" for platform-specific code when needed
- Easy integration of native third-party libraries and APIs
- Ability to call platform-specific APIs directly from shared code
- Hybrid UI scenarios combining web and native elements

### 6. Performance as a Feature

Performance is not an afterthought; it's a primary consideration. We prioritize optimization based on real-world profiling and ongoing enhancements to ensure apps are fast and responsive.

**Performance Focus:**

- Optimized Skia rendering engine for smoother performance
- Hardware-accelerated UI features (e.g., hardware-accelerated shadows)
- Optimized image loading (e.g., offloading decoding to WebWorkers for WebAssembly)
- Continuous profiling and performance improvements in every release
- Prevention of poor user experiences and negative reviews through proactive performance work

---

## Comprehensive Cross-Platform Support

Uno Platform enables building applications for all major platforms from a **single codebase**:

- **Windows** (Win32, UWP, WinUI 3)
- **WebAssembly** (Browser-based, no plugins required)
- **iOS** (iPhone and iPad)
- **Android** (Phones and tablets)
- **macOS** (Native Mac applications)
- **Linux** (GTK-based applications)
- **Embedded devices** (IoT and specialized hardware)

This comprehensive support dramatically lowers maintenance costs and reduces time-to-market for updates and new features.

---

## Enterprise-Grade Tooling & Ecosystem

### Development Tools

- **Visual Studio Integration:** Full support for Visual Studio 2026 (and previous versions)
- **Always on the latest .NET:** Day-0 support for new .NET releases, with preview-specific builds available for teams who want to stay on the bleeding edge.
- **Hot Reload & Real-Time UI Design:** Visual designer lets you tweak XAML and C# on a running app with instant changes
- **Status Indicators:** Enhanced developer experience with environment health monitoring, restore progress, and SDK validation

### Rich Component Ecosystem

- Seamless integration with **WinUI controls**
- **Windows Community Toolkit**
- **Uno Toolkit** for additional controls and extensions
- **.NET MAUI controls** compatibility
- Hundreds of UI components available out of the box
- Extensive open-source and 3rd party libraries support

### Collaboration Features

- **Figma Integration** for rapid prototyping and design handoff
- **Modern Solution Formats:** Support for the human-readable `.slnx` solution format for easier team collaboration and code reviews
- **Source Control Friendly:** Designed with team development and version control in mind

---

## Open Source and Commercial Flexibility

Uno Platform balances open-source principles with commercial sustainability:

- **Open-Source Foundation:** Free and open-source under the Apache 2.0 license
- **Commercial Tools:** Optional AI-powered workflows and visual designers available
- **Community Edition:** Full access to core platform features
- **Professional Edition:** Advanced features for enterprise teams
- **Educational Support:** Discounts for educational institutions and open-source contributors

---

## Why Uno Platform?

### For Organizations

- **Cost Efficiency:** Single codebase significantly reduces development and maintenance costs compared to platform-specific implementations
- **Faster Time-to-Market:** Deploy to all platforms simultaneously rather than building separately
- **Future-Proof:** Active development, regular updates, and commitment to latest technologies
- **Risk Mitigation:** Open-source foundation ensures you're never locked in

### For Developers

- **Leverage Existing Skills:** Use your C# and XAML knowledge immediately
- **Career Growth:** Cross-platform expertise is highly valued in the market
- **Modern Tooling:** AI-assisted development, hot reload, visual designers
- **Community Support:** Active community, extensive documentation, and commercial support options

### For Teams

- **Unified Codebase:** Frontend and backend teams can work in the same language and ecosystem
- **Design-Developer Collaboration:** Figma integration and visual tools bridge the gap
- **Scalable Architecture:** From prototypes to enterprise applications
- **Quality Assurance:** Single codebase means testing once deploys everywhere

---

## Looking Forward

Uno Platform continues to evolve with the industry, embracing:

- **AI Integration:** Making AI-powered development accessible and productive
- **Modern .NET:** Supporting the latest .NET releases on day-0
- **Performance:** Continuous optimization for better user experiences
- **Developer Experience:** Tools that make developers more productive and happier
- **Platform Expansion:** Supporting emerging platforms and form factors

Our philosophy is simple: **empower developers with the best tools, leverage proven technologies, and never compromise on quality, performance, or developer experience.**

---

## Learn More

- **Official Website:** [https://platform.uno](https://platform.uno)
- **Documentation:** [https://platform.uno/docs](https://platform.uno/docs)
- **GitHub:** [https://github.com/unoplatform](https://github.com/unoplatform)
- **Community:** [https://discord.gg/eBHZSKG](https://discord.gg/eBHZSKG)
