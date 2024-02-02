---
uid: Uno.QA.RemoteControl
---

# Remote Control QA

- Install Latest vsix from CI artifacts
  - VS2017
  - VS2019
- Create a new App project
- Build all projects one by one
  - Validate no build error
  - Validate no restore errors

- Build and launch app [Wasm|iOS|Android]
  - Restart VS if started
  - In MainPage.xaml, change the Text property to something else
  - Save
  - Observe a UI change

- Move the iOS project in a solution folder
  - Restart VS if started
  - Build and launch iOS app
  - In MainPage.xaml, change the Text property to something else
  - Save
  - Observe a UI change
