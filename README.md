# Travelette Relay (Backend API)

This is the backend API for the Travelette admin dashboard, built with ASP.NET Core and PostgreSQL.

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database

## Setup

1. **Install Dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure Database Connection**
   
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=travelette;Username=postgres;Password=your_password"
     }
   }
   ```

3. **Create Database**
   
   The database will be automatically created when you run the application for the first time using `EnsureCreated()`.

4. **Run the Application**
   ```bash
   dotnet run
   ```

   The API will be available at:
   - HTTPS: `https://localhost:7044`
   - HTTP: `http://localhost:5158`

## API Endpoints

- `GET /api/trips` - Get all trips
- `GET /api/trips/{id}` - Get a specific trip
- `POST /api/trips` - Create a new trip
- `PUT /api/trips/{id}` - Update a trip
- `DELETE /api/trips/{id}` - Delete a trip

## CORS

CORS is configured to allow requests from the admin frontend running on:
- `http://localhost:5173`
- `http://localhost:5174`
- `http://localhost:3000`
- `http://localhost:5175`

