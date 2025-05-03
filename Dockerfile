FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CompanyManagementSystem.csproj", "./"]
RUN dotnet restore "CompanyManagementSystem.csproj"
COPY . .
RUN dotnet build "CompanyManagementSystem.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CompanyManagementSystem.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CompanyManagementSystem.dll"]