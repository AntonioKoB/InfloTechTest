# User Management Technical Exercise

[![CI](https://github.com/AntonioKoB/InfloTechTest/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/AntonioKoB/InfloTechTest/actions/workflows/ci.yml) [![CD](https://github.com/AntonioKoB/InfloTechTest/actions/workflows/cd.yml/badge.svg?branch=main)](https://github.com/AntonioKoB/InfloTechTest/actions/workflows/cd.yml)

The exercise is an ASP.NET Core web application backed by Entity Framework Core, which faciliates management of some fictional users.
We recommend that you use [Visual Studio (Community Edition)](https://visualstudio.microsoft.com/downloads) or [Visual Studio Code](https://code.visualstudio.com/Download) to run and modify the application. 

**The UI has been re-implemented in Blazor talking to a REST API, so the solution now runs as two applications, and it uses SQL Server via Entity Framework Core migrations. See the [Documentation](#documentation) section below for the solution layout, how to run it, and how to set up a database to run it against. A development environment is deployed to Azure on every push to `main`: [app-inflo-blazor-dev-p5pupochhqltc.azurewebsites.net](https://app-inflo-blazor-dev-p5pupochhqltc.azurewebsites.net) (see [Live environment](#live-environment)).**

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
| `UserManagement.Services` | Domain services (`IUserService`, `IUserLogService`, audit-log decorator, diff builder), the commands and their handlers, the message bus and the command status store. |
| `UserManagement.Api` | REST API over the domain services, and the worker that executes commands. Owns all database access. |
| `UserManagement.Api.Contracts` | DTOs and enums making up the API's public contract. Referenced by both the API and its clients, with no ASP.NET Core dependency. |
| `UserManagement.Blazor` | Blazor Server UI. Talks to the API over HTTP and does not reference the service or data layers. |

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

No `--startup-project` is needed: `UserManagement.Data` carries the EF Core design-time package and a design-time factory (`DataContextFactory`), so the tooling builds the model from the data project alone, and the API's startup code, including `Database.Migrate()`, does not run during scaffolding.

Commit the generated files under `UserManagement.Data/Migrations` - they're applied automatically the next time the app starts, no separate `database update` step needed.

### 5. Production (Azure)

Azure SQL is the intended production target - it's the same `Microsoft.EntityFrameworkCore.SqlServer` provider, so only the connection string changes, not the code. .NET User Secrets is a local-development-only mechanism (it's only loaded when `ASPNETCORE_ENVIRONMENT=Development`), so it plays no role in production. In Azure App Service, the equivalent is setting each value as an App Service Configuration entry - these surface to the app as environment variables, which ASP.NET Core's configuration system already reads automatically, so no code change is required. For stronger secret management (centralized rotation, RBAC-audited access) Azure Key Vault with a Managed Identity is a natural next step once a real deployment pipeline exists to attach it to. The [Resiliency](#resiliency) section covers the two things an App Service deployment leans on beyond configuration: transient-fault retries against Azure SQL, and the `/health` endpoints for the platform's health probes.

Everything each app needs beyond its committed `appsettings.json`:

| App | Setting | Local | Azure App Service |
|---|---|---|---|
| `UserManagement.Api` | `ConnectionStrings:DefaultConnection` | user secrets | connection string `DefaultConnection` (SQL Server) |
| `UserManagement.Api` | `Jwt:SigningKey` (32+ random bytes) | user secrets | app setting `Jwt__SigningKey` |
| `UserManagement.Blazor` | `Api:BaseUrl` (not a secret) | `appsettings.json` | app setting `Api__BaseUrl`, the deployed API's URL |

The Blazor app's authentication cookie is protected with ASP.NET Core Data Protection keys. App Service keeps them per site by default; if the app is ever scaled out or swapped between slots, the key ring must be shared (Azure Blob storage plus Key Vault) so every instance accepts the others' cookies.

## UI architecture (Blazor)

The original `UserManagement.Web` MVC application has been removed. Users and Logs were its entire surface area, so once both moved to Blazor there was nothing left for it to serve, and leaving an empty shell project behind would only have added a second thing to run and maintain.

### Replacing the MVC project

The Blazor UI is a new project; `UserManagement.Web` was not converted in place. Converting would have meant stripping out MVC controllers, views, view models and the bundling middleware while simultaneously adding Blazor's hosting model to the same project - a single tangled changeset where additions and removals are hard to review separately. As two distinct operations, "add the new app" and "delete the old one" each stand on their own and read clearly in isolation.

### Hosting model: Blazor Server

The UI runs on Blazor Server. Both hosting models share the same component model, so the choice comes down to where component code executes and what that implies:

- **Security surface.** The API token never reaches browser script: it lives inside an encrypted, HttpOnly authentication cookie issued by the Blazor host and is attached to API calls on the server (see [Authentication](#authentication)). A WebAssembly client must hold its token in browser-accessible storage, which makes XSS a token-theft risk; production SPAs often add a backend-for-frontend for that reason.
- **No CORS.** Component code runs server-side, so calls to the API are ordinary server-to-server requests with no browser origin involved. CORS is a browser-enforced mechanism and simply doesn't apply.
- **No payload download.** There is no .NET runtime to download before first render.

The trade-off accepted in exchange is that the UI needs a live SignalR connection, and interactions cost a server round trip.

Note that the API boundary is real regardless of this choice: the Blazor app depends only on `UserManagement.Api.Contracts` and reaches everything else over HTTP. Swapping to WebAssembly later would not require changing the API, only adding CORS and moving token storage.

### Component libraries and packages

- **[MudBlazor](https://mudblazor.com/)** for UI components (cards, grid, buttons, snackbar toasts). Chosen over hand-written CSS for a component set that looks consistent without bespoke styling work.
- **[Refit](https://github.com/reactiveui/refit)** for the API client. The `IUsersApi`/`ILogsApi` interfaces declare the endpoints and Refit generates the implementations, so there is no hand-rolled `HttpClient` plumbing and the client mirrors the API contract directly. Note that Refit's runtime reflection-based client generation now lives in the separate `Refit.Reflection` package, which is referenced explicitly.
- **[Markdig](https://github.com/xoofx/markdig)** to render this README into the Requirements panel on the home page.
- **[bUnit](https://bunit.dev/)** for component tests.

A few styles that apply to markup rendered by MudBlazor or the built-in input components live in `wwwroot/app.css`, not in component-scoped `.razor.css` files. Blazor's CSS isolation only tags elements written directly in a component's own markup, so scoped rules do not reach elements a child component renders.

### Credentials

Every user has a password, stored only as a hash (`User.PasswordHash`) produced by ASP.NET Core's own `PasswordHasher<TUser>` (PBKDF2 with a per-password salt, from the `Microsoft.Extensions.Identity.Core` package - just the hashing primitive, not the Identity framework's user store, registration or lockout machinery). The clear-text password exists only in the create/update request to the API: it is never stored, never returned in a `UserDto`, and never written to the audit log - `PasswordHash` is `[JsonIgnore]`d, so the before/after snapshots and the diff never contain it (a password change shows as an "Updated" entry with no field changes).

- **Add user** requires a password. **Edit user** has an optional "New password" field - leave it blank to keep the current one.
- **Seeded users** all share the password `12345`. The seed rows are defined in [`UserManagement.Data/DataContext.cs`](UserManagement.Data/DataContext.cs) (`HasData` in `OnModelCreating`), and the `AddUserPasswordHash` migration sets the password on them, so any of them can be used to sign in. A user created before that migration has no credential (`NULL`) until one is set through Edit.
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
5. `POST /logout`, also antiforgery-protected, tells the API the session has ended (`POST /api/auth/logout`, bearer-authenticated) and clears the cookie.

Signing in and signing out are recorded in the audit log as `LoggedIn` and `LoggedOut` entries against the user, alongside the existing Created/Viewed/Updated/Deleted actions. They mark session boundaries, so they carry no snapshot and show no diff. Failed sign-in attempts are not recorded: there is no verified user to attribute them to.

On the API, `UsersController` and `LogsController` carry `[Authorize]` and `AuthController.Login` carries `[AllowAnonymous]`; validation is the standard JWT bearer scheme. The API is bearer-only and sets no cookies, so cross-site request forgery does not apply to it. The antiforgery validation lives on the Blazor host's two form posts, the only requests that change the browser's sign-in state; a post without the token is rejected with a 400.

#### Calling the API directly

`POST /api/auth/login` with `{ "email": "...", "password": "..." }` returns the token, its expiry and the user's display name; send the token as `Authorization: Bearer <token>` on every other request.

#### Next steps

Left out to keep the scope to what the exercise asks for. Each is a small, self-contained change:

- **Token revocation.** Tokens are stateless and stay valid until they expire. `ICredentialService.SignOutAsync` is the hook where a revocation list (or a shorter lifetime plus refresh tokens) would go; today it only records the audit entry.
- **Hash upgrades on sign-in.** `PasswordHasher` reports `SuccessRehashNeeded` when a stored hash uses an older format or work factor; the result is accepted, but the hash is not rewritten.
- **A "Password changed" audit label.** A password-only edit is recorded as an Updated entry with an empty diff, because the hash is never part of the snapshots. A dedicated action would make it readable in the log.
- **An OpenAPI bearer security scheme**, so Scalar's "try it" can attach a token instead of only documenting the endpoints.
- **A password policy.** The only rule is that a password is required; the seeded accounts use `12345` by design.

## Static assets

`LigerShark.WebOptimizer.Core` bundled and minified the MVC application's Bootstrap and jQuery assets, and was removed along with that project. The Blazor app does not need an equivalent. `app.MapStaticAssets()` in `UserManagement.Blazor/Program.cs` serves the static web assets the build has already fingerprinted and precompressed, and `App.razor` references them through `@Assets["..."]`, so each URL carries its content hash and can be cached indefinitely. MudBlazor ships a single pre-minified CSS and JS file each, and Blazor's CSS isolation bundles every component-scoped `.razor.css` into one generated `UserManagement.Blazor.styles.css`. The only hand-written stylesheet, `wwwroot/app.css`, is served compressed but not minified - it is one small file, so a minifier would be a build step for nothing. There is no separate bundling step to configure.

## Resiliency

### API client (Blazor)

The Blazor app's calls to `UserManagement.Api` (via the `IAuthApi`, `IUsersApi` and `ILogsApi` Refit clients) go through [`Microsoft.Extensions.Http.Resilience`](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) - Microsoft's own resilience package, built on [Polly](https://github.com/App-vNext/Polly) v8. It's wired in `UserManagement.Blazor/Program.cs` via `.AddStandardResilienceHandler()` on each `HttpClient`, which bundles retry (with exponential backoff), a per-attempt and total-request timeout, a circuit breaker, and a concurrency rate limiter in one call, so no individual Polly policies are hand-wired.

### API to database

The API's SQL Server provider is registered with `EnableRetryOnFailure` (`AddDataAccess`, in `UserManagement.Data`), which runs every query and `SaveChanges` under EF Core's retrying execution strategy. It retries only the error numbers the provider classifies as transient - the throttling, failover and dropped-connection faults a hosted database such as Azure SQL is documented to raise - and not a genuine failure such as a constraint violation. The budget is three retries with an exponential backoff capped at five seconds, roughly nine seconds worst case, chosen to sit inside the Blazor client's ten-second per-attempt timeout: the client already retries, so the API giving up quickly on a request avoids both layers retrying on top of each other. The startup `Database.Migrate()` call runs under the same strategy, so a database that is still waking up when the API starts is retried and the process does not crash. Nothing in the data layer opens a user-initiated transaction, so no `CreateExecutionStrategy().ExecuteAsync(...)` wrapping is required.

There is no circuit breaker between the API and the database. A breaker earns its place where there is somewhere else to send the traffic, or a cheaper failure to fall back to - the Blazor app's breaker fails fast to an error message instead of hanging a circuit. In front of the only database there is no fallback: a breaker would turn a slow database into a hard outage for the duration of the break, which is worse than a bounded retry.

For Azure SQL specifically, EF Core also offers `UseAzureSql()` in place of `UseSqlServer()`; it supersedes the now-obsolete `UseAzureSqlDefaults`, whose defaults are documented as "including retries on errors". Switching is a one-line change in `AddDataAccess` once the deployment target is confirmed as Azure SQL.

### Health checks

Both hosts expose `GET /health` for the platform's health probes (App Service's Health check feature, a load balancer, a container orchestrator). The endpoints are anonymous, since probes carry no token, and return plain text: `Healthy` with a 200, or `Unhealthy` with a 503.

- `UserManagement.Api` (`https://localhost:7085/health` locally) includes a database connectivity check (`AddDbContextCheck<DataContext>`), so a process that is up but cannot reach its database reports as down and can be taken out of rotation.
- `UserManagement.Blazor` (`https://localhost:7086/health` locally) is a liveness check only. The host has no database of its own; its dependency on the API is already covered by the API's probe and by the client-side circuit breaker above.

In App Service, set each site's Health check path to `/health`.

## Caching

Two reads are cached, each with the cache that matches the kind of read it is. A read with no side effect is cached at the HTTP layer, the cheapest place to serve it from. A read that is audited is cached beneath the audit, in the service layer, so a hit is still recorded. Everything else goes to the database every time.

### The users list: output caching

`GET /api/users` is served through ASP.NET Core output caching under a named policy (`OutputCachingExtensions`, in `UserManagement.Api/Caching`). Each `filter` value is its own cached response, every entry is tagged `users`, and the worker evicts the tag the moment a create, update or delete command completes (see [Message bus and worker](#message-bus-and-worker)), so an edit that moves a user between the Active and Non-active lists is correct as soon as the command is. A command that failed (a duplicate email, a user that vanished) changed nothing and leaves the cache alone. A five-minute expiry backs the eviction, so anything that writes to the database around the API self-heals, and the store's size limit bounds the memory used.

Output caching refuses to cache any request that carries an `Authorization` header, and refuses again after the response when the user turned out to be authenticated - the safe assumption that an authenticated response is personal. The users list is the same for every signed-in caller, so `CacheAuthenticatedRequestsPolicy`, appended to that one policy, opts it back in under the framework's other rules (a GET, a 200, no cookie). Nothing else in the API is output-cached, and `UseOutputCache` sits after authorization, so an unauthenticated caller still gets a 401; no cached body is served.

### A single user: a service-layer cache

The single-user read is audited: expanding a row in the UI calls `GET /api/users/{id}?recordAsViewed=true`, and the "Viewed" entry is written by the auditing decorator around `IUserService`. An HTTP cache cannot serve that request without skipping the audit, so its cache is `CachingUserService`, a second decorator placed *beneath* the auditing one: `Auditing(Caching(UserService))`. A hit is served from memory and still recorded as a view; the composition test in `ServiceCollectionExtensionsTests` pins that order.

The decorator caches a user by id for five minutes and hands out copies, because the API's update flow edits the fetched user in place before saving it. Update and Delete invalidate that user whether or not they succeed - a failed save can mean the row changed or vanished underneath, and the cost is one extra read. Misses are not remembered, since an id that does not exist now may after the next create. The lists (cached above), the email lookup (uniqueness checks and sign-in must see the database) and the logs (append-only, changing on every audited action) pass straight through.

The decorator depends on `ICache`, a three-method contract (`GetAsync`, `SetAsync` with a time-to-live, `RemoveAsync`) implemented by `MemoryCacheAdapter` over the framework's in-process `IMemoryCache`. Swapping the store means one new adapter and one registration line.

### Two invalidation paths

An update or a delete therefore invalidates in two places: the worker evicts the list tag when the command completes, and the decorator removes the user's key as the handler writes through it. Each cache is invalidated by the layer that owns it; the boundary between them is the rule at the top of this section. Both happen once the row has changed; accepting the request changes nothing.

### Production: Redis

Both stores are in-process, which is right for a single instance and wrong for a scaled-out App Service, where each instance would evict only itself. Each has a drop-in distributed replacement (`AddStackExchangeRedisOutputCache` for the list; a Redis `ICache` adapter over `IDistributedCache` for the single user, which must serialize `PasswordHash` despite its `[JsonIgnore]`), and nothing in the policies, the worker or the decorator changes.

## Continuous integration

Every pull request, and every push to `main`, runs the `CI` workflow (`.github/workflows/ci.yml`) on GitHub Actions. It restores the solution, builds it in Release and runs all four test projects. `Directory.Build.props` sets `TreatWarningsAsErrors`, so the build step is also the lint: a new warning anywhere in the solution fails the run. The tests need no database - `UserManagement.Data.Tests` runs `DataContext` on EF Core's InMemory provider and everything above the data layer is mocked - so there is no SQL Server service container to provision or keep in step with production. There is no `global.json`; the workflow pins the SDK line (`10.0.x`) itself.

A newer push to the same branch cancels the run it supersedes (`concurrency` with `cancel-in-progress`), so a pull request only ever has one run in flight. The test results are uploaded as a `test-results` artifact (one `.trx` per test project) whether the run passes or fails, so a failure can be diagnosed from the run page without reproducing it locally. The badge at the top of this file tracks the latest run on `main`.

The workflow runs the same commands as a local build, so these three reproduce it from the repository root:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

## Infrastructure

Everything the two hosts need on Azure is declared in one Bicep template, `infra/main.bicep`, deployed at resource-group scope, with one parameter file per environment (`infra/dev.bicepparam` today). Bicep was chosen over Terraform because there is no state file to host or lock - Azure Resource Manager is the state - and the only tooling is the Azure CLI that a deployment pipeline already needs. The environment name is a parameter and appears in every resource name, so a new environment is one more parameter file. The web apps and the SQL server carry a `uniqueString(resourceGroup().id)` suffix because their names must be unique across all of Azure; everything else follows the `<type>-inflo-<environment>` convention.

| Resource | Name | Tier |
|---|---|---|
| App Service plan (Linux) | `plan-inflo-<env>` | F1 |
| Web app: `UserManagement.Api` | `app-inflo-api-<env>-<suffix>` | .NET 10 on the plan above |
| Web app: `UserManagement.Blazor` | `app-inflo-blazor-<env>-<suffix>` | .NET 10 on the plan above, WebSockets on |
| Azure SQL logical server | `sql-inflo-<env>-<suffix>` | - |
| Azure SQL database | `sqldb-inflo-<env>` | General Purpose serverless, `useFreeLimit` |
| Log Analytics workspace | `log-inflo-<env>` | PerGB2018, 0.1 GB daily cap |
| Application Insights | `appi-inflo-<env>` | Workspace-based |

### Tiers and their limits

The development environment runs on the smallest tiers; scaling any of them up is a change to the template, not to the code. The limits worth knowing:

- **App Service F1** gives 60 CPU-minutes a day, 1 GB of memory and a shared instance for the two apps. There is no Always On, so an idle app is unloaded and the first request after that pays a cold start. The Health check feature is not active on the Free tier; both apps still declare `/health` as their path so it lights up the moment the plan is scaled up. Linux was chosen over Windows: the .NET 10 runtime is one `linuxFxVersion` line. Quota for this plan tier is granted per region and can be zero in a region a subscription has never used for App Service, which is why the hosting region is its own parameter (`hostingLocation`) and can differ from the region the data lives in.
- **Azure SQL serverless with `useFreeLimit`** has a monthly allowance of vCore-seconds. `freeLimitExhaustionBehavior` is `AutoPause`: when the allowance runs out the database pauses until the next month instead of billing. The database also auto-pauses after an hour idle, so the first request after a pause waits for it to resume (the API's [retry policy](#api-to-database) covers that). One caveat when checking the tier from the CLI: older Azure CLI releases report `useFreeLimit` as `null` even when it is set (their SQL API version predates the property). The portal's Pricing tier line is authoritative, and the `kind` property `az sql db show` returns contains `freelimit` on any CLI version.
- **Application Insights** writes to a workspace with a 0.1 GB daily cap, about 3 GB a month, so a runaway log loop is bounded.

App Service terminates TLS at its front end and forwards plain HTTP to the container, so both apps get `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; without it `UseHttpsRedirection` would see every request as HTTP and redirect forever.

### What the template wires

The settings listed under [Production (Azure)](#5-production-azure) are all composed inside the template, so nothing has to be typed into the portal:

| App | Setting | Source |
|---|---|---|
| `UserManagement.Api` | connection string `DefaultConnection` (SQL Azure) | the SQL server's FQDN, the database name and the two admin parameters |
| `UserManagement.Api` | `Jwt__SigningKey` | the `jwtSigningKey` parameter |
| `UserManagement.Blazor` | `Api__BaseUrl` | `https://` + the API site's default hostname |
| both | `APPLICATIONINSIGHTS_CONNECTION_STRING` | the Application Insights component |
| both | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true`, see above |

The template is the source of truth for configuration: each deployment replaces the app settings with this set, so a value added by hand in the portal does not survive the next deployment. The SQL server's firewall has the `AllowAllWindowsAzureIps` rule (0.0.0.0), which admits connections from Azure services only; the API needs it because it runs the EF Core migrations at startup. The template outputs both hostnames, both site names, the SQL server FQDN and the Application Insights connection string, which is what a deployment pipeline needs next.

### Deploying

One-time bootstrap: a resource group, and three values that are secrets. The three secrets are not in any file; the parameter file reads them from environment variables at deployment time (`readEnvironmentVariable`), so the same command works from a laptop and from a pipeline that maps its secrets onto the same names. The SQL login cannot be a reserved name such as `admin` or `sa`; the password needs 8 to 128 characters from at least three of upper case, lower case, digits and symbols; the signing key needs 32 or more random bytes (`openssl rand -base64 48` produces one).

Bash:

```bash
az group create --name rg-inflo-dev --location uksouth
export SQL_ADMIN_LOGIN='<login>'
export SQL_ADMIN_PASSWORD='<password>'
export JWT_SIGNING_KEY='<key>'
```

PowerShell:

```powershell
az group create --name rg-inflo-dev --location uksouth
$env:SQL_ADMIN_LOGIN = '<login>'
$env:SQL_ADMIN_PASSWORD = '<password>'
$env:JWT_SIGNING_KEY = '<key>'
```

The variables live only in that shell session, so the deployment commands below run from the same window. Preview the changes, then apply them:

```bash
az deployment group what-if --resource-group rg-inflo-dev --template-file infra/main.bicep --parameters infra/dev.bicepparam
```

```bash
az deployment group create --resource-group rg-inflo-dev --template-file infra/main.bicep --parameters infra/dev.bicepparam
```

The deployment is idempotent - running it again re-applies the template and reports no changes when nothing drifted - so a deployment pipeline runs it on every release before publishing the applications. `az bicep build` and `az bicep lint` validate the template offline, and the `what-if` above is the check to run before any change to it.

## Continuous deployment

Every push to `main` (and a manual run from the Actions tab) runs the `CD` workflow, `.github/workflows/cd.yml`. It re-applies the Bicep template and then publishes both applications to the sites the template created, so infrastructure and code always move together and a change to the template needs no separate step. The `CI` workflow still guards pull requests; `CD` repeats the build and the tests itself so nothing red is ever deployed.

### Live environment

The site names come from the template (`app-inflo-<app>-<env>-<suffix>`, the suffix derived from the resource group), so these addresses are stable across deployments. The Free tier unloads an idle app, so the first request after a quiet spell can take a while.

| App | URL | Health |
|---|---|---|
| `UserManagement.Blazor` | https://app-inflo-blazor-dev-p5pupochhqltc.azurewebsites.net | [/health](https://app-inflo-blazor-dev-p5pupochhqltc.azurewebsites.net/health) |
| `UserManagement.Api` | https://app-inflo-api-dev-p5pupochhqltc.azurewebsites.net | [/health](https://app-inflo-api-dev-p5pupochhqltc.azurewebsites.net/health) |

To sign in, use any of the seeded users listed under [Credentials](#credentials) with password `12345`; for example `ploew@example.com`. The same accounts work on a local run.

### How the workflow logs in

The workflow holds no Azure credential. It uses OpenID Connect: GitHub issues a short-lived token for the run, and an Entra app registration is configured to trust tokens whose subject is this repository's `main` branch. Entra exchanges that token for an Azure access token limited to what the app has been granted - Contributor on the one resource group. There is no client secret to store, rotate or leak, and a token from another branch or a fork is refused.

### One-time bootstrap

Done once per subscription from a shell logged in with `az login` and `gh auth login`, after the resource group and the three deployment values from [Deploying](#deploying) exist.

1. The app registration and its service principal. The `appId` printed by the first command is the client id used below.

    ```bash
    az ad app create --display-name github-inflo-deploy --query appId -o tsv
    az ad sp create --id <appId> --query id -o tsv
    ```

2. Contributor on the resource group and nothing else. If the command reports that the principal was not found, the directory has not replicated it yet: wait a minute and retry.

    ```bash
    az role assignment create --assignee-object-id <servicePrincipalObjectId> --assignee-principal-type ServicePrincipal --role Contributor --scope /subscriptions/<subscriptionId>/resourceGroups/rg-inflo-dev
    ```

3. The federated credential. The subject pins it to the `main` branch; the audience is the fixed value Azure expects. The JSON lives in a temporary file outside the repository.

    ```json
    {
      "name": "github-main",
      "issuer": "https://token.actions.githubusercontent.com",
      "subject": "repo:AntonioKoB@52575552/InfloTechTest@1357388560:ref:refs/heads/main",
      "audiences": ["api://AzureADTokenExchange"]
    }
    ```

    The subject carries the owner id and the repository id: repositories created after July 2026 get GitHub's immutable subject claim, so a recycled owner or repository name can never reuse the trust. The exact prefix for a repository is `gh api repos/<owner>/<repo>/actions/oidc/customization/sub`, and the `azure/login` step prints the subject it presented, which is the quickest way to spot a mismatch.

    ```bash
    az ad app federated-credential create --id <appId> --parameters federated-credential.json
    ```

4. The six repository secrets. The first three identify where to log in; the last three are the values the Bicep parameter file reads, mapped by the `infra` job onto environment variables of the same name.

    | Secret | Value | Used by |
    |---|---|---|
    | `AZURE_CLIENT_ID` | the app registration's `appId` | `azure/login` in every job that touches Azure |
    | `AZURE_TENANT_ID` | `az account show --query tenantId` | `azure/login` |
    | `AZURE_SUBSCRIPTION_ID` | `az account show --query id` | `azure/login` |
    | `SQL_ADMIN_LOGIN` | the SQL administrator login | the `infra` job, as `SQL_ADMIN_LOGIN` for `dev.bicepparam` |
    | `SQL_ADMIN_PASSWORD` | the SQL administrator password | the `infra` job, as `SQL_ADMIN_PASSWORD` |
    | `JWT_SIGNING_KEY` | the API's HS256 signing key | the `infra` job, as `JWT_SIGNING_KEY` |

    ```bash
    gh secret set AZURE_CLIENT_ID --body <appId>
    gh secret set AZURE_TENANT_ID --body <tenantId>
    gh secret set AZURE_SUBSCRIPTION_ID --body <subscriptionId>
    gh secret set SQL_ADMIN_LOGIN
    gh secret set SQL_ADMIN_PASSWORD
    gh secret set JWT_SIGNING_KEY
    ```

    Without `--body` the command prompts for the value, so a secret never appears on a command line or in shell history. The last three must be the values the environment is already running with: the template writes the SQL password into the server and into the API's connection string on every run, so a different value amounts to a password rotation, and a different signing key invalidates every token already issued.

### What a run does

| Job | Waits for | What it does |
|---|---|---|
| `build` | - | Restore, Release build, the full test suite (results uploaded), then `dotnet publish` of both hosts, uploaded as the `api` and `blazor` artifacts |
| `infra` | - | `azure/login`, then the same `az deployment group create` command as in [Deploying](#deploying), with the three secrets mapped onto the environment variables the parameter file reads. Exposes the template's `apiSiteName` and `blazorSiteName` outputs to the later jobs |
| `deploy-api`, `deploy-blazor` | `build`, `infra` | Download the artifact and `azure/webapps-deploy` it to the site named by the `infra` output |
| `smoke` | both deploys | `GET /health` on each site until it answers `200 Healthy`, for up to three minutes |

`build` and `infra` run in parallel: the template is idempotent and independent of the code. Both deploy jobs wait for both, so a failing test or a failed template stops the run before any site changes. The workflow sets no app setting of its own. The template owns the complete set (see [What the template wires](#what-the-template-wires)), so whatever `main.bicep` declares is what the sites run with after every deployment, and a value edited in the portal does not survive the next run. Deployments are serialised through a concurrency group that does not cancel: a second push queues behind the running deployment, and CI runs, which use their own group, cannot cancel a deployment either.

### Smoke test

Both applications expose `GET /health` ([Health checks](#health-checks)). The API's check opens the database, so a `Healthy` answer proves three things at once: the site started, the EF Core migration ran, and the connection string the template composed is right. The Blazor check proves that host is serving. The Free tier has no Always On, so the first request after a deployment can take tens of seconds while the app starts; the job polls every ten seconds for up to three minutes and fails the run if either site does not answer.

### Adding an environment

A new environment is a parameter file (`infra/<env>.bicepparam`, with its own `environmentName` and, if needed, hosting region), a federated credential whose subject names that environment's trigger (`...:environment:<env>` or `...:ref:refs/heads/<branch>`), and a second `infra`/deploy entry, or a matrix over the environment name, passing its parameter file and resource group. The workflow's logic does not change.

## Observability

Both hosts send telemetry to the Application Insights component the template creates (`appi-inflo-<env>`) through the `Microsoft.ApplicationInsights.AspNetCore` SDK. The connection string reaches each site as the `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting, written by the template; the SDK reads it from configuration, so there is no telemetry code beyond the registration.

### What is collected

| Signal | `UserManagement.Api` | `UserManagement.Blazor` |
|---|---|---|
| Requests | every API call, with status and duration | every page request |
| Dependencies | the SQL commands behind each request | the HTTP calls to the API |
| Exceptions | unhandled exceptions, attached to the request that raised them | the same, including circuit errors |
| Logs | `ILogger` output at the levels set in `appsettings.json` | the same |

The Blazor host's calls to the API carry W3C trace context, so a page request, the API request it triggered and that request's SQL commands appear as one end-to-end transaction. Sampling is the SDK's default rate-limited sampler, untouched. There is no browser-side snippet: the UI is server-rendered, so the server sees every interaction already.

### Handled errors are logged where they are handled

Unhandled exceptions are recorded by the framework with no code of this project's own. A handled error would otherwise vanish into its response, so each place that handles one logs it with a fixed event id. The messages are source-generated `LoggerMessage` methods, which keeps the templates typed and costs nothing when a level is switched off. Credentials and tokens never appear in a message.

| Event | Where | Level |
|---|---|---|
| Login rejected (the email, never the password) | API `AuthController`, Blazor `AuthEndpoints` | Warning |
| A command failed in the worker (its type, its id and the reason - a duplicate email, a user that no longer exists - with the exception) | API `CommandWorker` | Warning |
| The API rejected the session token (method and path, never the token) | Blazor `BearerTokenHandler` | Warning |
| The API rejected the logout; signed out locally anyway | Blazor `AuthEndpoints` | Warning |
| Antiforgery validation failed on a login or logout post | Blazor `AuthEndpoints` | Warning |
| The worker's consume loop faulted (the exception); the host stops so the process is restarted | API `CommandWorker` | Error |
| User, log entry or command not found by id | API `UsersController`, `LogsController`, `CommandsController` | Information |
| The worker stopped with the host | API `CommandWorker` | Information |

The worker runs outside any request, so its SQL commands and its log lines appear as their own operations, separate from the request that accepted the command.

### Locally

A local run sends nothing. Without `APPLICATIONINSIGHTS_CONNECTION_STRING` the SDK is not registered at all (`AddApiTelemetry` in the API, `AddBlazorTelemetry` in the Blazor host), so startup is unchanged and there is no warning about the missing setting. To point a local run at the deployed component, put its connection string in the host's user secrets under that key. The log lines above reach the console either way.

### Cost

Ingestion is free for the first 5 GB per month per workspace, and the workspace's daily cap (see [Infrastructure](#infrastructure)) keeps this environment well inside that. Ninety days of retention are included.

## Message bus and worker

Writes to users are not executed inside the HTTP request. The API validates the request, turns it into a command, puts the command on a bus and answers **202 Accepted**; a worker hosted in the API process takes commands off the bus and executes them. Reads are untouched, and stay behind the caches described above.

### The command flow

1. `POST /api/users`, `PUT /api/users/{id}` and `DELETE /api/users/{id}` publish a `CreateUserCommand`, `UpdateUserCommand` or `DeleteUserCommand` (`UserManagement.Services/Commands`) and return `202 Accepted` with `{ "commandId": "…" }` in the body and a `Location` header pointing at `GET /api/commands/{commandId}`. The command is marked Pending before it is published, so the worker cannot finish before the mark is written.
2. `CommandWorker` (`UserManagement.Api/Commands`), a `BackgroundService`, consumes `IMessageBus` in order. Each command runs in its own dependency-injection scope through the `ICommandHandler<TCommand>` registered for its type. The handlers write through the same `IUserService` the API used to call directly, so the uniqueness check and the single-user cache behave as before, and each returns the id of the user it affected.
3. The outcome lands in `ICommandStatusStore`: Completed with that user id, or Failed with the exception message. A handler that throws does not stop the worker; the failure is logged with the exception (event 1401, see [Observability](#observability)) and the next command is taken.
4. The audit log takes the same road. `IUserLogService.RecordAsync` builds the finished `UserLog` entry - snapshots serialized at the moment of the action - and publishes it as a `RecordUserLogCommand`; `RecordUserLogCommandHandler` writes it. Every entry (Created, Updated, Deleted, Viewed, LoggedIn, LoggedOut) is therefore written off the request path, and a user write and its log entry are decoupled from each other as well as from the request.

### The status endpoint

`GET /api/commands/{id}` answers `200` with

```json
{ "commandId": "…", "state": "Pending", "userId": null, "error": null }
```

where `state` is `Pending`, `Completed` or `Failed`. `userId` is set once the command completed and is the only place a created user's id appears; `error` carries the reason when it failed, and a duplicate email - the usual failure - reads `A user with email '…' already exists.`. An unknown id is a `404`.

### What stays synchronous

Model validation: an invalid body is a `400` immediately, as before. The caller made a mistake it can fix now, and there is nothing to queue. For the same reason `PUT` still checks that the user exists (`404`), and the password is hashed inside the request so that the command carries the hash and the clear-text password never reaches a transport. Everything that touches the database on a write happens in the worker.

The Blazor UI treats the `202` as a promise, not a result: Add, Edit and Delete show a saving state and poll the status endpoint every 250 ms through a small client-side poller (`CommandPoller`, in `UserManagement.Blazor/Api`) until the command is Completed, then refresh what they show, or Failed, then show the reason in the form - the duplicate email lands where a validation error would. A command still Pending after 30 seconds is reported as a timeout, so a lost command does not look like a slow one; a poll is also cancelled when its component is disposed, so a closed browser tab does not keep one running.

### Cost, and the limits that come with it

The in-memory transport (`InMemoryMessageBus`, a `System.Threading.Channels` channel) and the in-memory status store (`InMemoryCommandStatusStore`, a `ConcurrentDictionary`) were chosen purely for cost: they need no Azure resource and add nothing to the bill. They are not durable across restarts and not shared across instances - a command accepted just before a restart is lost, and an instance only knows about the commands it accepted itself. Neither bites on a single free-tier instance, which is what the template deploys. A production deployment would use Azure Service Bus behind the same `IMessageBus` interface, so that the worker can move to its own host and scale independently of the API, and a table behind `ICommandStatusStore`, so that statuses survive restarts, are visible from every instance and can expire (the dictionary keeps every entry).

There is no outbox either. Marking a command Pending and publishing it, and writing a user and publishing its log entry, are each two separate operations with no transaction around them. The in-memory bus makes that harmless, since a publish cannot fail except at shutdown; a broker would not. The command table is where an outbox lands: the row inserted when the API accepts a command is both its durable queue entry and the status the caller polls, written in the same transaction as any business change, with the worker or a relay reading from it.

## Points to improve

Known gaps, in the order they would be tackled:

1. **Paginate the users list.** `GET /api/users` returns every row for the chosen filter, which is fine at the seeded size and not at scale. The Logs page already pages server-side (`GetPageAsync`), so the pattern exists: the users endpoint would take `page` and `pageSize`, the Blazor list would gain the same controls the Logs page has, and the output cache needs no change, since its key already varies by every query parameter and each page becomes its own bounded entry.
2. **A durable transport and status store** - Azure Service Bus behind `IMessageBus` and a table behind `ICommandStatusStore` - as described under [Message bus and worker](#message-bus-and-worker).
3. **Distributed cache stores** for a multi-instance deployment, as described under [Caching](#caching).
4. **Pin the Blazor client's resilience wiring with a test.** The named `HttpClient` that carries `AddStandardResilienceHandler` and the handler the authenticated Refit clients are built from are matched by name (`nameof(IUsersApi)`); a rename would silently drop the retries, and nothing catches it today. Lifting the client factory out of `Program.cs` into a testable extension would let a test resolve `IUsersApi` and assert the handler chain.
