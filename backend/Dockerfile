ARG buildImage
ARG runtimeImage

FROM ${buildImage} as build

WORKDIR ${APP_ROOT}/src

COPY --chown=${USER} ["./backend.csproj", "./"]

RUN dotnet restore "./backend.csproj"

COPY --chown=${USER} ["./*", "./"]

RUN dotnet publish "./backend.csproj" -c Release -o ${APP_ROOT}/app/publish /p:Platform="Any CPU"

FROM ${runtimeImage} as runtime
ARG aspnetcorePort

WORKDIR ${APP_ROOT}/app
COPY --from=build ${APP_ROOT}/app/publish .

ENV ASPNETCORE_URLS=http://+:${aspnetcorePort}
EXPOSE ${aspnetcorePort}

ENTRYPOINT ["dotnet", "backend.dll"]