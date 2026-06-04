# Project Walkthrough: Pokemon Trainer Portal & Dream Team Manager

We have successfully built, verified, and deployed a full-stack, high-performance Pokemon Trainer portal using ASP.NET Core Web API, MS SQL / SQLite / PostgreSQL database providers, and a modern Angular frontend.

The project is tracked in the private GitHub repository [AlonHrtmn/pockemon_game_asignment](https://github.com/AlonHrtmn/pockemon_game_asignment) and has been successfully pushed.

---

## 1. Accomplishments & Architecture

### Backend (`pokemon-backend`)
- **JWT Authentication**: Secured registration and login endpoints. Password hashes are generated and verified timing-attack-safely via PBKDF2 (`Rfc2898DeriveBytes`).
- **Resilient Pokemon Caching Service**: 
  - Resolves performance latency issues of `pokeapi.co` by proxying requests.
  - Queries local database cache first. If cache is missing, fetches from PokeAPI, stores it locally, and serves.
  - **Offline/API-down Fallback**: If PokeAPI is down or unreachable, a deterministic seed-based Mock Generator generates full stats, element types, and sets sprites to load artwork from Github's raw assets CDN.
- **Dream Team management**: Implemented team persistence for up to 5 members. Added guards to prevent duplicate Pokemons and handles active slot replacements cleanly.
- **AI Coach Synergy Analyzer**: Evaluates element type coverage, calculates stat averages, determines team archetype (Hyper Offense, Speed Blitzers, Iron Fortress, or Balanced), lists specific type resistances/weaknesses, and generates tactical suggestions.
- **Flexible DB Engine**: Easily switches between SQLite (for out-of-the-box portable testing) and MS SQL Server/PostgreSQL by toggling `"DatabaseProvider"` in `appsettings.json`.

### Frontend (`pokemon-frontend`)
- **Stand-alone Architecture**: Utilizes modern Angular features (Standalone components, reactive Signals, and Functional Route Guards/Interceptors).
- **Stunning Design System**: Crafted a customized dark futuristic HUD aesthetic featuring:
  - Floating animated neon glow orbs in the background.
  - Glassmorphic login and dashboard cards.
  - Glowing team slot selectors showing empty silhouettes or detailed Pokemon art.
  - Color-coded element badges matching Pokemon elements (Red for Fire, Blue for Water, Yellow for Electric, etc.).
  - Stat bar overlays inside detail modal sheets, displaying values dynamically.
  - Slidable AI Coach Drawer presenting tactical suggestions.

---

## 2. Test Verification

We implemented test suites on both the backend and frontend to guarantee system robustness.

### Backend Unit Tests (`pokemon-backend-tests`)
- **Framework**: `xUnit` + `Moq` + Entity Framework Core `InMemory` Database.
- **Coverage (20 Tests Passed)**:
  - **AuthService**: Registering unique users, blocking duplicate usernames, verifying hashed passwords, generating JWTs, and validating invalid credentials.
  - **TeamService**: Throwing `ArgumentException` on out-of-bound slots, replacing occupied slots, preventing duplicate Pokemons, moving duplicates to target slots, and clearing teams.
- **Execution Command**:
  ```powershell
  cd pokemon-backend-tests
  dotnet test
  ```
- **Result**:
  ```text
  Passed!  - Failed: 0, Passed: 20, Skipped: 0, Total: 20, Duration: 373 ms
  ```

### Frontend Unit Tests (`pokemon-frontend`)
- **Framework**: `Angular Testing Library` + `Vitest` (Angular's default unit test runner).
- **Coverage (7 Tests Passed)**:
  - **Root App**: Verification of router-outlet container presence.
  - **AuthService**: Correctly handles login token/username storage in `localStorage` and logs out by clearing local data.
  - **AuthGuard**: Restricts access to unauthorized pages and redirects users to `/auth`.
- **Execution Command**:
  ```powershell
  cd pokemon-frontend
  npm run test
  ```
- **Result**:
  ```text
  Test Files  3 passed (3)
       Tests  7 passed (7)
    Duration  3.13s
  ```

---

## 3. Supabase Cloud Integration & Verification

We successfully configured and connected the C# backend to the hosted Supabase PostgreSQL cloud database instance (`kfcdauuhkllczabnpzta` in `ap-northeast-1` region).

### Connection Resolution & Setup
- **Subdomain Correction**: Identified that the project's connection pooler is on the `aws-1` subdomain (`aws-1-ap-northeast-1.pooler.supabase.com`) rather than `aws-0`.
- **Session Mode Connection**: Configured connection to the Session Pooler on port `5432` which seamlessly supports EF Core prepared statements without extra configuration.
- **Database Initializer**: EF Core's `EnsureCreated()` successfully resolved DNS, established a secure connection to the Supabase pooler, checked the schema, and verified database tables.

---

## 4. How to Run Locally

### 1. Run the Backend API
1. Open a terminal in `pokemon-backend`.
2. Run the application:
  ```bash
  dotnet run
  ```
  *(Or using the explicit path: `C:\Users\Hp\.dotnet\dotnet.exe run`)*
3. The API will start on: `http://localhost:5088` (Swagger UI available at `http://localhost:5088/swagger`).
   > [!NOTE]
   > The app is currently configured to connect to your remote Supabase PostgreSQL database cloud instance. To toggle back to offline SQLite, change `"DatabaseProvider"` to `"Sqlite"` in `appsettings.json`.

### 2. Run the Frontend Client
1. Open a terminal in `pokemon-frontend`.
2. Run the development server:
  ```bash
  npm start
  ```
3. Open your browser and navigate to: `http://localhost:4200`.
4. Register a trainer account, enter the portal, and start assembling your ultimate dream team!

---

## 5. Live Cloud Deployments

We have deployed the entire full-stack system live to the cloud. You can access it from any device:

* **Live Frontend UI (Vercel)**: [https://pokemon-trainer-portal.vercel.app/](https://pokemon-trainer-portal.vercel.app/)
* **Live Backend API (Render)**: [https://pokemon-trainer-api.onrender.com](https://pokemon-trainer-api.onrender.com)
  *(Swagger document available at: `https://pokemon-trainer-api.onrender.com/swagger/index.html`)*
* **Database (Supabase)**: Hosted on the `aws-1-ap-northeast-1` cluster in Tokyo.

### End-to-End Cloud Verification
We verified the production cloud application successfully:
1. Created a new trainer account named **`cloudtrainer2`** directly on the Vercel site.
2. The registration resolved correctly with CORS permission and automatically routed the session to the dashboard.
3. Searched the Pokémon database for `Charmander` and added it to **Slot 2** of the Dream Team.
4. Confirmed the selection saved securely to the Supabase cloud instance.
