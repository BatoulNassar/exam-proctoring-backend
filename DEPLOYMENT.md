# Deployment

## 1. Environment

```
ASPNETCORE_ENVIRONMENT = Production
```

This one matters more than it looks. Under `Development` the application applies
migrations **and seeds demo data** — students named Layla and Karim, fake exam
sessions, fabricated alerts. Pointed at a real database, that writes demo rows
into production.

## 2. Environment variables

Secrets are not read from `appsettings.json`. Set these on the server (IIS:
*Configuration Editor → system.webServer/aspNetCore → environmentVariables*, or
the hosting control panel):

| Variable | Example |
|---|---|
| `Email__From` | `noreply@yourdomain.edu` |
| `Email__SmtpHost` | `smtp.gmail.com` |
| `Email__SmtpPort` | `587` |
| `Email__Username` | the SMTP account |
| `Email__Password` | the SMTP password or app password |

Password reset is the only feature that needs these. Without them it fails with a
message naming the missing keys, rather than a bare 500.

The double underscore is how .NET maps a variable to a nested configuration key:
`Email__SmtpHost` becomes `Email:SmtpHost`.

## 3. WebSockets

The live monitoring screens and student warnings run over SignalR. IIS rejects
WebSocket connections unless the feature is installed:

*Server Manager → Add Roles and Features → Web Server (IIS) → Application
Development → **WebSocket Protocol***

Without it the API still serves requests, but every live screen stays frozen and
students never see a warning.

## 4. Schema

Migrations are never applied automatically outside Development. See
[db/README.md](db/README.md) for the script and the procedure.

## 5. First deployment only

A new database has no roles, permissions, super admin or settings row. Seed them
by setting the flag below, starting the API once, then setting it back to
`false`. Demo data is never written outside Development regardless of this flag.

```json
"Database": { "RunBootstrapSeedOnStartup": true }
```

## 6. Smoke test

```
GET  /swagger                 page loads
POST /api/auth/login          returns a token
GET  /api/alerts/types        returns the five alert types
GET  /api/settings            returns the settings row
```

Then open a SignalR connection to `/ws/monitoring` to confirm WebSockets work.
