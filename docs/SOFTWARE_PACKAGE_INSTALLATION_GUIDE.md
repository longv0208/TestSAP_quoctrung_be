# FURPMS Software Package — Installation Guide

**System:** FPT University Research Project Management System (FURPMS)  
**Package components:** Backend source code, frontend source code, database migrations/sample data,
installation instructions, and role-based test accounts.

> Place this file at the root of the submitted software package, next to `FURPMS_BEv2` and
> `FURPMS-Web`. The package must not contain real API keys, SMTP passwords, production database
> credentials, or personal `.env.local` files.

## 1. Package contents

The submitted archive should have the following structure:

```text
FURPMS_Software_Package/
├── SOFTWARE_PACKAGE_INSTALLATION_GUIDE.md   # this document
├── FURPMS_BEv2/                             # ASP.NET Core backend
│   ├── FURPMS_BE.sln
│   ├── docker-compose.yml                   # PostgreSQL 16
│   ├── FURPMS.API/
│   ├── FURPMS.Application/
│   ├── FURPMS.Domain/
│   ├── FURPMS.Infrastructure/
│   │   └── Migrations/                      # database schema history
│   ├── FURPMS.Tests/
│   └── docs/
├── FURPMS-Web/                              # React/Vite frontend
│   ├── package.json
│   ├── package-lock.json
│   ├── src/
│   ├── public/
│   └── .env.development
└── database/                                # optional when a SQL file is required
    └── furpms_sample.sql
```

Do not include generated or secret files:

- Backend: `bin/`, `obj/`, `FURPMS.API/appsettings.Development.json`, local upload folders.
- Frontend: `node_modules/`, `dist/`, `.env.local`, `.env.*.local`.
- Infrastructure: Docker volume data, production database dumps, real user data.

## 2. Technology stack

| Component | Technology | Required/recommended version |
|---|---|---|
| Backend | ASP.NET Core Web API | .NET SDK 8.0 or later capable of targeting `net8.0` |
| ORM | Entity Framework Core | 8.0 |
| Database | PostgreSQL | 16, provided through Docker Compose |
| Frontend | React + TypeScript + Vite | React 19, TypeScript 6, Vite 8 |
| JavaScript runtime | Node.js | Node.js 22 LTS recommended; minimum supported by Vite 8 |
| Package manager | npm | npm 10 or later |
| Container runtime | Docker Desktop / Docker Engine | Docker Compose v2 or later |
| Browser | Chrome, Edge, or Firefox | Current stable version |

The package was last verified with .NET SDK `10.0.100` targeting .NET 8, Node.js `24.11.0`, npm
`11.6.1`, Docker `29.6.2`, and Docker Compose `5.3.1`. These are verification versions, not strict
minimum requirements.

## 3. Network ports

| Service | Address | Purpose |
|---|---|---|
| PostgreSQL | `localhost:5433` | Local database; port 5433 avoids conflicts with native PostgreSQL on 5432 |
| Backend API | `http://localhost:5068` | REST API and Swagger |
| Swagger UI | `http://localhost:5068/swagger` | API inspection and manual testing |
| Frontend | `http://localhost:5173` | Web application |

If one of these ports is occupied, stop the conflicting process. Changing a port requires updating
the database connection string or `VITE_API_BASE_URL` accordingly.

## 4. Quick installation — recommended method

The following procedure creates a clean PostgreSQL database, applies all Entity Framework
migrations, loads master data, and loads the sample/demo scenarios automatically.

### Step 1 — Extract the package

Extract the archive to a path without restrictive permissions, for example:

```text
D:\FURPMS_Software_Package\
```

Keep `FURPMS_BEv2` and `FURPMS-Web` as sibling directories. Do not move individual backend
projects out of `FURPMS_BEv2` because the solution uses relative project references.

### Step 2 — Start PostgreSQL

Open PowerShell or a terminal in the backend directory:

```powershell
cd D:\FURPMS_Software_Package\FURPMS_BEv2
docker compose up -d
docker compose ps
```

Expected result: container `furpms-db-1` is running and becomes `healthy`. The Compose file creates:

- Database: `furpms`
- User: `postgres`
- Host port: `5433`
- Persistent volume: `pg_data`

The database password in `docker-compose.yml` is for the local academic package only. Replace it
before deploying to a public server.

### Step 3 — Restore, build, and run the backend

```powershell
cd D:\FURPMS_Software_Package\FURPMS_BEv2
dotnet restore FURPMS_BE.sln
dotnet build FURPMS_BE.sln
dotnet run --project FURPMS.API --launch-profile http
```

At startup, the API automatically:

1. Connects to PostgreSQL.
2. Applies pending Entity Framework migrations.
3. Seeds mandatory roles, master data, settings, rubric templates, and the administrator account.
4. In `Development`, seeds role-based demo accounts and projects at different workflow stages.

The backend is ready when `http://localhost:5068/swagger` opens successfully. Keep this terminal
running while using the frontend.

### Step 4 — Install and run the frontend

Open a second terminal:

```powershell
cd D:\FURPMS_Software_Package\FURPMS-Web
npm ci
npm run typecheck
npm run build
npm run dev
```

Open `http://localhost:5173`.

The committed `.env.development` already contains:

```dotenv
VITE_API_BASE_URL=http://localhost:5068/api
VITE_USE_MOCK_API=false
```

Do not change `.env.production` for a local installation. If a machine-specific override is needed,
create `.env.development.local`; this file must not be included in the submission.

## 5. Optional AI configuration

Core workflows run without an AI key; AI buttons display a controlled message and users can enter
data manually. To demonstrate AI extraction and reviewer suggestions, create the ignored file:

`FURPMS_BEv2/FURPMS.API/appsettings.Development.json`

```json
{
  "GeminiAI": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-3.5-flash-lite",
    "FallbackModel": "gemini-3.1-flash-lite"
  }
}
```

Restart the backend after changing this file. Never place the real key in `appsettings.json`, source
control, screenshots, the SQL dump, or the submitted archive. Before the demonstration, run one AI
request and confirm that a cached result is available as a fallback for rate-limit or network errors.

## 6. Optional email configuration

Email is not required for the core installation because all important events also create in-app
notifications and email logs. To send real messages, add the following section to the same ignored
`appsettings.Development.json` file:

```json
{
  "EmailSettings": {
    "SmtpServer": "YOUR_SMTP_HOST",
    "SmtpPort": 587,
    "SmtpUsername": "YOUR_SMTP_USERNAME",
    "SmtpPassword": "YOUR_SMTP_PASSWORD",
    "FromEmail": "YOUR_VERIFIED_SENDER",
    "FromName": "FURPMS",
    "FrontendUrl": "http://localhost:5173"
  }
}
```

Use a verified sender. Fake demo addresses under `@furpms.edu.vn` should be redirected to a controlled
test inbox when testing email delivery; they are not real university mailboxes.

## 7. File storage

For local installation, uploaded files are stored on local disk automatically; no cloud account is
required. For a public deployment with an ephemeral filesystem, configure Cloudinary through secure
environment variables:

```text
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
Cloudinary__Folder
```

Do not include these values in the software package. The API remains the authorized download gateway;
private document provider URLs are not exposed directly to the frontend.

## 8. Database and sample data

### 8.1 Recommended submitted database format

The authoritative database definition is included as Entity Framework migrations in the backend.
The sample database is reproducible: starting the API in `Development` applies migrations and runs
the idempotent seeders. This is preferred to relying on a machine-specific database backup.

The seeded scenarios include:

- Research cycles `UD26` and `CB26`.
- Research types: applied research and basic research.
- Role-based users and academic profiles.
- Rubric BM03 (100 points) and BM10 (20 points, acceptance reviewer form).
- Projects `NCKH-2026-001` through `NCKH-2026-011` at different workflow stages.
- Review councils, invitations, meetings, ballots, contracts, progress reports, amendments,
  deliverables, disbursement milestones, final reports, acceptance, settlement, and notifications.

Seed operations are idempotent: restarting the backend does not intentionally duplicate the fixed
demo scenarios.

### 8.2 Create an optional `furpms_sample.sql`

If the submission portal explicitly requires a database file, start the database and backend once so
migrations and seed data are complete. Then create the `database` directory at the package root and
export the sample database:

```powershell
cd D:\FURPMS_Software_Package
New-Item -ItemType Directory -Force -Path .\database | Out-Null
docker exec furpms-db-1 pg_dump -U postgres -d furpms --clean --if-exists --no-owner --no-privileges |
  Set-Content -Encoding utf8 .\database\furpms_sample.sql
```

Verify that the file is non-empty:

```powershell
Get-Item .\database\furpms_sample.sql | Select-Object Name, Length, LastWriteTime
```

The sample dump contains password hashes and demonstration records only. Inspect it before submission
and never export a production database containing real personal information.

### 8.3 Restore the optional SQL file

Use this only when evaluating the submitted SQL snapshot instead of allowing migrations/seeders to
create a fresh database. Stop the backend first. The commands below delete only the local Docker
database named `furpms` inside container `furpms-db-1`:

```powershell
docker exec furpms-db-1 psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS furpms;"
docker exec furpms-db-1 psql -U postgres -d postgres -c "CREATE DATABASE furpms;"
Get-Content -Raw .\database\furpms_sample.sql |
  docker exec -i furpms-db-1 psql -U postgres -d furpms
```

Then restart the backend. Its idempotent startup seeder verifies mandatory data without duplicating
the fixed sample scenarios.

### 8.4 Reset to a clean generated sample database

This operation irreversibly deletes the local Docker volume for this Compose project. Run it only in
`FURPMS_BEv2`, never against a shared or production database:

```powershell
cd D:\FURPMS_Software_Package\FURPMS_BEv2
docker compose down -v
docker compose up -d
dotnet run --project FURPMS.API --launch-profile http
```

## 9. Test accounts and permissions

All Development demo accounts use password `password`.

| System role/use | Display identity | Email | Main permissions |
|---|---|---|---|
| Administrator | System Administrator | `admin@furpms.edu.vn` | Users, roles, master data, cycles, settings, analytics |
| Research Office Staff | Trần Thị Mai Lan | `staff.demo@furpms.edu.vn` | Review rounds, councils, meetings, contracts, progress and acceptance administration |
| Principal Investigator 1 | Nguyễn Văn An | `pi.demo@furpms.edu.vn` | Proposals, team, budget, products, progress/final reports, amendments |
| Principal Investigator 2 | Hoàng Văn Bình | `pi2.demo@furpms.edu.vn` | Second PI dataset and multi-project scenarios |
| Committee Chair | PGS.TS. Lê Quang Minh | `reviewer1.demo@furpms.edu.vn` | Scores assigned projects and locks council minutes/decisions |
| Committee Secretary | TS. Phạm Thu Hương | `reviewer2.demo@furpms.edu.vn` | Scores and prepares council minutes |
| Opponent/Reviewer | TS. Vũ Đình Nam | `reviewer3.demo@furpms.edu.vn` | Proposal review and BM10 acceptance assessment |
| Committee Member | TS. Đặng Hoài Anh | `reviewer4.demo@furpms.edu.vn` | Scores or votes on assigned projects |
| Committee Member | ThS. Bùi Thanh Hà | `reviewer5.demo@furpms.edu.vn` | Scores or votes on assigned projects |

Committee positions such as Chair, Secretary, Opponent, and Member are assignments inside a specific
council, not separate global login roles. The seed data consistently assigns reviewer accounts 1–3
to the positions shown above for demonstration convenience.

The password `password` is intentionally simple for offline academic evaluation. Disable demo data
and replace all credentials before any public or production use.

## 10. Verification checklist

### Backend and database

```powershell
cd D:\FURPMS_Software_Package\FURPMS_BEv2
docker compose ps
dotnet build FURPMS_BE.sln
dotnet test FURPMS_BE.sln
```

Confirm:

- [ ] PostgreSQL container is healthy.
- [ ] Backend build and tests succeed.
- [ ] `http://localhost:5068/swagger` opens.
- [ ] Login returns a token for the Admin, Staff, PI, and Reviewer accounts.
- [ ] Sample cycles, projects, councils, and contracts are visible.

### Frontend

```powershell
cd D:\FURPMS_Software_Package\FURPMS-Web
npm ci
npm run typecheck
npm run build
```

Confirm:

- [ ] Type checking and production build succeed.
- [ ] `npm run dev` opens the login page at `http://localhost:5173`.
- [ ] Each role is redirected to its permitted dashboard/navigation.
- [ ] A forbidden route displays the unauthorized page instead of protected data.
- [ ] Proposal documents and seeded evidence links open successfully.

## 11. Troubleshooting

| Symptom | Cause | Resolution |
|---|---|---|
| API cannot connect to PostgreSQL | Container is stopped or 5433 is occupied | Run `docker compose ps`; start Docker; inspect port 5433 |
| Port 5068 is already in use | Another backend instance is running | Stop the older `dotnet`/FURPMS API process; run only one instance |
| Frontend requests the wrong server | A local env file overrides `.env.development` | Inspect `.env.local` and `.env.development.local`; set `VITE_API_BASE_URL=http://localhost:5068/api` |
| Browser reports CORS/network error | Backend is not running or FE URL is incorrect | Open Swagger first, then verify the frontend API URL |
| Demo accounts are missing | Backend is not running as `Development` or sample data was disabled | Use the `http` launch profile and a clean generated sample database |
| AI says it is not configured | Gemini API key is absent | Add ignored `appsettings.Development.json` and restart the API |
| AI returns 404 for a model | Configured model was retired | Use the model names documented in Section 5 or update them to currently available Gemini models |
| AI returns 429/503 | Provider rate limit or overload | Wait for the UI cooldown/retry; use the stored analysis; do not repeatedly click |
| Email is not received | SMTP is not configured or demo address is not real | Use in-app notifications/email logs or configure a controlled test inbox |
| Uploaded file disappears after public redeploy | Ephemeral server filesystem | Configure Cloudinary for public deployment; local Docker evaluation is unaffected |
| Document link returns 404 | Old data references a missing local file | Reset/seed the sample database or include the required sample document files |

## 12. Starting and stopping after installation

Normal start:

```powershell
# Terminal 1
cd D:\FURPMS_Software_Package\FURPMS_BEv2
docker compose up -d
dotnet run --project FURPMS.API --launch-profile http

# Terminal 2
cd D:\FURPMS_Software_Package\FURPMS-Web
npm run dev
```

Normal stop:

1. Press `Ctrl+C` in the backend and frontend terminals.
2. Preserve the database but stop its container:

```powershell
cd D:\FURPMS_Software_Package\FURPMS_BEv2
docker compose stop
```

Do not use `docker compose down -v` for normal shutdown because `-v` deletes the local database.

## 13. Submission checklist for item 3 — Software Package

- [ ] Backend source code (`FURPMS_BEv2`) is included with complete commit history in the repository.
- [ ] Frontend source code (`FURPMS-Web`) is included with complete commit history in the repository.
- [ ] EF Core migrations and idempotent sample seeders are included.
- [ ] Optional `database/furpms_sample.sql` is included if the portal requires a physical database file.
- [ ] This installation guide is copied to the package root.
- [ ] Role-based test accounts in Section 9 were tested after a clean database reset.
- [ ] `dotnet build`, `dotnet test`, `npm run typecheck`, and `npm run build` pass.
- [ ] No `node_modules`, `bin`, `obj`, `dist`, Docker volumes, or temporary upload folders are included.
- [ ] No Gemini, SMTP, Cloudinary, JWT production secret, or production database credential is included.
- [ ] Archive was extracted and installed once on a different machine or clean user profile.

## 14. Related documents

- `docs/DEMO_SCRIPT_SUBMISSION.md` — demo order, data, accounts, and presenter assignments.
- `docs/USE_CASE_STATUS_TRACEABILITY.md` — status and traceability of all registered Use Cases.
- `docs/DEMO_GUIDE.md` — detailed sample scenario descriptions.
- `docs/API_CONTRACT.md` — REST API contract.
- `docs/BUSINESS_RULES.md` — business constraints and implementation references.

