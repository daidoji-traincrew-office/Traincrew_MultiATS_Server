# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

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
RUN cd ./Traincrew_MultiATS_Server.Crew && dotnet publish -a $TARGETARCH --no-restore -o /app


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
WORKDIR /opt
COPY Traincrew_MultiATS_Server.Crew/Data /opt/Data
ENTRYPOINT ["/app/Traincrew_MultiATS_Server.Crew"]