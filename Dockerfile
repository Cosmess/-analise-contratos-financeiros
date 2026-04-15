FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY analise-contratos-financeiros.sln ./
COPY RagFinanceiro.Api/RagFinanceiro.Api.csproj RagFinanceiro.Api/
COPY RagFinanceiro.Application/RagFinanceiro.Application.csproj RagFinanceiro.Application/
COPY RagFinanceiro.Domain/RagFinanceiro.Domain.csproj RagFinanceiro.Domain/
COPY RagFinanceiro.Infrastructure/RagFinanceiro.Infrastructure.csproj RagFinanceiro.Infrastructure/

RUN dotnet restore analise-contratos-financeiros.sln

COPY . .
RUN dotnet publish RagFinanceiro.Api/RagFinanceiro.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RagFinanceiro.Api.dll"]
