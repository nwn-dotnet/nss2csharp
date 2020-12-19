FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
ADD . /Build
WORKDIR /Build
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/runtime
RUN apt update && apt install wget -y && apt clean && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /publish .
CMD ["dotnet", "Nss2CSharp.dll"]
