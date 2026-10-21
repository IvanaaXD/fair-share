"""
Shared pieces of the FairShare load tests: test accounts, sign-in, finding the API routes and
the test data that is sent. Used by locustfile.py and provjera.py.

Settings come from environment variables, so nothing has to be changed in the code:
  FS_USERS       e-mail addresses of the test accounts, separated by commas
                 (default: the 8 demo accounts created by DemoDataSeeder)
  FS_PASSWORD    their password (default: the demo password)
  FS_VERIFY_TLS  "1" to check the HTTPS certificate (default: no - the local development
                 certificate is self-signed)
"""
from __future__ import annotations

import datetime as dt
import os
import random
import threading
import time

import requests
import urllib3

DEMO_USERS = [
    "jelena@demo.fairshare.local",
    "marko@demo.fairshare.local",
    "ana@demo.fairshare.local",
    "nikola@demo.fairshare.local",
    "tamara@demo.fairshare.local",
    "vuk@demo.fairshare.local",
    "milica@demo.fairshare.local",
    # ognjen@demo.fairshare.local is left out: DemoDataSeeder blocks that account on purpose
    # (to show unblocking in the administration), so it cannot sign in.
]

USERS = [email.strip() for email in os.getenv("FS_USERS", ",".join(DEMO_USERS)).split(",") if email.strip()]
PASSWORD = os.getenv("FS_PASSWORD", "Demo1234!")
VERIFY_TLS = os.getenv("FS_VERIFY_TLS", "0") == "1"

if not VERIFY_TLS:
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

LOGIN_PATH = "/api/auth/login"

# Some controllers use [Route("api/[controller]")] (singular: api/Budget), others a plural route
# (api/expenses). The first address that answers with something other than 404 is used, so the
# tests work with either.
ROUTE_CANDIDATES: dict[str, list[str]] = {
    "groups": ["/api/groups/mine", "/api/group/mine"],
    "expenses": ["/api/expenses", "/api/expense"],
    "budgets": ["/api/budget/mine", "/api/budgets/mine"],
    "categories": ["/api/category", "/api/categories"],
    "analytics": ["/api/analytics/spending", "/api/analytic/spending"],
    "unread": [
        "/api/notifications/unread-count",
        "/api/notification/unread-count",
        "/api/notifications/unreadcount",
        "/api/notification/unreadcount",
    ],
    "report": ["/api/reports/spending", "/api/report/spending"],
}

# Routes inside a group; found with the first group of the first account.
GROUP_ROUTE_CANDIDATES: dict[str, list[str]] = {
    "group_expenses": ["/api/groups/{group}/expenses", "/api/group/{group}/expenses"],
    "settlements": ["/api/groups/{group}/settlements", "/api/group/{group}/settlements"],
}

# Without these the main scenario still makes sense; their tasks are simply skipped.
OPTIONAL_ROUTES = {"unread", "report"}

ROUTES: dict[str, str] = {}
CATEGORY_IDS: list[str] = []
_setup_lock = threading.Lock()


def today() -> str:
    return dt.date.today().isoformat()


def this_month() -> str:
    return dt.date.today().strftime("%Y-%m")


def auth_header(token: str) -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


# ---------- accounts ----------

class Account:
    """A test account. All virtual users that share it also share one access token."""

    def __init__(self, email: str):
        self.email = email
        self.token: str | None = None
        self.user_id: str | None = None
        # [{"id": ..., "memberIds": [...]}] - groups this account is a member of
        self.groups: list[dict] = []
        self.lock = threading.Lock()


class AccountPool:
    """Hands out the accounts in turn, so the load is spread over all of them."""

    def __init__(self, emails: list[str]):
        if not emails:
            raise RuntimeError("FS_USERS is empty - at least one test account is needed.")
        self.accounts = [Account(email) for email in emails]
        self._next = 0
        self._lock = threading.Lock()

    def take(self) -> Account:
        with self._lock:
            account = self.accounts[self._next % len(self.accounts)]
            self._next += 1
            return account


POOL = AccountPool(USERS)


def sign_in(client, account: Account, force: bool = False) -> None:
    """
    Signs the account in once (or again after a 401). The sign-in goes through the Locust
    client, so it appears in the statistics as its own row. Logging in only once per account
    keeps the test under the API's limit of sign-ins per IP address.
    """
    with account.lock:
        if account.token and not force:
            return
        for attempt in range(4):
            with client.post(
                LOGIN_PATH,
                json={"username": account.email, "password": PASSWORD},
                name="POST /api/auth/login",
                catch_response=True,
            ) as response:
                if response.status_code == 200:
                    body = response.json()
                    account.token = body["accessToken"]
                    account.user_id = body.get("id")
                    response.success()
                    break
                response.failure(f"{response.status_code}: {response.text[:200]}")
                if response.status_code != 429 or attempt == 3:
                    raise RuntimeError(
                        f"Prijava nije uspjela za {account.email}: HTTP {response.status_code}. "
                        + ("Ograničenje prijava – da li je API pokrenut sa --RateLimiting:Enabled=false?"
                           if response.status_code == 429
                           else "API ne odgovara – da li je pokrenut i na ovoj adresi?"
                           if response.status_code == 0
                           else response.text[:200]))
                # 10 sign-ins per minute per address: wait as long as the API says, then try again.
                wait = int(response.headers.get("Retry-After", "20"))
            print(f"Prijava {account.email}: 429, čekam {wait} s…")
            time.sleep(wait)

        # Still under the account's lock: other virtual users of the same account wait here
        # until the routes and the account's groups are known, and only then start their tasks.
        try:
            prepare(client.base_url, account)
        except Exception:
            account.token = None
            raise


def prepare(host: str, account: Account) -> None:
    """Finds the routes and loads what the tests need (groups, categories). Not measured."""
    headers = auth_header(account.token)
    with _setup_lock:
        if not ROUTES:
            discover_routes(host, headers)
        if not CATEGORY_IDS:
            categories = requests.get(host + ROUTES["categories"], headers=headers, verify=VERIFY_TLS, timeout=30)
            categories.raise_for_status()
            CATEGORY_IDS.extend(category["id"] for category in categories.json())

    if not account.groups:
        groups = requests.get(host + ROUTES["groups"], headers=headers, verify=VERIFY_TLS, timeout=30)
        groups.raise_for_status()
        account.groups = [
            {"id": group["id"], "memberIds": [member["userId"] for member in group.get("members", [])]}
            for group in groups.json()
        ]

    with _setup_lock:
        if account.groups and not all(key in ROUTES for key in GROUP_ROUTE_CANDIDATES):
            discover_group_routes(host, headers, account.groups[0]["id"])


MISSING_ROUTES: list[str] = []


def discover_routes(host: str, headers: dict[str, str]) -> None:
    for key, candidates in ROUTE_CANDIDATES.items():
        for path in candidates:
            # A GET without parameters is enough to see whether the route exists.
            response = requests.get(host + path, headers=headers, verify=VERIFY_TLS, timeout=30)
            if response.status_code != 404:
                ROUTES[key] = path
                break
        else:
            MISSING_ROUTES.append(f"{key} (probano: {', '.join(candidates)})")

    required_missing = [item for item in MISSING_ROUTES if item.split(" ")[0] not in OPTIONAL_ROUTES]
    if required_missing:
        raise RuntimeError("Nije pronađena ruta za: " + "; ".join(required_missing))


def discover_group_routes(host: str, headers: dict[str, str], group_id: str) -> None:
    missing = []
    for key, candidates in GROUP_ROUTE_CANDIDATES.items():
        for template in candidates:
            response = requests.get(host + template.format(group=group_id), headers=headers, verify=VERIFY_TLS, timeout=30)
            if response.status_code != 404:
                ROUTES[key] = template
                break
        else:
            missing.append(f"{key} (probano: {', '.join(candidates)})")
    if missing:
        MISSING_ROUTES.extend(missing)
        raise RuntimeError("Nije pronađena ruta za: " + "; ".join(missing))


def group_path(key: str, group_id: str, suffix: str = "") -> str:
    """group_path("settlements", id, "/balances") -> "/api/groups/{id}/settlements/balances"."""
    return ROUTES[key].format(group=group_id) + suffix


def collection(key: str) -> str:
    """"/api/expenses" from "/api/expenses"; "/api/group" from "/api/group/mine"."""
    path = ROUTES[key]
    return path[: -len("/mine")] if path.endswith("/mine") else path


# ---------- test data ----------

def personal_expense() -> dict:
    return {
        "amount": round(random.uniform(2, 80), 2),
        "currency": "BAM",
        "date": today(),
        "categoryId": random.choice(CATEGORY_IDS),
        "description": "Locust – тест оптерећења",
        "latitude": None,
        "longitude": None,
        "isRecurring": False,
        "recurrenceInterval": "None",
    }


def group_expense(account: Account, group: dict, amount: float | None = None) -> dict:
    """An equal split between all members, paid by the signed-in account."""
    return {
        "categoryId": random.choice(CATEGORY_IDS),
        "paidByUserId": account.user_id,
        "amount": amount if amount is not None else round(random.uniform(5, 120), 2),
        "description": "Locust – групни трошак",
        "date": today(),
        "splitType": "Equal",
        "currency": None,
        "participants": [{"userId": member, "amount": None, "percentage": None} for member in group["memberIds"]],
    }


def groups_with_others(account: Account) -> list[dict]:
    """Groups where a shared expense makes sense (at least two members)."""
    return [group for group in account.groups if len(group["memberIds"]) >= 2]
