FROM mcr.microsoft.com/dotnet/core/runtime:latest
ARG TagName

WORKDIR /AVD3
COPY Dockerfile-Entrypoint.sh /

RUN apt-get update && apt-get install -y curl zip
RUN /Dockerfile-Entrypoint.sh USEVERSION $TagName NOOP

ENTRYPOINT ["/Dockerfile-Entrypoint.sh"]

