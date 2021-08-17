FROM mcr.microsoft.com/dotnet/sdk:5.0.200 AS build

COPY . ./

RUN dotnet publish ./ActionRunner/ActionRunner.csproj -c Release -o out --no-self-contained

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0
COPY --from=build /out .

ENTRYPOINT [ "dotnet", "/ActionRunner.dll" ]
