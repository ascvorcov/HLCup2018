FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app
COPY . ./
RUN dotnet publish -c Release -o out
FROM microsoft/dotnet:2.2-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/out ./
EXPOSE 80
ENTRYPOINT ["dotnet", "hlcup2018.dll"]

