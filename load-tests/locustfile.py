"""
FairShare load tests (Locust). One scenario per class - always name the class you want to run:

  locust -f locustfile.py MixedUser        realistic mix of reading and writing (main test)
  locust -f locustfile.py CacheUser        analytics only - run with the cache on and off
  locust -f locustfile.py ConcurrencyUser  many requests change the same group expense at once
  locust -f locustfile.py RateLimitUser    shows the sign-in limit (expects HTTP 429)
  locust -f locustfile.py ReportUser       PDF reports - a few users, a stability check

See PROCITAJ.md for how to start the API and run each scenario.
"""
from __future__ import annotations

import random
import threading
from collections import Counter

from locust import HttpUser, between, constant, events, task

from fairshare_api import (
    LOGIN_PATH,
    POOL,
    ROUTES,
    VERIFY_TLS,
    Account,
    auth_header,
    collection,
    group_expense,
    group_path,
    groups_with_others,
    personal_expense,
    sign_in,
    this_month,
)

# Business rules the API reports with 409 are correct answers, not failures - e.g. a settlement
# suggestion cannot be calculated while a former member still has a balance.
BUSINESS_CONFLICT = 409


class FairShareUser(HttpUser):
    """Base class: signs in with a shared test account and sends authorised requests."""

    abstract = True
    # "Think time" between two actions of one user, like a person using the app.
    wait_time = between(1, 3)

    def on_start(self) -> None:
        self.client.verify = VERIFY_TLS
        self.account: Account = self.choose_account()
        try:
            sign_in(self.client, self.account)
        except Exception as error:  # noqa: BLE001 - shown to the person running the test
            # Without a sign-in nothing can be measured: stop the whole test with the reason.
            print(f"\n*** Test je prekinut: {error}\n")
            self.environment.runner.quit()
            raise

    def choose_account(self) -> Account:
        return POOL.take()

    def call(self, method: str, path: str, name: str, expected: tuple[int, ...] = (200,), **kwargs):
        """One request; statuses in `expected` count as success, anything else as a failure."""
        with self.client.request(
            method,
            path,
            name=name,
            headers=auth_header(self.account.token),
            catch_response=True,
            **kwargs,
        ) as response:
            if response.status_code == 401:
                # The access token expired during a long test: sign in again, count this one as failed.
                response.failure("401 - token expired, signing in again")
                sign_in(self.client, self.account, force=True)
            elif response.status_code in expected:
                response.success()
            elif response.status_code == 0:
                # No HTTP answer at all: the API is not running, crashed or refused the connection.
                response.failure(f"API ne odgovara: {response.error}")
            else:
                response.failure(f"{response.status_code}: {response.text[:200]}")
            return response

    def any_group(self) -> dict | None:
        return random.choice(self.account.groups) if self.account.groups else None


# ---------- 1. main scenario: realistic mix ----------

class MixedUser(FairShareUser):
    """
    About 70 % reading and 30 % writing, the way people use the app: they mostly look at
    groups, balances and their expenses, and now and then add an expense or settle up.
    """

    # --- reading ---

    @task(10)
    def my_groups(self):
        self.call("GET", ROUTES["groups"], "GET group (mine)")

    @task(8)
    def group_balances(self):
        group = self.any_group()
        if group:
            self.call("GET", group_path("settlements", group["id"], "/balances"), "GET group balances")

    @task(8)
    def group_expenses(self):
        group = self.any_group()
        if group:
            self.call("GET", group_path("group_expenses", group["id"], "?page=1&pageSize=20"), "GET group expenses (page)")

    @task(4)
    def group_settlements(self):
        group = self.any_group()
        if group:
            self.call("GET", group_path("settlements", group["id"]), "GET group settlements")

    @task(8)
    def my_expenses(self):
        self.call("GET", f"{ROUTES['expenses']}?page=1&pageSize=20", "GET personal expense (page)")

    @task(5)
    def my_budgets(self):
        self.call("GET", f"{ROUTES['budgets']}?month={this_month()}", "GET budget (this month)")

    @task(6)
    def analytics(self):
        period = random.choice(["LastWeek", "LastMonth", "Last3Months"])
        self.call("GET", f"{ROUTES['analytics']}?period={period}&currency=BAM", "GET analytic (spending)")

    @task(3)
    def categories(self):
        self.call("GET", ROUTES["categories"], "GET category")

    @task(5)
    def unread_notifications(self):
        if "unread" in ROUTES:
            self.call("GET", ROUTES["unread"], "GET unread notifications")

    # --- writing ---

    @task(8)
    def add_personal_expense(self):
        self.call("POST", collection("expenses"), "POST personal expense", expected=(200, 201), json=personal_expense())

    @task(6)
    def add_group_expense(self):
        # Expense + one split per member + removing old suggestions, in one transaction.
        groups = groups_with_others(self.account)
        if groups:
            group = random.choice(groups)
            self.call(
                "POST",
                group_path("group_expenses", group["id"]),
                "POST group expense (equal split)",
                expected=(200, 201),
                json=group_expense(self.account, group),
            )

    @task(3)
    def suggest_settlement(self):
        # The debt simplification algorithm over the group's net balances.
        group = self.any_group()
        if group:
            self.call(
                "POST",
                group_path("settlements", group["id"], "/generate?useExact=false"),
                "POST settlement suggestion (algorithm)",
                expected=(200, BUSINESS_CONFLICT),
            )


# ---------- 2. cache: analytics only ----------

class CacheUser(FairShareUser):
    """
    The same few analytics requests again and again. Run it twice - with the cache on and with
    "Caching:Enabled=false" - and compare the response times.
    """

    @task
    def analytics(self):
        period = random.choice(["LastWeek", "LastMonth", "Last3Months", "Last6Months"])
        self.call("GET", f"{ROUTES['analytics']}?period={period}&currency=BAM", "GET analytics (spending)")

    @task
    def categories(self):
        self.call("GET", ROUTES["categories"], "GET categories")


# ---------- 3. concurrency: the same group expense changed at once ----------

CONCURRENCY = Counter()
_shared_expense: dict = {}
_shared_lock = threading.Lock()


class ConcurrencyUser(FairShareUser):
    """
    Every virtual user edits THE SAME group expense, as fast as possible. Correct behaviour:
    each request either succeeds (200) or is refused with 409 ("the data was changed in the
    meantime") - never a 500, and the expense always stays consistent.
    """

    wait_time = between(0, 0.2)

    def choose_account(self) -> Account:
        # Only the payer may edit the expense, so everyone uses the first account.
        return POOL.accounts[0]

    def on_start(self) -> None:
        super().on_start()
        with _shared_lock:
            if _shared_expense:
                return
            groups = groups_with_others(self.account)
            if not groups:
                raise RuntimeError(f"{self.account.email} has no group with other members.")
            group = groups[0]
            body = group_expense(self.account, group, amount=100.0)
            response = self.call("POST", group_path("group_expenses", group["id"]), "setup: create shared expense", expected=(200, 201), json=body)
            _shared_expense.update(group=group, body=body, id=response.json()["id"])

    @task
    def edit_same_expense(self):
        body = dict(_shared_expense["body"], amount=round(random.uniform(10, 500), 2))
        response = self.call(
            "PUT",
            group_path("group_expenses", _shared_expense["group"]["id"], f"/{_shared_expense['id']}"),
            "PUT the same group expense",
            expected=(200, 409),
            json=body,
        )
        CONCURRENCY[response.status_code] += 1


# ---------- 4. rate limiting: too many sign-ins ----------

RATE_LIMIT = Counter()


class RateLimitUser(HttpUser):
    """
    Sign-in attempts with a wrong password from one address. With the limits on, after 10
    attempts in a minute the API answers 429 - the protection against guessing passwords.
    Run it with one user and the limits ON (a normal start of the API).
    """

    wait_time = constant(0.5)

    def on_start(self) -> None:
        self.client.verify = VERIFY_TLS

    @task
    def wrong_password(self):
        with self.client.post(
            LOGIN_PATH,
            json={"username": POOL.accounts[0].email, "password": "pogresna-lozinka"},
            name="POST login (wrong password)",
            catch_response=True,
        ) as response:
            RATE_LIMIT[response.status_code] += 1
            # 400/401 = refused sign-in, 429 = the limit - all are the expected behaviour.
            if response.status_code in (400, 401, 429):
                response.success()
            else:
                response.failure(f"{response.status_code}: {response.text[:200]}")


# ---------- 5. reports ----------

class ReportUser(FairShareUser):
    """A PDF report is heavy for the processor on purpose; a few users check that it stays stable."""

    wait_time = between(3, 6)

    @task
    def spending_pdf(self):
        if "report" not in ROUTES:
            raise RuntimeError("Ruta za PDF izvještaj nije pronađena - vidi provjera.py.")
        self.call("GET", f"{ROUTES['report']}?period=LastMonth&currency=BAM&format=Pdf&lang=sr-Cyrl", "GET spending report (PDF)")


# ---------- start and end of a test ----------

@events.test_start.add_listener
def only_one_scenario(environment, **_kwargs):
    # Without a class name Locust would run all scenarios together, which measures nothing useful.
    if environment.user_classes and len(environment.user_classes) > 1:
        names = ", ".join(cls.__name__ for cls in environment.user_classes)
        print(f"\n*** Pokreni JEDAN scenario, npr.: locust -f locustfile.py MixedUser (sada: {names})\n")
        environment.runner.quit()


@events.test_stop.add_listener
def summary(environment, **_kwargs):
    if CONCURRENCY:
        total = sum(CONCURRENCY.values())
        print("\n=== Konkurentnost: isti grupni trošak ===")
        for status, count in sorted(CONCURRENCY.items()):
            print(f"  HTTP {status}: {count} ({count * 100 / total:.1f} %)")
        errors = sum(count for status, count in CONCURRENCY.items() if status >= 500)
        print("  Grešaka 5xx:", errors, "- ispravno" if errors == 0 else "- PROBLEM")
    if RATE_LIMIT:
        print("\n=== Ograničenje prijava ===")
        for status, count in sorted(RATE_LIMIT.items()):
            print(f"  HTTP {status}: {count}")
        print("  Ograničenje radi." if RATE_LIMIT.get(429) else "  Nijedan 429 - da li je API pokrenut sa RateLimiting:Enabled=false?")
