/**
 * Set when the server says the signed-in account has been blocked by an administrator. The
 * app signs the user out, and the login page shows why. Cleared by the next sign-in.
 */
let blocked = false;

export function markAccountBlocked() {
  blocked = true;
}

export function clearAccountBlocked() {
  blocked = false;
}

export function wasAccountBlocked(): boolean {
  return blocked;
}
