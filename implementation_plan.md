# Pokemon Dream Team Trainer System

Build a high-performance, visually stunning Pokemon Trainer portal using ASP.NET Core Web API, MS SQL Server, and Angular. The application enables trainers to register/login, search and explore Pokemons with high-performance caching, and build a "Dream Team" of up to 5 Pokemons. It also features a smart AI-powered Team Coach that analyzes the team's composition and provides interactive feedback.

---

## User Review Required

> [!IMPORTANT]
> **Key Architecture Decisions**
> 1. **Server-Side PokeAPI Proxying & Caching**: To address performance issues of PokeAPI, we will proxy all calls through our backend. We will implement a `PokemonCache` table in the database and a fast memory cache. When a Pokemon is searched or requested, the backend serves it from cache. If not present, it fetches it from PokeAPI, stores it, and returns it. This guarantees high speed and handles third-party API offline edge cases.
> 2. **AI Integration**: We will build an **AI Team Coach** panel. Once a trainer has added Pokemons to their Dream Team, they can click "Consult Coach". The system will run an analysis based on the team's types, stats, and synergy, and present it as a premium chat/feedback interface with recommendations.
> 3. **SQL Server LocalDB**: We will use SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) which is lightweight, standard for .NET development, and installs easily.
> 4. **Authentication**: Standard JWT Bearer token authentication. Unauthenticated users will be restricted from accessing API endpoints and routed to the gorgeous login/registration screen.

---

## Open Questions

> [!NOTE]
> No major blockers, but please confirm:
> - Do you have a preferred SQL Server connection string or instance name? We will default to `Server=(localdb)\MSSQLLocalDB;Database=PokemonTrainerDb;Trusted_Connection=True;TrustServerCertificate=True;`.
> - For the AI feedback, since we want this to be self-contained and run locally without requiring external paid API keys (like OpenAI), we will implement a clever rule-based C# AI Coach Engine that generates dynamic, personalized analysis based on type weaknesses, stats, and strategies, and wraps it in a natural-language-like response. If you'd like us to connect to a real LLM API (e.g., Gemini or OpenAI), please let us know.

---

## Proposed Changes

We will divide the project into two main directories: `pokemon-backend` (C# ASP.NET Core API) and `pokemon-frontend` (Angular).

### Database Schema (MS SQL)

#### `Users` Table
* `Id` (INT, Identity, PK)
* `Username` (NVARCHAR(50), Unique, Not Null)
* `PasswordHash` (NVARCHAR(255), Not Null)
* `CreatedAt` (DATETIME, Default GETDATE())

#### `DreamTeams` Table
* `Id` (INT, Identity, PK)
* `UserId` (INT, FK to Users.Id, Not Null)
* `PokemonId` (INT, Not Null)
* `PokemonName` (NVARCHAR(100), Not Null)
* `SpriteUrl` (NVARCHAR(255), Not Null)
* `Type1` (NVARCHAR(50), Not Null)
* `Type2` (NVARCHAR(50), Null)
* `SlotIndex` (INT, Not Null) -- 0 to 4 (max 5)

#### `PokemonCache` Table
* `PokemonId` (INT, PK)
* `Name` (NVARCHAR(100), Index, Not Null)
* `DetailsJson` (NVARCHAR(MAX), Not Null) -- Holds stats, abilities, sprites, types, etc.
* `LastUpdatedAt` (DATETIME, Default GETDATE())

---

### Backend Components (`pokemon-backend`)

#### [NEW] [Database Context & Migrations](file:///c:/Users/Hp/Documents/interviews/pokemon-backend/Data/AppDbContext.cs)
Defines Entity Framework Core DBContext mapping to MS SQL database.

#### [NEW] [Auth Controller & Service](file:///c:/Users/Hp/Documents/interviews/pokemon-backend/Controllers/AuthController.cs)
Handles registration and login. Generates JWT Bearer tokens for authenticated users.

#### [NEW] [Pokemon Controller & Service](file:///c:/Users/Hp/Documents/interviews/pokemon-backend/Controllers/PokemonController.cs)
Exposes endpoints to list/search Pokemons, fetch detailed information, and retrieve lists with caching. It handles the third-party PokeAPI proxying.

#### [NEW] [Team Controller & Service](file:///c:/Users/Hp/Documents/interviews/pokemon-backend/Controllers/TeamController.cs)
Manages the user's Dream Team (adds, removes, lists up to 5 members).

#### [NEW] [AI Coach Controller & Service](file:///c:/Users/Hp/Documents/interviews/pokemon-backend/Controllers/AiCoachController.cs)
Analyses the current user's Dream Team. Returns type synergy analysis, stat summaries, and advice.

---

### Frontend Components (`pokemon-frontend`)

#### [NEW] [Design System (index.css)](file:///c:/Users/Hp/Documents/interviews/pokemon-frontend/src/index.css)
A clean, premium design style with a dark theme, glassmorphic cards, glowing element colors (e.g., Red for Fire, Blue for Water, Yellow for Electric), smooth micro-animations, and modern font choices.

#### [NEW] [Authentication Screen](file:///c:/Users/Hp/Documents/interviews/pokemon-frontend/src/app/components/auth/auth.component.ts)
A gorgeous, dual-mode Register / Login card with interactive inputs, form validation, and error messages.

#### [NEW] [Dashboard & Search](file:///c:/Users/Hp/Documents/interviews/pokemon-frontend/src/app/components/dashboard/dashboard.component.ts)
The central hub containing:
* **Dream Team Grid**: A sticky top bar or panel displaying the 5 selected Pokemons in glowing frames with interactive hover/remove actions.
* **Pokemon Search/Explore Grid**: A responsive search interface with a smart filtering search bar (filter by type, stat ranges, name).
* **AI Coach Panel**: A sliding drawer or overlay presenting the AI analysis, team synergy, and fun recommendations with animations.

#### [NEW] [Pokemon Detail Dialog](file:///c:/Users/Hp/Documents/interviews/pokemon-frontend/src/app/components/pokemon-detail/pokemon-detail.component.ts)
A full-featured overlay card displaying stats, element types, abilities, moves, and a detailed profile.

---

### Edge-Case Handling

1. **SQL Server Unavailable**: The C# backend will catch connection exceptions and return a `503 Service Unavailable` with a user-friendly error payload. The Angular frontend will show a prominent, stylish floating warning: *"Database is currently offline. You can browse cached data in offline mode but cannot save changes to your team."*
2. **PokeAPI Down**: The backend will serve from `PokemonCache` for requests. If a Pokemon is not in the cache, the backend will return a placeholder fallback (classic Generation 1 starter details) and display a warning banner: *"PokeAPI is currently unreachable. Operating using cached database records."*

---

## Verification Plan

### Automated Tests
* **Backend Unit Tests**: Implement tests using `xUnit` and `Moq` for `PokemonService` caching, `TeamService` limit checking (max 5 members), and `AuthService` hash validations.
* **Frontend Component Tests**: Implement tests using Angular `Jasmine/Karma` to verify user auth form validation, search filtering, and the maximum-member guard (5 slots).

### Manual Verification
* Run the API backend on a port (e.g., `5000/5001` or `http://localhost:5072`).
* Run the Angular Dev server on `http://localhost:4200`.
* Test end-to-end paths:
  1. Register a new trainer and log in.
  2. Verify pages are inaccessible before login (route guard check).
  3. Search for Pokemon (verify speed after first cache load).
  4. Build a team of 5 Pokemons. Verify that adding a 6th is prevented.
  5. Consult the AI Coach and review the feedback.
  6. Stop MS SQL service temporarily and verify offline banner.
