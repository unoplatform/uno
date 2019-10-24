# build troubleshooting


## on your computer

Start by deleting `mono-wasm-xxx` in `%temp%` or `$TEMP`.

Increase the build log verbosity in [Visual Studio to diagnostics](https://docs.microsoft.com/en-us/visualstudio/msbuild/obtaining-build-logs-with-msbuild?view=vs-2019). 

Head to Tools -> Options -> MS Build Verbosity

- Change to detailed (window/console logger)
- Change to detailed (log)

Quit visual studio and ensure that all processes have been terminated.

```bash
taskkill /im "devenv*" /f /t
taskkill /im "msbuild*" /f /t
taskkill /im "uno*" /f /t
```

Reset the source generation cache and all local changes to your git repository. Be careful, as this will wipe out code you haven't yet commited.

```bash
git reset --hard
git clean -fdx
git checkout master
git pull origin master
```

Open the solution again using the appropriate solution filter and wait for background tasks (bottom left blue corner) to be {} aka None aka Ready.

Compile the `Uno UI -> Uno.UI` project.

If that doesn't work, try the following.

```
nuget locals all -clear
cd src
msbuild Uno.UI.sln /m /r /bl:restore.binlog /t:restore
msbuild Uno.UI.sln /m /r /bl:build.binlog
```

Then privately share the binlog. Please note that binlogs can contain sensitive information such as environment variables, thus if `ARM_CLIENT_SECRET` or `AWS_IAM_SECRET` or `MY_SUPER_SECRET_GITHUB_TOKEN` are set then they will be inside the logs and anyone with the logs can view em. Be careful.

## on azure devops

Logs and artifacts for a specific build can be accessed via `https://uno-platform.visualstudio.com/Uno%20Platform/_build/results?buildId=<buildid>`

NuGet build artifacts can be consumed via `https://pkgs.dev.azure.com/uno-platform/Uno Platform/_packaging/Features/nuget/v3/index.json`
