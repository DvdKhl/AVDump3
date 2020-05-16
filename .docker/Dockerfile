FROM mcr.microsoft.com/dotnet/core/runtime:latest

WORKDIR /AVD3

RUN apt-get update && apt-get install -y curl zip
RUN ./Dockerfile-Entrypoint.sh USERVERSION ${DockerTag} NOOP

COPY Dockerfile-Entrypoint.sh /
ENTRYPOINT ["/Dockerfile-Entrypoint.sh"]

