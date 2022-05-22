FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /build
COPY RQ.Bot/RQ.Bot.csproj RQ.Bot.csproj
RUN dotnet restore RQ.Bot.csproj
COPY RQ.Bot/* ./
RUN dotnet build RQ.Bot.csproj -c Release -o /build/app

FROM build AS publish
RUN dotnet publish RQ.Bot.csproj -c Release -o /build/app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS prod
ENV RQB_TELEGRAM_TOKEN="xxxxxxx:yyyyyyyyyyyyy" \
    RQB_TELEGRAM_ADMIN_ID="0" \
    RQB_DB_PATH="/app/db" \
    RQB_QUESTIONS_PATH="/app/questions/sample.csv" \    
    RQB_URLS="http://*:9000"
EXPOSE 9000
WORKDIR /app
COPY --from=publish /build/app/* ./
RUN mkdir -p /app/db /app/questions && chown -R nobody:nogroup ./
COPY questions/* /app/questions/
USER nobody
CMD dotnet RQ.Bot.dll \
  --urls $RQB_URLS \
  --dbPath $RQB_DB_PATH \
  --botToken $RQB_TELEGRAM_TOKEN \
  --adminID $RQB_TELEGRAM_ADMIN_ID \
  --pathToQuest $RQB_QUESTIONS_PATH
