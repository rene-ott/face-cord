FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build-env
WORKDIR /app

COPY *.sln .
COPY *.csproj .

RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim
WORKDIR /app
COPY --from=build-env /app/out .
RUN apt-get update && apt-get install -y xorg openbox libnss3 libasound2
ENTRYPOINT ["dotnet", "FaceCord.dll"]