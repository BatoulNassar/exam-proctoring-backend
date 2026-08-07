# Database deployment

Schema changes are applied as a deliberate step, separate from starting the API.
Outside Development the application performs no schema change and no database
access during startup (see `DatabaseStartupExtensions`), so a database that is
unreachable or has a mismatched migration history can never take the API down.

## Applying migrations to a server

1. **Back up the database first.** The script is idempotent, not reversible.

2. Check where the target database currently stands:

   ```sql
   SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
   ```

3. Run `migrate.sql` against it. Each migration is wrapped in a
   `IF NOT EXISTS (... __EFMigrationsHistory ...)` guard, so migrations that are
   already applied are skipped and re-running the whole file is safe.

   ```
   sqlcmd -S <server> -d <database> -U <user> -P <password> -i db/migrate.sql
   ```

4. Re-run the query from step 2 and confirm the expected migrations are listed.

## Regenerating the script

`migrate.sql` is generated from the migrations in
`ExamProctoring.Infrastructure/Migrations`. Regenerate it after adding a
migration, and commit the result so it is clear what was deployed:

```
dotnet ef migrations script --idempotent \
  --project ExamProctoring.Infrastructure \
  --startup-project exam-proctoring-app \
  --output db/migrate.sql
```

## First deployment only: bootstrap data

A new database has no roles, permissions or super admin. Those are seeded by
setting the flag below, starting the API once, then turning it off again. Demo
data is never written outside Development regardless of this flag.

```json
"Database": { "RunBootstrapSeedOnStartup": true }
```
