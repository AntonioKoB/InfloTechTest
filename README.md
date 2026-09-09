# User Management Technical Exercise

The exercise is an ASP.NET Core web application backed by Entity Framework Core, which faciliates management of some fictional users.
We recommend that you use [Visual Studio (Community Edition)](https://visualstudio.microsoft.com/downloads) or [Visual Studio Code](https://code.visualstudio.com/Download) to run and modify the application. 

**The application uses SQL Server via Entity Framework Core migrations. See the [Documentation](#documentation) section below for how to set up a database to run it against.**

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

The connection string is never committed to source control. Locally, it's configured via [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), which stores it outside the repository entirely (`%APPDATA%\Microsoft\UserSecrets` on Windows), so there's no risk of accidentally committing real credentials. From `UserManagement.Web`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=InfloUsersDB;User Id=InfloDBUser;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

Replace `YOUR_SERVER` with your instance (e.g. `localhost`, `localhost\SQLEXPRESS`, or `(localdb)\MSSQLLocalDB`) and `YOUR_PASSWORD` with the password chosen in step 2. `TrustServerCertificate=True` avoids a TLS-certificate error against a local instance that isn't using a trusted certificate.

Once set, just run the app (`dotnet run` from `UserManagement.Web`, or via your IDE) - it applies any pending migrations automatically on startup and seeds the same 11 users the in-memory version used to.

### 4. Adding future migrations

If the `User`/`UserLog` model changes, generate a new migration from the repository root:

```bash
dotnet ef migrations add <MigrationName> --project UserManagement.Data --startup-project UserManagement.Web
```

Commit the generated files under `UserManagement.Data/Migrations` - they're applied automatically the next time the app starts, no separate `database update` step needed.

### 5. Production (Azure)

Azure SQL is the intended production target - it's the same `Microsoft.EntityFrameworkCore.SqlServer` provider, so only the connection string changes, not the code. .NET User Secrets is a local-development-only mechanism (it's only loaded when `ASPNETCORE_ENVIRONMENT=Development`), so it plays no role in production. In Azure App Service, the equivalent is setting the connection string as an App Service Configuration value - this surfaces to the app as an environment variable, which ASP.NET Core's configuration system already reads automatically, so no code change is required. For stronger secret management (centralized rotation, RBAC-audited access) Azure Key Vault with a Managed Identity is a natural next step once a real deployment pipeline exists to attach it to.

## Static assets (bundling)

CSS and JS are bundled and minified via [`LigerShark.WebOptimizer.Core`](https://github.com/ligershark/WebOptimizer), configured in `UserManagement.Web/Program.cs`. Bootstrap's CSS + the site's own `site.css` are combined into a single `/css/bundle.css`, and jQuery + Bootstrap's JS bundle + `site.js` into a single `/js/bundle.js` - `_Layout.cshtml` references only these two files instead of the five individual ones. No extra tooling or build step is required; the middleware bundles/minifies on first request and serves from cache after that.

## API client resiliency (Blazor)

The Blazor app's calls to `UserManagement.Api` (via the `IUsersApi`/`ILogsApi` Refit clients) go through [`Microsoft.Extensions.Http.Resilience`](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) - Microsoft's own resilience package, built on [Polly](https://github.com/App-vNext/Polly) v8. It's wired in `UserManagement.Blazor/Program.cs` via `.AddStandardResilienceHandler()` on each `HttpClient`, which bundles retry (with exponential backoff), a per-attempt and total-request timeout, a circuit breaker, and a concurrency rate limiter in one call, rather than hand-wiring individual Polly policies.
