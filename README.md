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

From two terminals, after completing the [Database](#database) setup:

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

If the `User`/`UserLog` model changes, generate a new migration from the repository root:

```bash
dotnet ef migrations add <MigrationName> --project UserManagement.Data --startup-project UserManagement.Api
```

Commit the generated files under `UserManagement.Data/Migrations` - they're applied automatically the next time the app starts, no separate `database update` step needed.

### 5. Production (Azure)

Azure SQL is the intended production target - it's the same `Microsoft.EntityFrameworkCore.SqlServer` provider, so only the connection string changes, not the code. .NET User Secrets is a local-development-only mechanism (it's only loaded when `ASPNETCORE_ENVIRONMENT=Development`), so it plays no role in production. In Azure App Service, the equivalent is setting the connection string as an App Service Configuration value - this surfaces to the app as an environment variable, which ASP.NET Core's configuration system already reads automatically, so no code change is required. For stronger secret management (centralized rotation, RBAC-audited access) Azure Key Vault with a Managed Identity is a natural next step once a real deployment pipeline exists to attach it to.

## UI architecture (Blazor)

The original `UserManagement.Web` MVC application has been removed. Users and Logs were its entire surface area, so once both moved to Blazor there was nothing left for it to serve, and leaving an empty shell project behind would only have added a second thing to run and maintain.

### A new project rather than converting the MVC one

The Blazor UI is a new project rather than an in-place conversion of `UserManagement.Web`. Converting would have meant stripping out MVC controllers, views, view models and the bundling middleware while simultaneously adding Blazor's hosting model to the same project - a single tangled changeset where additions and removals are hard to review separately. As two distinct operations, "add the new app" and "delete the old one" each stand on their own and read clearly in isolation.

### Blazor Server rather than WebAssembly

The UI runs on Blazor Server. Both hosting models share the same component model, so the choice comes down to where component code executes and what that implies:

- **Security surface.** When authentication is added, the token can stay in the server-side circuit and never reach the browser. A WebAssembly client must hold its token in browser-accessible storage, which makes XSS a token-theft risk and is why production SPAs frequently front themselves with a backend-for-frontend to avoid exactly that.
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

### Authentication

Not implemented. When it is added, the intended approach is a token issued on login and attached to the API calls, with the token held server-side by the Blazor app rather than in the browser.

## Static assets

`LigerShark.WebOptimizer.Core` bundled and minified the MVC application's Bootstrap and jQuery assets, and was removed along with that project. The Blazor app does not need an equivalent: its build pipeline already fingerprints and compresses static web assets, MudBlazor ships a single pre-minified CSS and JS file each, and Blazor's CSS isolation bundles all component-scoped styles into one generated stylesheet. There is no separate bundling step to configure.

## API client resiliency (Blazor)

The Blazor app's calls to `UserManagement.Api` (via the `IUsersApi`/`ILogsApi` Refit clients) go through [`Microsoft.Extensions.Http.Resilience`](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) - Microsoft's own resilience package, built on [Polly](https://github.com/App-vNext/Polly) v8. It's wired in `UserManagement.Blazor/Program.cs` via `.AddStandardResilienceHandler()` on each `HttpClient`, which bundles retry (with exponential backoff), a per-attempt and total-request timeout, a circuit breaker, and a concurrency rate limiter in one call, rather than hand-wiring individual Polly policies.
