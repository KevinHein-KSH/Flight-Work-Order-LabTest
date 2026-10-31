# LabTest

A small full‑stack application to manage flights and attach validated work‑order commands.

- Frontend: React + TypeScript (Vite, MUI, TanStack Query)
- Backend: ASP.NET Core (.NET 8, EF Core + SQLite)

## Features

### Flights
- Upload CSV/JSON files with required fields (validated client‑ and server‑side)
- Paginated, filterable, and sortable flights list
- Shows “Last Active Work Order” timestamp per flight

### Work Orders
- Per‑flight history with pagination and an “Active” row
- Command input with live preview and validation; submit disabled until valid
- Only one active work order per flight; submitting a new one closes the previous

## Tech Stack
- Frontend: React 19, Vite, TypeScript, MUI, TanStack Query, Axios
- Backend: ASP.NET Core 8, EF Core (SQLite), Swagger

## Getting Started

### Prerequisites
- Node.js 18+ (or 20+)
- .NET 8 SDK

### 1) Backend

```bash
cd backend
dotnet run
```

- Swagger: http://localhost:5069/swagger
- DB: SQLite at `backend/labtest.db` (created by EF; migrations are included under `backend/Migrations`)
  - If needed: `dotnet ef database update` to apply migrations explicitly

### 2) Frontend
```bash
cd frontend
npm install
npm run dev
```
- Dev server: http://localhost:5173
- API base URL: `frontend/src/services/http.ts` (defaults to `http://localhost:5069/api`). Update if you run the backend on another port.

### CORS
Backend allows CORS for `http://localhost:5173` and `http://127.0.0.1:5173`. Adjust in `backend/Program.cs` if your origin differs.

## Usage

### Upload Flights
- Click “Choose File”, pick a `.csv` or `.json`.
- The filename is shown; Upload is enabled only when the file is valid.
- Click “Upload” to send to `/api/Flight/bulk-upload`.

Required fields (case‑insensitive):
- `FlightNumber`, `ScheduledArrivalTimeUtc`, `OriginAirport`, `DestinationAirport`

CSV example (headers required):
```csv
FlightNumber,ScheduledArrivalTimeUtc,OriginAirport,DestinationAirport
BA123,2025-06-01T12:30:00Z,LHR,JFK
```

JSON example (array of objects):
```json
[
  {
    "flightNumber":"BA123",
    "scheduledArrivalTimeUtc":"2025-06-01T12:30:00Z",
    "originAirport":"LHR",
    "destinationAirport":"JFK"
  }
]
```

Notes
- Client‑side validation blocks upload with a generic “Invalid file” message if any required field/header is missing.
- Server also validates schema and data; errors return `400`.

### Flights Table
- Filter by flight number.
- Click “Flight #” to sort asc/desc/none (sorting is applied client‑side within the current page).
- “Last WO” shows the latest active work order time for each flight (projected by the backend).

### Work Orders
- Select a flight to view/create work orders.
- Command format (tokens, `|` separated): `CHK<number>`, `BAG<number>`, `CLEAN<number>`, `PBB<0|90|180|270>`
  - Example: `CHK15|BAG25|CLEAN10|PBB90`
- Live preview shows parsed chips; submit is disabled until the command is valid.
- The newest submitted work order becomes Active; previous actives are closed automatically.

## API Overview
- `GET /api/Flight?pageNumber=1&pageSize=10` — paged flights list,
  returns `{ items, totalCount, pageNumber, pageSize }` of `ResponseFlightModel` with `lastActiveWO` timestamp.
- `POST /api/Flight/bulk-upload` — multipart upload of CSV/JSON; returns `{ count, errors }`.
- `GET /api/WorkOrder/flight/{flightId}?pageNumber=1&pageSize=10` — flight details with `workOrdersPreview` and `activeWorkOrder`.
- `POST /api/WorkOrder/{flightId}` — create a new work order with body `{ raw: string }`.

## Configuration
- Frontend API base URL: `frontend/src/services/http.ts`
- Backend connection string: `backend/appsettings.json` (`DefaultConnection` to `labtest.db`)

## Troubleshooting
- CORS: If you see CORS errors, ensure backend is running on `http://localhost:5069` and the origin is allowed in `Program.cs`.
- Invalid file on upload: Ensure headers/keys match the required fields and the file extension is `.csv` or `.json`.
- DB reset: stop backend and remove `backend/labtest.db`, then run again to recreate.

## Scripts

- Frontend
  - `npm run dev` — start Vite dev server
  - `npm run build` — type‑check and build
  - `npm run preview` — preview built assets
- Backend
  - `cd backend && dotnet run` — run API on `http://localhost:5069`

## AI Tool Usage

 - Used AI assistance for:
   - React Query hooks scaffolding
   - TypeScript type definitions
   - README/documentation structuring
   - Backend business logic written manually

If I Had 2 More Days...

  High Priority

  - Add unit tests for parser and integration tests for API endpoints
  - Add CSV/Excel export of work order history

  Nice-to-Have

  - Advanced filters (by date range, airport)
  - Dockerized setup for one‑command start

## Notes
- Sorting is client‑side; for server‑side sorting across pages, add query params and extend the backend accordingly.
- The flights list DTO is projected in the backend to include the active work order timestamp to avoid N+1 queries.
