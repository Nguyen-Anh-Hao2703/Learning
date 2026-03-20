# Bước 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy file .csproj ngay tại thư mục hiện tại
COPY ["Learning.csproj", "./"]
RUN dotnet restore "Learning.csproj"

# Copy tất cả các file còn lại và build
COPY . .
RUN dotnet publish "Learning.csproj" -c Release -o /app

# Bước 2: Chạy
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Learning.dll"]