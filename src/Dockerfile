ARG buildImage
ARG runtimeImage

FROM ${buildImage} as build

WORKDIR ${APP_ROOT}/src

COPY --chown=${USER} ["./src.csproj", "./"]

RUN dotnet restore "./src.csproj"

COPY --chown=${USER} ["./*", "./"]

RUN dotnet publish "./src.csproj" -c Release -o ${APP_ROOT}/app/publish /p:Platform="Any CPU"

FROM ${runtimeImage} as runtime
ARG aspnetcorePort

WORKDIR ${APP_ROOT}/app
COPY --from=build ${APP_ROOT}/app/publish .

ENV ASPNETCORE_URLS=http://+:${aspnetcorePort}
EXPOSE ${aspnetcorePort}

ENTRYPOINT ["dotnet", "src.dll"]