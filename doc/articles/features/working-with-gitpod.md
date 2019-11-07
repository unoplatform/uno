# Using GitPod

## Developing an Uno App using GitPod

To be defined.

## Contributing to Uno using GitPod

To contribute to Uno using GitPod:
1. [![Open Uno in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/unoplatform/uno)
1. In the opened shell, type the following to build the Uno solution:
    ```
    build/gitpod/build-wasm.sh
    ```
    The build should end without any errors
1. If you want to enable XAML Hot Reload, open another shell, then run:
    ```sh
    build/gitpod/serve-remote-control.sh
    ```
1. Open another shell, then start the Uno http server:
    ```sh
    build/gitpod/serve-sampleapp-wasm.sh
    ```

Once the server is started, GitPod will automatically open a browser window on the side to show the sample application.

You can make your changes in XAML directly, to view the changes through Hot Reload. If you make changes in the code, you'll need to rerun the `build-wasm.sh` script, then refresh the browser section on the side.

Once you're done with your changes, make a Pull Request through the [GitPod's GitHub integration](https://www.gitpod.io/docs/58_pull_requests/) and let us know about it on our [gitter channel](https://gitter.im/uno-platform/Lobby)!
