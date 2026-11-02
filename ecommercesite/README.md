# ecommercesite

An ASP.NET Core 8 MVC e-commerce starter application demonstrating a clean
layered architecture (Controllers → Services → Repositories → EF Core) with
SQL Server, session-based authentication, customer management, product
category management, and user registration / login.

## Project structure

```
ecommercesite/
├── Controllers/         # MVC controllers (thin: validation, routing, TempData)
├── Services/            # Business logic layer (interfaces + implementations)
├── Repositories/        # Data-access layer (generic + specialized repositories)
├── Data/                # ApplicationDbContext + EF Core setup
├── Models/              # EF Core entities
├── ViewModels/          # DTOs for views (Login, Register, PagedResult)
├── Views/               # Razor views
├── wwwroot/             # Static assets (css, js, favicon)
├── Migrations/          # EF Core migrations
├── appsettings.json     # Configuration (connection string, logging, session)
├── Program.cs           # Composition root (DI, middleware, routing)
└── ecommercesite.csproj # Project file
```

## Architecture

The application follows a classic 3-layer architecture with strict dependency
direction: `Controllers → Services → Repositories → DbContext`. Each layer
depends only on the abstraction (interface) of the layer below, which keeps
the code testable and easy to swap implementations.

| Layer           | Responsibility                                              |
| --------------- | ---------------------------------------------------------- |
| Controllers     | Request binding, validation, TempData, redirect / view     |
| Services        | Business rules, orchestration, password hashing, paging    |
| Repositories    | CRUD + queries against `DbSet<T>` (SQL-side paging/filter) |
| Data / DbContext| EF Core mapping, `DbSet<>` exposure, model config          |

## Features

- **Role-based access** — Admin and Customer roles. The first user to register is auto-promoted to Admin; all subsequent registrations are Customers. Admins manage everything; customers can only browse products and place/view their own orders.
- **Customer CRUD** (admin) with server-side pagination, live jQuery UI autocomplete search, and a pie + bar chart of customer distribution by address.
- **Product Category CRUD** (admin).
- **Product CRUD** (admin) + browse (customers can view the catalog but not edit).
- **Order management** (admin) with multi-line-item create, customer + product dropdowns, status workflow, delivery assignment, search + date-range filter, and pie + bar charts by location and customer.
- **Customer order placement** — logged-in customers can browse products, place orders (auto-tied to their account), and view only their own orders.
- **Delivery person management** (admin) with order assignment + delivery status tracking.
- **Yearly order report** (admin) with column chart + KPI cards.
- **User registration** with strong validation + PBKDF2 password hashing.
- **Login / logout** using session cookies with role-aware redirects.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB is fine for development) or a connection string to a
  reachable SQL Server instance.

## Getting started

1. **Restore packages**

   ```bash
   dotnet restore
   ```

2. **Configure the connection string**

   Edit `appsettings.json` (or `appsettings.Development.json`) and replace
   `DefaultConnection` with a connection string that points to your SQL
   Server instance. The default uses LocalDB:

   ```
   Server=(localdb)\MSSQLLocalDB;Database=ecommercesite;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
   ```

3. **Apply the database migration**

   ```bash
   dotnet ef database update
   ```

   This creates the `Customer`, `Registers`, `Products` and `ProductCategories`
   tables.

4. **Run the application**

   ```bash
   dotnet run
   ```

   Open the printed URL (typically `https://localhost:7116`).

5. **(Optional) Configure external login providers**

   The app supports password login out of the box, plus **four** external
   OAuth providers: **Google**, **Microsoft**, **Facebook**, and **Twitter**.
   Each provider is independent — configure zero, one, or all of them. The
   login page shows a button for every provider whose credentials are real
   (not the `YOUR_*` placeholders).

   ### Google

   1. [Google Cloud Console](https://console.cloud.google.com/) → APIs &
      Services → Credentials → Create Credentials → **OAuth client ID**.
   2. Application type: *Web application*.
   3. Authorized redirect URI: `https://localhost:7116/signin-google`
   4. Copy the Client ID + Client Secret.
   5. Configure:

      ```bash
      dotnet user-secrets set "Authentication:Google:ClientId" "your-id.apps.googleusercontent.com"
      dotnet user-secrets set "Authentication:Google:ClientSecret" "your-secret"
      ```

   ### Microsoft

   1. [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade) → New registration.
   2. Supported account types: *Accounts in any organizational directory and personal Microsoft accounts* (= `common` tenant).
   3. Redirect URI (web): `https://localhost:7116/signin-microsoft`
   4. Copy the Application (client) ID. Under Certificates & secrets, create a client secret and copy the Value.
   5. Under Authentication → Implicit grant, check **ID tokens**.
   6. Configure:

      ```bash
      dotnet user-secrets set "Authentication:Microsoft:ClientId" "your-client-id"
      dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "your-secret"
      dotnet user-secrets set "Authentication:Microsoft:Tenant" "common"
      ```

      `Tenant` options: `common` (all accounts), `consumers` (personal only), `organizations` (work/school only), or a tenant GUID.

   ### Facebook

   1. [Facebook Developers](https://developers.facebook.com/) → My Apps → Create App → *Consumer*.
   2. Add the *Facebook Login* product → Settings.
   3. Valid OAuth Redirect URI: `https://localhost:7116/signin-facebook`
   4. Copy the App ID + App Secret.
   5. Configure:

      ```bash
      dotnet user-secrets set "Authentication:Facebook:AppId" "your-app-id"
      dotnet user-secrets set "Authentication:Facebook:AppSecret" "your-secret"
      ```

   ### Twitter / X

   1. [Twitter Developer Portal](https://developer.twitter.com/) → Projects &
      Apps → Create App.
   2. App settings → User authentication settings → Edit.
      - Type: *Web app*
      - Callback URI: `https://localhost:7116/signin-twitter`
      - Request email from users: **Yes**
   3. Copy the API Key (Consumer Key) + API Key Secret (Consumer Secret).
   4. Configure:

      ```bash
      dotnet user-secrets set "Authentication:Twitter:ConsumerKey" "your-key"
      dotnet user-secrets set "Authentication:Twitter:ConsumerSecret" "your-secret"
      ```

   ### After configuring

   Restart the app. The login page now shows a **Login with [Provider]**
   button for each configured provider. On first login via any provider, a
   Customer account is auto-created with the user's email; subsequent logins
   sign them in directly. If a user logs in with the same email through
   different providers, they all map to the same account.

   Providers whose credentials are left as `YOUR_*` placeholders are
   automatically hidden — the app runs in password-only mode until you
   configure at least one.

   ### Any other OAuth2 provider (GitHub, Discord, LinkedIn, Spotify, …)

   The four providers above (Google, Microsoft, Facebook, Twitter) use
   first-party NuGet packages. For **any other OAuth2 provider**, add an
   entry to the `Authentication:Providers` array in `appsettings.json` —
   no code changes needed. Each entry needs the OAuth2 endpoints, a
   client ID + secret, the scopes to request, and the JSON field name
   that carries the user's email in the provider's user-info response.

   **GitHub example (pre-filled in appsettings.json):**

   ```json
   "Providers": [
     {
       "Name": "GitHub",
       "DisplayName": "GitHub",
       "ClientId": "your-github-client-id",
       "ClientSecret": "your-github-client-secret",
       "AuthorizationEndpoint": "https://github.com/login/oauth/authorize",
       "TokenEndpoint": "https://github.com/login/oauth/access_token",
       "UserInformationEndpoint": "https://api.github.com/user",
       "EmailField": "email",
       "Scopes": [ "user:email" ],
       "Color": "#24292e"
     }
   ]
   ```

   Register a GitHub OAuth app at
   [github.com/settings/developers](https://github.com/settings/developers)
   — Authorization callback URL: `https://localhost:7116/signin-github`.

   **Discord example:**

   ```json
   {
     "Name": "Discord",
     "DisplayName": "Discord",
     "ClientId": "your-discord-client-id",
     "ClientSecret": "your-discord-client-secret",
     "AuthorizationEndpoint": "https://discord.com/api/oauth2/authorize",
     "TokenEndpoint": "https://discord.com/api/oauth2/token",
     "UserInformationEndpoint": "https://discord.com/api/users/@me",
     "EmailField": "email",
     "Scopes": [ "identify", "email" ],
     "Color": "#5865F2"
   }
   ```

   **GitLab example:**

   ```json
   {
     "Name": "GitLab",
     "DisplayName": "GitLab",
     "ClientId": "your-gitlab-client-id",
     "ClientSecret": "your-gitlab-client-secret",
     "AuthorizationEndpoint": "https://gitlab.com/oauth/authorize",
     "TokenEndpoint": "https://gitlab.com/oauth/token",
     "UserInformationEndpoint": "https://gitlab.com/api/v4/user",
     "EmailField": "email",
     "Scopes": [ "read_user" ],
     "Color": "#FC6D26"
   }
   ```

   The `Name` becomes the authentication scheme name + the `provider`
   query parameter passed to `AccountController.ExternalLogin`. The
   `DisplayName` is shown on the login button. `Color` is the hex color
   of the letter-badge icon for providers without a branded SVG logo
   (the four first-class providers always get their real logo).

   Entry whose `ClientId` starts with `YOUR_` is skipped, so you can
   leave the placeholder GitHub entry in `appsettings.json` and it won't
   break anything.

## Key improvements made in this revision

- **Bug fix:** `HomeController` now lives in the `ecommercesite.Controllers`
  namespace (previously had no namespace at all).
- **Bug fix:** `RegisterService` no longer throws `NotImplementedException`
  for read/update/delete operations; all methods are implemented.
- **Bug fix:** `AccountController` no longer mixes the `ecommerceSite.Models`
  and `ecommercesite.Models` namespaces.
- **Bug fix:** `ProductCat/Index.cshtml` and `ProductCat/Create.cshtml` now
  post to the correct `ProductCat` controller (was `ProductCategory`).
- **Bug fix:** `Dashboard.cshtml` no longer emits a second `<!DOCTYPE html>`
  inside the layout — it uses the shared `_Layout.cshtml`.
- **Bug fix:** `AccountController.Logout` action added (was referenced by the
  sidebar but did not exist).
- **Security:** Passwords are hashed with PBKDF2-SHA256 (100k iterations,
  16-byte salt). The plain-text check is gone.
- **Performance:** Pagination + search now happen in SQL via `IQueryable`
  (`Skip`/`Take`/`Where`) instead of loading the whole table into memory.
- **Performance:** `AsNoTracking()` is used on all read-only queries.
- **Validation:** Login uses a dedicated `LoginViewModel` (only Email +
  Password) instead of the full `Register` entity, so `ModelState.IsValid`
  no longer fails because of missing required fields.
- **UX:** `TempData["Success"]` / `TempData["Error"]` toast messages after
  CRUD operations.
- **UX:** Modernized `_Layout`, `_Menu`, `site.css` with a cohesive color
  system, hover states, and a responsive sidebar dashboard.
- **Hygiene:** Added `.gitignore` (was missing — `bin/`, `obj/` were
  committed).
- **Hygiene:** Consolidated eight overlapping migrations into a single
  `InitialCreate`.

## Configuration reference

| Key                                | Description                                  |
| ---------------------------------- | -------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string              |
| `Session:IdleTimeout`              | Session timeout (default 30 min)             |
| `Session:CookieName`               | Session cookie name                          |
| `Logging:LogLevel:Default`         | Default log severity                         |
| `Authentication:Google:ClientId/ClientSecret` | Google OAuth credentials (leave as `YOUR_GOOGLE_*` to disable) |
| `Authentication:Microsoft:ClientId/ClientSecret/Tenant` | Microsoft Account OAuth credentials |
| `Authentication:Facebook:AppId/AppSecret` | Facebook OAuth credentials |
| `Authentication:Twitter:ConsumerKey/ConsumerSecret` | Twitter OAuth credentials |
| `Authentication:Providers[]` | Array of generic OAuth2 providers (GitHub, Discord, LinkedIn, ...) — see below |

## OAuth / external login

The app supports any combination of external login providers out of the box.
Each provider is registered only when its credentials in `appsettings.json`
are real (not placeholders), so the app runs fine with zero, one, or all of
them enabled. The login page renders a branded button for every enabled
provider.

### Built-in first-class providers

These use the official Microsoft.AspNetCore.Authentication.* packages and
need no endpoint configuration — just credentials.

| Provider   | Config section                          | Where to get credentials |
| ---------- | --------------------------------------- | ------------------------ |
| Google     | `Authentication:Google`                 | <https://console.cloud.google.com/apis/credentials> |
| Microsoft  | `Authentication:Microsoft`              | <https://entra.microsoft.com> → App registrations |
| Facebook   | `Authentication:Facebook`               | <https://developers.facebook.com/apps> |
| Twitter / X| `Authentication:Twitter`                | <https://developer.twitter.com> |

For each, set the **Authorized redirect URI** in the provider's console to:

```
https://localhost:7116/signin-{provider}
```

(replace `7116` with your actual HTTPS port from `launchSettings.json`, and
`{provider}` with `google`, `microsoft`, `facebook`, or `twitter`).

### Generic OAuth2 providers (GitHub, Discord, LinkedIn, GitLab, ...)

Any other OAuth2 provider can be added by appending an entry to the
`Authentication:Providers` array in `appsettings.json` — no code changes
needed. Each entry needs the provider's authorization / token / user-info
endpoints plus your app's client ID + secret.

Example: GitHub is pre-configured as a placeholder. To enable it, get an
OAuth app at <https://github.com/settings/developers> (Authorization
callback URL = `https://localhost:7116/signin-GitHub`), then fill in:

```json
"Providers": [
  {
    "Name": "GitHub",
    "DisplayName": "GitHub",
    "ClientId": "your-github-oauth-app-client-id",
    "ClientSecret": "your-github-oauth-app-client-secret",
    "AuthorizationEndpoint": "https://github.com/login/oauth/authorize",
    "TokenEndpoint": "https://github.com/login/oauth/access_token",
    "UserInformationEndpoint": "https://api.github.com/user",
    "EmailField": "email",
    "Scopes": [ "user:email" ],
    "Color": "#24292e"
  }
]
```

To add another provider (e.g. Discord), append another object to the same
array with the appropriate endpoints and scopes.

### How external login works

1. User clicks "Login with Google" (or any enabled provider).
2. Browser redirects to the provider's consent screen.
3. After consent, the provider redirects to `/Account/ExternalLoginCallback`.
4. The callback reads the user's email from the provider's claims.
5. If a `Register` account with that email already exists → sign in.
6. If no account exists → auto-create a Customer account with a random
   password (so it can only be accessed via the external provider) → sign in.
7. The user lands on their role-appropriate dashboard (CustomerDashboard for
   customers, admin Dashboard for admins).

The `ExternalLogins` table records which provider + provider-key was used
for each login, so you can later build a "linked accounts" management page.

## Payment gateway

The e-commerce site includes a **simulated payment gateway** so customers
can pay for orders with a card at checkout. No real money moves — it's
designed for demos and education — but the architecture mirrors a real
gateway (Stripe / Razorpay / PayPal) so you can swap in a real provider
by replacing one service.

### How it works

1. Customer places an order → order created with `Status = "Pending"`
2. Browser redirects to `/Payment/Checkout/{orderId}`
3. Customer enters card details (name, number, expiry, CVV)
4. The `PaymentService` validates the card:
   - **Luhn check** — catches typos in the card number
   - **Expiry** — must be in the future
   - **CVV** — must be 3 digits (Visa/MC/Discover) or 4 (Amex)
   - **Brand detection** — Visa / Mastercard / Amex / Discover from the IIN
5. The service simulates a gateway round-trip (800–1800 ms delay) and
   applies test-card rules to decide success vs. decline
6. A `Payment` row is recorded regardless of outcome (for audit)
7. On success, the order's `Status` is bumped to `"Confirmed"`
8. The browser redirects to `/Payment/Result/{orderId}` showing the
   transaction ID + payment history

### Test card numbers

| Card number | Brand | Result |
| ----------- | ----- | ------ |
| `4242 4242 4242 4242` | Visa | ✅ Success |
| `5555 5555 5555 4444` | Mastercard | ✅ Success |
| `3782 822463 10005` | Amex | ✅ Success |
| `6011 1111 1111 1117` | Discover | ✅ Success |
| `4000 0000 0000 0002` | Visa | ❌ Decline: insufficient funds |
| `4000 0000 0000 0119` | Visa | ❌ Decline: suspected fraud |
| `4000 0000 0000 0069` | Visa | ❌ Decline: expired card |
| `4000 0000 0000 0127` | Visa | ❌ Decline: incorrect CVV |
| Any other valid-Luhn number | * | ✅ Success |

Use any future expiry date (e.g. `12/30`) and any 3-digit CVV (`123`).
For Amex, use a 4-digit CVV (`1234`).

### Swapping in a real gateway

The entire gateway logic lives in `Services/PaymentService.cs`. To use
Stripe / Razorpay / PayPal instead:

1. Install the provider's SDK (e.g. `Stripe.net`)
2. Add your API key to `appsettings.json` under a new `Payment:Stripe:SecretKey` section
3. Replace the body of `PaymentService.ProcessPaymentAsync` with a call to
   `PaymentIntentService.CreateAsync(...)` (Stripe) or
   `RazorpayClient.Payment.Capture(...)` (Razorpay)
4. Map the provider's response to the `PaymentResultDto` — the controller,
   views, and database schema stay unchanged

The `Payment` table already stores everything a real gateway returns:
transaction ID, card last 4, brand, amount, status, failure reason, and
timestamps. No migration is needed when switching providers.

## License

This project is provided as-is for educational purposes. Replace the
connection string, branding, and password hashing parameters before using
in production.
