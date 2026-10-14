import type { UserRole } from '../api/types';

/**
 * The backend sends the role as text ("Admin"); a backend without JsonStringEnumConverter
 * sends the enum number - UserRole { Customer = 0, Admin = 1 }.
 */
export function isAdminRole(role: UserRole | undefined | null): boolean {
  return role === 'Admin' || role === 1;
}
