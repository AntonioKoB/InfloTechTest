# User Management Technical Exercise

The exercise is an ASP.NET Core web application backed by Entity Framework Core, which faciliates management of some fictional users.
We recommend that you use [Visual Studio (Community Edition)](https://visualstudio.microsoft.com/downloads) or [Visual Studio Code](https://code.visualstudio.com/Download) to run and modify the application. 

**The UI has been re-implemented in Blazor talking to a REST API, so the solution now runs as two applications, and it uses SQL Server via Entity Framework Core migrations. See the [Documentation](#documentation) section below for the solution layout, how to run it, and how to set up a database to run it against.**

## The Exercise
Complete as many of the tasks below as you feel comfortable with. These are split into 4 levels of difficulty 
* **Standard** - Functionality that is common when working as a web developer
* **Advanced** - Slightly more technical tasks and problem solving
* **Expert** - Tasks with a higher level of problem solving and architecture needed
* **Platform** - Tasks with a focus on infrastructure and scaleability, rather than application development.

### 1. Filters Section (Standard)

The users page contains 3 buttons below the user listing - **Show All**, **Active Only** and **Non Active**. Show All has already been implemented. Implement the remaining buttons using the following logic:
* Active Only – This should show only users where their `IsActive` property is set to `true`
* Non Active – This should show only users where their `IsActive` property is set to `false`

### 2. User Model Properties (Standard)

Add a new property to the `User` class in the system called `DateOfBirth` which is to be used and displayed in relevant sections of the app.

### 3. Actions Section (Standard)

Create the code and UI flows for the following actions
* **Add** – A screen that allows you to create a new user and return to the list
* **View** - A screen that displays the information about a user
* **Edit** – A screen that allows you to edit a selected user from the list  
* **Delete** – A screen that allows you to delete a selected user from the list

Each of these screens should contain appropriate data validation, which is communicated to the end user.

### 4. Data Logging (Advanced)

Extend the system to capture log information regarding primary actions performed on each user in the app.
* In the **View** screen there should be a list of all actions that have been performed against that user. 
* There should be a new **Logs** page, containing a list of log entries across the application.
* In the Logs page, the user should be able to click into each entry to see more detail about it.
* In the Logs page, think about how you can provide a good user experience - even when there are many log entries.

### 5. Extend the Application (Expert)

Make a significant architectural change that improves the application.
Structurally, the user management application is very simple, and there are many ways it can be made more maintainable, scalable or testable.
Some ideas are:
* Re-implement the UI using a client side framework connecting to an API. Use of Blazor is preferred, but if you are more familiar with other frameworks, feel free to use them.
* Update the data access layer to support asynchronous operations.
* Implement authentication and login based on the users being stored.
* Implement bundling of static assets.
* Update the data access layer to use a real database, and implement database schema migrations.

### 6. Future-Proof the Application (Platform)

Add additional layers to the application that will ensure that it is scaleable with many users or developers. For example:
* Add CI pipelines to run tests and build the application.
* Add CD pipelines to deploy the application to cloud infrastructure.
* Add IaC to support easy deployment to new environments.
* Introduce a message bus and/or worker to handle long-running operations.

## Additional Notes

* Please feel free to change or refactor any code that has been supplied within the solution and think about clean maintainable code and architecture when extending the project.
* If any additional packages, tools or setup are required to run your completed version, please document these thoroughly.

# Documentation

## Solution layout

| Project | Purpose |
|---|---|
| `UserManagement.Data` | EF Core `DataContext`, entities and migrations. |
| `UserManagement.Services` | Domain services (`IUserService`, `IUserLogService`, audit-log decorator, diff builder). |
| `UserManagement.Api` | REST API over the domain services. Owns all database access. |
| `UserManagement.Api.Contracts` | DTOs and enums making up the API's public contract. Referenced by both the API and its clients, and deliberately free of any ASP.NET Core dependency. |
| `UserManagement.Blazor` | Blazor Server UI. Talks to the API over HTTP and never touches the service or data layers directly. |

Each has a matching `*.Tests` project, except `UserManagement.Api.Contracts`, which is DTOs only.

## Running the application

The API and the UI are two separate applications and both need to be running. The API must be reachable at the URL configured in `UserManagement.Blazor/appsettings.json` (`Api:BaseUrl`, `https://localhost:7085` by default).

From two terminals, after completing the [Database](#database) setup and adding the [signing key](#setup-the-signing-key):

```bash
dotnet run --project UserManagement.Api
```

```bash
dotnet run --project UserManagement.Blazor
```

Then browse to `https://localhost:7086`. In Visual Studio, set both projects as startup projects instead.

| Application | URL |
|---|---|
| Blazor UI | `https://localhost:7086` |
| API | `https://localhost:7085` |
| API reference (Development only) | `https://localhost:7085/scalar` |

## Database

The application uses SQL Server (via `Microsoft.EntityFrameworkCore.SqlServer`) and EF Core migrations, instead of the original in-memory provider. The schema and seed data are defined by the migrations checked into `UserManagement.Data/Migrations`, and are applied automatically on startup - you just need a database to point at and a connection string, no manual `dotnet ef` commands required to run the app.

### 1. Prerequisite: a SQL Server instance

Any edition works - a full local SQL Server install, SQL Server Express, or LocalDB. This was developed and verified against a local named instance.

### 2. Create the login, user and database

The application connects with SQL Server authentication, so a login/user needs to exist before the app can create the database. `Database.Migrate()` (run automatically on startup) creates the database and schema itself, but the server-level login/user is a one-time setup step outside EF's remit. Run the following against your instance (e.g. via SQL Server Management Studio or `sqlcmd`), adjusting the password:

```sql
CREATE LOGIN InfloDBUser WITH PASSWORD = 'Your-Strong-Password-Here';
GO
CREATE DATABASE InfloUsersDB;
GO
USE InfloUsersDB;
CREATE USER InfloDBUser FOR LOGIN InfloDBUser;
ALTER ROLE db_owner ADD MEMBER InfloDBUser;
GO
```

### 3. Configure the connection string (local development)

The connection string is never committed to source control. Locally, it's configured via [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), which stores it outside the repository entirely (`%APPDATA%\Microsoft\UserSecrets` on Windows), so there's no risk of accidentally committing real credentials. The API owns all database access, so the connection string belongs to it. From `UserManagement.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=InfloUsersDB;User Id=InfloDBUser;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

Replace `YOUR_SERVER` with your instance (e.g. `localhost`, `localhost\SQLEXPRESS`, or `(localdb)\MSSQLLocalDB`) and `YOUR_PASSWORD` with the password chosen in step 2. `TrustServerCertificate=True` avoids a TLS-certificate error against a local instance that isn't using a trusted certificate.

Once set, start the API (see [Running the application](#running-the-application)) - it applies any pending migrations automatically on startup and seeds the same 11 users the in-memory version used to.

### 4. Adding future migrations

If the `User`/`UserLog` model changes, generate a new migration from the repository root (requires the `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add <MigrationName> --project UserManagement.Data
```

No `--startup-project` is needed: `UserManagement.Data` carries the EF Core design-time package and a design-time factory (`DataContextFactory`), so the tooling builds the model from the data project alone and the API's startup code - including its `Database.Migrate()` call - never runs during scaffolding.

Commit the generated files under `UserManagement.Data/Migrations` - they're applied automatically the next time the app starts, no separate `database update` step needed.

### 5. Production (Azure)

Azure SQL is the intended production target - it's the same `Microsoft.EntityFrameworkCore.SqlServer` provider, so only the connection string changes, not the code. .NET User Secrets is a local-development-only mechanism (it's only loaded when `ASPNETCORE_ENVIRONMENT=Development`), so it plays no role in production. In Azure App Service, the equivalent is setting each value as an App Service Configuration entry - these surface to the app as environment variables, which ASP.NET Core's configuration system already reads automatically, so no code change is required. For stronger secret management (centralized rotation, RBAC-audited access) Azure Key Vault with a Managed Identity is a natural next step once a real deployment pipeline exists to attach it to.

Everything each app needs beyond its committed `appsettings.json`:

| App | Setting | Local | Azure App Service |
|---|---|---|---|
| `UserManagement.Api` | `ConnectionStrings:DefaultConnection` | user secrets | connection string `DefaultConnection` (SQL Server) |
| `UserManagement.Api` | `Jwt:SigningKey` (32+ random bytes) | user secrets | app setting `Jwt__SigningKey` |
| `UserManagement.Blazor` | `Api:BaseUrl` (not a secret) | `appsettings.json` | app setting `Api__BaseUrl`, the deployed API's URL |

The Blazor app's authentication cookie is protected with ASP.NET Core Data Protection keys. App Service keeps them per site by default; if the app is ever scaled out or swapped between slots, the key ring must be shared (Azure Blob storage plus Key Vault) so every instance accepts the others' cookies.

## UI architecture (Blazor)

The original `UserManagement.Web` MVC application has been removed. Users and Logs were its entire surface area, so once both moved to Blazor there was nothing left for it to serve, and leaving an empty shell project behind would only have added a second thing to run and maintain.

### A new project rather than converting the MVC one

The Blazor UI is a new project rather than an in-place conversion of `UserManagement.Web`. Converting would have meant stripping out MVC controllers, views, view models and the bundling middleware while simultaneously adding Blazor's hosting model to the same project - a single tangled changeset where additions and removals are hard to review separately. As two distinct operations, "add the new app" and "delete the old one" each stand on their own and read clearly in isolation.

### Blazor Server rather than WebAssembly

The UI runs on Blazor Server. Both hosting models share the same component model, so the choice comes down to where component code executes and what that implies:

- **Security surface.** The API token never reaches browser script: it lives inside an encrypted, HttpOnly authentication cookie issued by the Blazor host and is attached to API calls on the server (see [Authentication](#authentication)). A WebAssembly client must hold its token in browser-accessible storage, which makes XSS a token-theft risk and is why production SPAs frequently front themselves with a backend-for-frontend to avoid exactly that.
- **No CORS.** Component code runs server-side, so calls to the API are ordinary server-to-server requests with no browser origin involved. CORS is a browser-enforced mechanism and simply doesn't apply.
- **No payload download.** There is no .NET runtime to download before first render.

The trade-off accepted in exchange is that the UI needs a live SignalR connection, and interactions cost a server round trip.

Note that the API boundary is real regardless of this choice: the Blazor app depends only on `UserManagement.Api.Contracts` and reaches everything else over HTTP. Swapping to WebAssembly later would not require changing the API, only adding CORS and moving token storage.

### Component libraries and packages

- **[MudBlazor](https://mudblazor.com/)** for UI components (cards, grid, buttons, snackbar toasts). Chosen over hand-written CSS for a component set that looks consistent without bespoke styling work.
- **[Refit](https://github.com/reactiveui/refit)** for the API client. The `IUsersApi`/`ILogsApi` interfaces declare the endpoints and Refit generates the implementations, so there is no hand-rolled `HttpClient` plumbing and the client mirrors the API contract directly. Note that Refit's runtime reflection-based client generation now lives in the separate `Refit.Reflection` package, which is referenced explicitly.
- **[Markdig](https://github.com/xoofx/markdig)** to render this README into the Requirements panel on the home page.
- **[bUnit](https://bunit.dev/)** for component tests.

A few styles that apply to markup rendered by MudBlazor or the built-in input components live in `wwwroot/app.css` rather than in component-scoped `.razor.css` files. Blazor's CSS isolation only tags elements written directly in a component's own markup, so scoped rules never reach elements a child component renders.

### Credentials

Every user has a password, stored only as a hash (`User.PasswordHash`) produced by ASP.NET Core's own `PasswordHasher<TUser>` (PBKDF2 with a per-password salt, from the `Microsoft.Extensions.Identity.Core` package - just the hashing primitive, not the Identity framework's user store, registration or lockout machinery). The clear-text password exists only in the create/update request to the API: it is never stored, never returned in a `UserDto`, and never written to the audit log - `PasswordHash` is `[JsonIgnore]`d, so the before/after snapshots and the diff never contain it (a password change shows as an "Updated" entry with no field changes).

- **Add user** requires a password. **Edit user** has an optional "New password" field - leave it blank to keep the current one.
- **Seeded users** all share the password `12345`. The `AddUserPasswordHash` migration sets it on the existing seed rows, so any of them can be used to sign in once login is in place. A user created before that migration has no credential (`NULL`) until one is set through Edit.
- `ICredentialService` in the Services layer owns hashing and verification. `AuthenticateAsync` returns the user only for a matching email and password on an active user with a credential set; an unknown email, wrong password, inactive user or missing credential all yield `null`.

### Authentication

Login is "based on the users being stored": any active user signs in with their email and password (for the seeded users, any seeded email with the password `12345`, see [Credentials](#credentials)). Every page in the Blazor app, and every API endpoint except login, requires a signed-in user.

#### Setup: the signing key

The API signs and validates its tokens with a symmetric key that must be at least 32 bytes long. Like the connection string it is never committed, and the API fails at startup with an explanatory message if it is missing. Locally it goes in the API's user secrets, either from the `UserManagement.Api` folder:

```bash
dotnet user-secrets set "Jwt:SigningKey" "<a random string of at least 32 characters>"
```

or in Visual Studio: right-click `UserManagement.Api`, **Manage User Secrets**, and add the entry alongside the connection string:

```json
{
  "ConnectionStrings:DefaultConnection": "...",
  "Jwt:SigningKey": "<a random string of at least 32 characters>"
}
```

Any random value works (for example the output of `openssl rand -base64 48`). Rotating it invalidates every issued token, so users just sign in again. Issuer, audience and token lifetime are ordinary settings in the `Jwt` section of `appsettings.json`.

#### How it works

1. The sign-in page posts its form to the Blazor host (`POST /login`), a genuine HTTP form post carrying an antiforgery token.
2. The Blazor host calls the API's `POST /api/auth/login` with the credentials. The API verifies the password hash through `ICredentialService` and issues a signed JWT (HS256, 60 minutes by default) carrying the user's id, email and name.
3. The Blazor host signs the browser in with an encrypted, HttpOnly authentication cookie whose principal carries the JWT as a claim. The token never reaches browser script, and the cookie expires when the token does.
4. Every API call the Blazor app makes goes through `BearerTokenHandler`, which attaches the token as an `Authorization: Bearer` header. A 401 from the API forces a full reload of the sign-in page.
5. `POST /logout`, also antiforgery-protected, clears the cookie.

On the API, `UsersController` and `LogsController` carry `[Authorize]` and `AuthController.Login` carries `[AllowAnonymous]`; validation is the standard JWT bearer scheme. The API is bearer-only and sets no cookies, so cross-site request forgery does not apply to it. The antiforgery validation lives on the Blazor host's two form posts, the only requests that change the browser's sign-in state; a post without the token is rejected with a 400.

#### Calling the API directly

`POST /api/auth/login` with `{ "email": "...", "password": "..." }` returns the token, its expiry and the user's display name; send the token as `Authorization: Bearer <token>` on every other request.

## Static assets

`LigerShark.WebOptimizer.Core` bundled and minified the MVC application's Bootstrap and jQuery assets, and was removed along with that project. The Blazor app does not need an equivalent: its build pipeline already fingerprints and compresses static web assets, MudBlazor ships a single pre-minified CSS and JS file each, and Blazor's CSS isolation bundles all component-scoped styles into one generated stylesheet. There is no separate bundling step to configure.

## API client resiliency (Blazor)

The Blazor app's calls to `UserManagement.Api` (via the `IUsersApi`/`ILogsApi` Refit clients) go through [`Microsoft.Extensions.Http.Resilience`](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) - Microsoft's own resilience package, built on [Polly](https://github.com/App-vNext/Polly) v8. It's wired in `UserManagement.Blazor/Program.cs` via `.AddStandardResilienceHandler()` on each `HttpClient`, which bundles retry (with exponential backoff), a per-attempt and total-request timeout, a circuit breaker, and a concurrency rate limiter in one call, rather than hand-wiring individual Polly policies.
