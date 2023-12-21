FROM gitpod/workspace-dotnet-vnc

USER gitpod
#.NET installed via .gitpod.yml task until the following issue is fixed: https://github.com/gitpod-io/gitpod/issues/5090
ENV DOTNET_VERSION=8.0
ENV DOTNET_ROOT=/workspace/.dotnet
ENV PATH=$DOTNET_ROOT:$PATH

# Install mono, msbuild and dependencies
RUN sudo apt-get -y install sudo