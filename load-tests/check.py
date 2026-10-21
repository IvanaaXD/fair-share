"""
Quick check before a load test: signs in with the first test account, finds the routes and calls
every endpoint the tests use once. Writes nothing to the database unless --write is given.

  python check.py https://localhost:7221
  python check.py https://localhost:7221 --write
"""
from __future__ import annotations

import sys

import requests

from fairshare_api import (
    LOGIN_PATH,
    MISSING_ROUTES,
    PASSWORD,
    POOL,
    ROUTES,
    VERIFY_TLS,
    auth_header,
    collection,
    group_expense,
    group_path,
    groups_with_others,
    personal_expense,
    prepare,
    this_month,
)


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    host = sys.argv[1].rstrip("/")
    write = "--write" in sys.argv[2:]
    account = POOL.accounts[0]

    login = requests.post(host + LOGIN_PATH, json={"username": account.email, "password": PASSWORD}, verify=VERIFY_TLS, timeout=30)
    print(f"{'OK  ' if login.status_code == 200 else 'FAIL'} {login.status_code}  POST {LOGIN_PATH}  ({account.email})")
    if login.status_code != 200:
        print("    ", login.text[:300])
        print("\nSign-in does not work: check that the API is running, the address, and that the demo accounts exist (DemoData:Enabled).")
        return 1
    account.token = login.json()["accessToken"]
    account.user_id = login.json().get("id")

    try:
        prepare(host, account)
    finally:
        print("\nRoutes found:")
        for key, path in ROUTES.items():
            print(f"   {key:<11} {path}")
        for item in MISSING_ROUTES:
            print(f"   NOT FOUND: {item}")
    print(f"\nAccount groups: {len(account.groups)}, categories: OK")

    headers = auth_header(account.token)
    group = account.groups[0]["id"] if account.groups else None
    checks = [
        ("GET", ROUTES["groups"], None),
        ("GET", f"{ROUTES['expenses']}?page=1&pageSize=20", None),
        ("GET", f"{ROUTES['budgets']}?month={this_month()}", None),
        ("GET", f"{ROUTES['analytics']}?period=LastMonth&currency=BAM", None),
        ("GET", ROUTES["categories"], None),
    ]
    if "unread" in ROUTES:
        checks.append(("GET", ROUTES["unread"], None))
    if "report" in ROUTES:
        checks.append(("GET", f"{ROUTES['report']}?period=LastMonth&currency=BAM&format=Pdf&lang=sr-Cyrl", None))
    if group:
        checks += [
            ("GET", group_path("settlements", group, "/balances"), None),
            ("GET", group_path("group_expenses", group, "?page=1&pageSize=20"), None),
            ("GET", group_path("settlements", group), None),
        ]
    if write:
        checks.append(("POST", collection("expenses"), personal_expense()))
        shared = groups_with_others(account)
        if shared:
            checks.append(("POST", group_path("group_expenses", shared[0]["id"]), group_expense(account, shared[0])))
            checks.append(("POST", group_path("settlements", shared[0]["id"], "/generate?useExact=false"), None))

    print()
    failed = 0
    for method, path, body in checks:
        response = requests.request(method, host + path, json=body, headers=headers, verify=VERIFY_TLS, timeout=60)
        ok = response.status_code < 400 or (path.endswith("useExact=false") and response.status_code == 409)
        failed += not ok
        print(f"{'OK  ' if ok else 'FAIL'} {response.status_code}  {method} {path}")
        if not ok:
            print("     ", response.text[:300])

    print("\nEverything is ready for Locust." if failed == 0 else f"\n{failed} call(s) failed - send this output.")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
