# FairShare – load tests (Locust)

Locust starts many "virtual users" at once. Each one signs in and does what a real user does: opens groups and balances, adds expenses, opens analytics. Meanwhile Locust measures:

- how many requests per second the API handles;
- how long a response takes (median, 95th and 99th percentile);
- how many requests fail.

## What is in this folder

| File | Purpose |
|---|---|
| `locustfile.py` | scenarios (classes `MixedUser`, `CacheUser`, `ConcurrencyUser`, `RateLimitUser`, `ReportUser`) |
| `fairshare_api.py` | shared part: sign-in, test accounts, route discovery, data that is sent |
| `check.py` | quick check before a test: do sign-in and all routes work |
| `run_levels.ps1` | runs a scenario at 10, 50, 100 and 200 users and builds the table |
| `summary.py` | turns the CSV results into a Markdown table for the thesis |
| `requirements.txt` | exact Locust version |

Results go to `results/` (HTML reports with charts, CSV files and the table).

## 1. Installation (once)

Python 3.10 or newer is needed (python.org; tick "Add python.exe to PATH" during installation). In PowerShell, from the repository root:

```powershell
cd load-tests
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
locust --version
```

If `Activate.ps1` says running scripts is disabled, run `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` once and try again. If `run_levels.ps1` says it "is not digitally signed", run `Unblock-File .\run_levels.ps1` once.

Every next time `cd load-tests` and `.\.venv\Scripts\Activate.ps1` are enough; the prompt then starts with `(.venv)`. The `.venv` folder is not committed.

## 2. Database: a separate copy for the tests

The tests write to the database and change existing data (settlements, the same expense 50 times…). That is why they never run against the real `FairShareDb` database, only against its copy `fairshare_loadtest`. The real database is not touched.

The tests use 7 demo accounts (`jelena@demo.fairshare.local` … `milica@demo.fairshare.local`, password `Demo1234!`). They already exist in `FairShareDb`, so the copy has them too. Ognjen is not used, because the demo data blocks him on purpose (to show unblocking in the admin pages).

**Create the copy.** In pgAdmin open the Query Tool on the **`postgres`** database (not on `FairShareDb`). The API must be stopped. Run the statements **one at a time** (select a statement, F5) – `CREATE DATABASE` and `DROP DATABASE` cannot run inside a transaction block:

```sql
ALTER DATABASE "FairShareDb" WITH ALLOW_CONNECTIONS false;

SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'FairShareDb' AND pid <> pg_backend_pid();

CREATE DATABASE fairshare_loadtest TEMPLATE "FairShareDb";

ALTER DATABASE "FairShareDb" WITH ALLOW_CONNECTIONS true;
```

`"FairShareDb"` must be in double quotes because of the capital letters. If `CREATE DATABASE` fails, always run the last statement (`ALLOW_CONNECTIONS true`) yourself – otherwise neither the application nor pgAdmin can connect to the real database.

**Reset between scenarios that write** (main test and concurrency): drop the copy and create it again, so every test starts from the same state:

```sql
DROP DATABASE fairshare_loadtest WITH (FORCE);
-- then the block above again
```

**When you are done with the tests:** `DROP DATABASE fairshare_loadtest WITH (FORCE);`

## 3. Starting the API for a test

**Not from Visual Studio and not in Debug mode** – the debugger and logging of every SQL query slow the API down many times, so the test would measure them. Stop the API in Visual Studio, then run from the `Backend` folder (where the `.sln` is):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=fairshare_loadtest;Username=postgres;Password=YOUR_PASSWORD"
dotnet run --project FairShare.API -c Release --launch-profile https -- `
  --RateLimiting:Enabled=false `
  --Logging:LogLevel:Default=Warning `
  --Logging:LogLevel:Microsoft.AspNetCore=Warning `
  --Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning
```

The first line points the API at the database copy, only in this PowerShell window. It takes precedence over user-secrets; once the window is closed, the API from Visual Studio works with the real database again.

What the switches after `--` mean (they apply only to this run, `appsettings.json` is not changed):

- **`RateLimiting:Enabled=false`:** all virtual users come from the same computer and share 7 accounts. With a limit of 300 requests per minute per user the test would measure the limit, not the API. A separate scenario shows that the limit works (section 6D).
- **`Logging…=Warning`:** no console output for every request and SQL query.

The payment simulator is not needed (the tests do not pay by card), but the API reads its keys from user-secrets as usual.

## 4. Check before a test

In a second PowerShell window (with `(.venv)`):

```powershell
python check.py https://localhost:7221
python check.py https://localhost:7221 --write
```

The first command only reads. The second adds one personal and one group expense and calculates a settlement suggestion. At the end it must say "Everything is ready for Locust."

The script finds out by itself whether routes are singular or plural (e.g. `/api/budget` or `/api/budgets`, `/api/group/{id}/settlements` or `/api/groups/{id}/settlements`) and prints the ones it found. If something fails, send the whole output.

## 5. Two ways to run

**With the web UI**, to watch live:

```powershell
locust -f locustfile.py MixedUser --host https://localhost:7221
```

Open http://localhost:8089, enter the number of users (e.g. 50) and "Ramp up" (e.g. 10 per second), then Start. The "Charts" tab shows the charts, "Download Data" the report.

**Headless**, for the thesis results: the result is saved to `results/` automatically.

```powershell
locust -f locustfile.py MixedUser --headless --host https://localhost:7221 -u 100 -r 10 -t 2m --csv results/mix_100 --html results/mix_100.html
```

Always name **one** class (`MixedUser`, `CacheUser`…). Without it Locust would run all scenarios together, so the test stops at once with a message.

## 6. Scenarios

### A) Main test: realistic mix (`MixedUser`)

About 70% reads and 30% writes, with 1–3 s of "thinking" between actions:

- **reads:** list of groups, group balances, group expenses, settlements, personal expenses, budgets, analytics, categories, unread notification count;
- **writes:** personal expense, group expense split equally (several tables in one transaction) and a settlement suggestion (algorithm over the debt graph).

Create a fresh database copy (section 2), start the API as in section 3, then:

```powershell
.\run_levels.ps1
```

It runs 10, 50, 100 and 200 users for 2 minutes each, and at the end prints and saves the table `results/mix_table.md`. Other durations and levels: `.\run_levels.ps1 -Duration 3m -Levels 10,50,100,200,400`.

The level at which p95 suddenly jumps (e.g. from 80 ms to 900 ms) is the saturation point of the laptop – a useful number for the thesis.

**Stress test** (beyond the saturation point): `.\run_levels.ps1 -Levels 400,800 -Duration 4m -Name stress`.

### B) Cache (`CacheUser`)

The same analytics over and over. It runs twice, and the difference in response times shows how much HybridCache helps.

1. API as in section 3 (cache on):
   ```powershell
   .\run_levels.ps1 -Scenario CacheUser -Name cache_on -Levels 50,200
   ```
2. Stop the API and start it the same way, with one more switch, `--Caching:Enabled=false`:
   ```powershell
   .\run_levels.ps1 -Scenario CacheUser -Name cache_off -Levels 50,200
   ```
3. Compare `results/cache_on_table.md` and `results/cache_off_table.md`.

### C) Concurrency (`ConcurrencyUser`)

50 users change the **same** group expense at the same time (all as Jelena, who paid it). Correct behaviour:

- every request either succeeds (200) or is rejected with 409 ("the data was changed in the meantime");
- **no 500 at all**;
- the expense stays consistent.

Create a fresh database copy (section 2), start the API as in section 3, then:

```powershell
locust -f locustfile.py ConcurrencyUser --headless --host https://localhost:7221 -u 50 -r 50 -t 1m --html results/concurrency.html
```

At the end it prints how many responses were 200 and how many 409, and whether there were any 5xx errors. To prove the expense stayed consistent, run this query in pgAdmin; it looks for expenses whose splits do not add up to the amount and must return **0 rows**:

```sql
SELECT ge."Id", ge."Amount", SUM(s."Amount") AS "SumOfSplits"
FROM "GroupExpenses" ge
JOIN "ExpenseSplits" s ON s."GroupExpenseId" = ge."Id"
GROUP BY ge."Id", ge."Amount"
HAVING ge."Amount" <> SUM(s."Amount");
```

### D) Sign-in rate limit (`RateLimitUser`)

Sign-in attempts with a wrong password. After 10 attempts per minute the API answers with 429 – protection against password guessing. **The API must be started normally, with the limits on**: without `--RateLimiting:Enabled=false`; Visual Studio is fine too.

```powershell
locust -f locustfile.py RateLimitUser --headless --host https://localhost:7221 -u 1 -r 1 -t 30s
```

At the end it prints e.g. "HTTP 401: 10, HTTP 429: 50 – The limit works." For the next minute you cannot sign in from that computer either, which is expected.

### E) PDF reports (`ReportUser`)

PDF generation is CPU-heavy on purpose, so this is only a stability check with a few users:

```powershell
locust -f locustfile.py ReportUser --headless --host https://localhost:7221 -u 5 -r 1 -t 2m --html results/reports.html
```

## 7. What goes into the thesis

- **Environment:** CPU, RAM, Windows, .NET 10 in Release mode, PostgreSQL. A note that Locust and the API run on the same laptop and share the CPU, so the results are conservative. Why rate limiting is off for the main test and tested separately.
- **Table** from `results/mix_table.md`: users, requests per second, median, p95, p99, failures.
- **Charts** from `results/mix_100.html` (or 200): "Total Requests per Second" and "Response Times". Open the HTML in a browser and take a screenshot.
- **Cache:** the two tables side by side (on / off) and a sentence about the difference.
- **Concurrency:** number of 200/409, 0 errors 5xx, the query from 6C returning 0 rows.
- **Rate limiting:** number of 429 from scenario D.

## 8. Later, with Docker

The same tests, only a different address: Nginx instead of the API directly, e.g. `--host https://localhost`. The limit and the cache are then set through environment variables in `docker-compose.yml` (`RateLimiting__Enabled=false`, `Caching__Enabled=false`).

## Settings without changing the code

| Variable | Meaning | Default |
|---|---|---|
| `FS_USERS` | e-mail addresses of the test accounts, separated by commas | the 7 demo accounts |
| `FS_PASSWORD` | their password | `Demo1234!` |
| `FS_VERIFY_TLS` | `1` = verify the HTTPS certificate | not verified (development certificate) |

PowerShell example: `$env:FS_USERS = "jelena@demo.fairshare.local,marko@demo.fairshare.local"`.

## Results so far (`results/`)

| Prefix | What was measured |
|---|---|
| `mix_*` | MixedUser, 10–200 users, before the concurrency fix |
| `mix_after_*` | MixedUser, 200 and 400 users, after the retry fix in UnitOfWork |
| `mix_after_2_*` | the same, after adding jitter between retries |
| `mix_after_800_*` | MixedUser, 800 users, 4 minutes |
| `stress_*` | MixedUser, 400 and 800 users, before the concurrency fix |
| `cache_on_*`, `cache_off_*` | CacheUser, 50 and 200 users, cache on / off |
