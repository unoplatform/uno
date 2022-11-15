FROM gitpod/workspace-dotnet-vnc

USER gitpod
#.NET installed via .gitpod.yml task until the following issue is fixed: https://github.com/gitpod-io/gitpod/issues/5090
ENV DOTNET_VERSION=7.0
ENV DOTNET_ROOT=/workspace/.dotnet
ENV PATH=$DOTNET_ROOT:$PATH

RUN sudo apt-get update
RUN sudo apt-get -y install gnupg2
RUN sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
RUN sudo apt-get update

# Install mono, msbuild and dependencies
RUN sudo apt-get -y install sudo