#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine3.17-amd64 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# OpenSSL 3.0 disables UnsafeLegacyRenegotiation by default, must re-enable it for some endpoints (see https://github.com/dotnet/runtime/issues/80641)
RUN sed -i 's/providers = provider_sect/providers = provider_sect\n\
ssl_conf = ssl_sect\n\
\n\
[ssl_sect]\n\
system_default = system_default_sect\n\
\n\
[system_default_sect]\n\
Options = UnsafeLegacyRenegotiation/' /etc/ssl/openssl.cnf

RUN apk add libwbclient libunistring libssl1.1 zlib libc6-compat
RUN apk -U upgrade # update package list and upgrade installed packages for security patches
RUN mkdir -p /usr/local/lib/gssntlmssp /usr/etc/gss/mech.d
COPY ["deps/gssntlmssp.so", "/usr/local/lib/gssntlmssp"]
COPY ["deps/mech.ntlmssp.conf", "/usr/etc/gss/mech.d"]

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine-amd64 AS build
WORKDIR /src
COPY ["EwtLinkGenerator.csproj", "EwtLinkGenerator/"]
RUN dotnet restore "EwtLinkGenerator/EwtLinkGenerator.csproj"
COPY . .
WORKDIR "/src/EwtLinkGenerator"
RUN dotnet build "EwtLinkGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EwtLinkGenerator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EwtLinkGenerator.dll"]