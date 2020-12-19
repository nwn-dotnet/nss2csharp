FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
RUN apt-get update && apt-get clean && rm -rf /var/lib/apt/lists/*
ADD . /Build
WORKDIR /Build
RUN dotnet publish -c Release -o /publish
FROM mcr.microsoft.com/dotnet/runtime
WORKDIR /app
COPY --from=build /publish .
CMD ["dotnet", "Nss2CSharp.dll"]
