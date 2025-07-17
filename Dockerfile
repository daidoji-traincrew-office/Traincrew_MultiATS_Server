# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

# Install Entity Framework Core tools
RUN dotnet tool install --global dotnet-ef 
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
RUN dotnet build -c Release -a $TARGETARCH --no-restore \
    -p:DefineConstants=IS_ENABLED_PRECOMPILED_MODEL
RUN dotnet ef dbcontext optimize -v --no-build \
    --project ./Traincrew_MultiATS_Server/Traincrew_MultiATS_Server.csproj \
    -o PreCompiled \
    -n Traincrew_MultiATS_Server.Models
RUN dotnet publish -c Release -a $TARGETARCH --no-restore --no-build \
    --project:Traincrew_MultiATS_Server.Crew.csproj \
    -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
WORKDIR /opt
ENTRYPOINT ["/app/Traincrew_MultiATS_Server.Crew"]