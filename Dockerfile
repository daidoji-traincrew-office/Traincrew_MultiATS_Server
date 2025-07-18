# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

# Install Entity Framework Core tools
RUN dotnet tool install --global dotnet-ef --version 8.0.18
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy project file and restore as distinct layers
RUN mkdir -p /source/Traincrew_MultiATS_Server.Common
COPY --link Traincrew_MultiATS_Server.Common/*.csproj Traincrew_MultiATS_Server.Common/
RUN mkdir -p /source/Traincrew_MultiATS_Server
COPY --link Traincrew_MultiATS_Server/*.csproj Traincrew_MultiATS_Server/
RUN mkdir -p /source/Traincrew_MultiATS_Server.Crew
COPY --link Traincrew_MultiATS_Server.Crew/*.csproj Traincrew_MultiATS_Server.Crew/
RUN cd ./Traincrew_MultiATS_Server.Crew && dotnet restore -a $TARGETARCH 

# Copy source code and publish app
COPY --link Traincrew_MultiATS_Server.Common/* Traincrew_MultiATS_Server.Common/
COPY --link Traincrew_MultiATS_Server/* Traincrew_MultiATS_Server/
COPY --link Traincrew_MultiATS_Server.Crew/* Traincrew_MultiATS_Server.Crew/

## PrecompiledModelを生成
RUN cd Traincrew_MultiATS_Server \
    && dotnet ef dbcontext optimize \
        --configuration Release \
        -o PreCompiled \
        -n Traincrew_MultiATS_Server.Models
# PrecompiledModelを有効にしてビルドする
# ここで定義を有効にしてビルドする
RUN cd Traincrew_MultiATS_Server.Crew \
    && dotnet publish \
        --configuration Release \
        -a $TARGETARCH \
        --no-restore \
        -p:DefineConstants=IS_ENABLED_PRECOMPILED_MODEL \
        -p:DebugType=full \
        -o /app \


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS download_tools
RUN dotnet tool install dotnet-trace --version 9.0.621003 --tool-path /.dotnet/tools

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=download_tools /.dotnet/tools /.dotnet/tools
ENV PATH="${PATH}:/.dotnet/tools"
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
WORKDIR /opt
ENTRYPOINT ["/app/Traincrew_MultiATS_Server.Crew"]