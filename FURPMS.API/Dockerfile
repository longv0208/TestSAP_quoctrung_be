# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy các file .csproj từ các thư mục tương ứng vào container
COPY ["FURPMS.API/FURPMS.API.csproj", "FURPMS.API/"]
COPY ["FURPMS.Application/FURPMS.Application.csproj", "FURPMS.Application/"]
COPY ["FURPMS.Infrastructure/FURPMS.Infrastructure.csproj", "FURPMS.Infrastructure/"]
COPY ["FURPMS.Domain/FURPMS.Domain.csproj", "FURPMS.Domain/"]

# 2. Khôi phục các gói phụ thuộc
RUN dotnet restore "FURPMS.API/FURPMS.API.csproj"

# 3. Copy toàn bộ mã nguồn
COPY . .

# 4. Build
WORKDIR "/src/FURPMS.API"
RUN dotnet build "FURPMS.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FURPMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FURPMS.API.dll"]
