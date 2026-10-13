FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Medzo.PurchasingSupplier.sln
RUN dotnet publish src/Medzo.PurchasingSupplier.Api/Medzo.PurchasingSupplier.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS dotnet-runtime

FROM apache/kafka:3.9.0 AS final
USER root
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="${PATH}:/usr/share/dotnet"

COPY --from=dotnet-runtime /usr/share/dotnet /usr/share/dotnet
COPY --from=build /app/publish .
COPY docker/start-with-kafka.sh /usr/local/bin/start-with-kafka.sh

RUN chmod +x /usr/local/bin/start-with-kafka.sh \
    && chown -R appuser:root /app

USER appuser
ENTRYPOINT ["/usr/local/bin/start-with-kafka.sh"]
