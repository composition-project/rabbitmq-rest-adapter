FROM microsoft/dotnet:2.0-runtime
ARG source
WORKDIR /app
COPY ${source:-bin} .
ENTRYPOINT ["dotnet", "RabbitHole.dll"]
