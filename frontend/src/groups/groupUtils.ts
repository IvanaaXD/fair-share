import { BookOpen, Briefcase, Gift, House, Mountain, Plane, PartyPopper, Users, Utensils, type LucideIcon } from 'lucide-react';
import type { Group, GroupMember, GroupRole } from '../api/types';
import { toLatin } from '../i18n/transliterate';

/** Below half a cent a balance counts as settled (amounts are split and rounded to cents). */
export const SETTLED_TOLERANCE = 0.005;

export type BalanceStatus = 'owe' | 'owed' | 'settled';

/** Negative balance: the user owes the group; positive: the group owes the user. */
export function balanceStatus(balance: number): BalanceStatus {
  if (balance <= -SETTLED_TOLERANCE) return 'owe';
  if (balance >= SETTLED_TOLERANCE) return 'owed';
  return 'settled';
}

export function isOwnerRole(role: GroupRole): boolean {
  return role === 'Owner' || role === 0;
}

/** Members in the order they joined - the owner (founder) first. Decides each member's color. */
export function membersByJoinDate(group: Group): GroupMember[] {
  return [...group.members].sort((a, b) => a.joinedAt.localeCompare(b.joinedAt));
}

/**
 * A member keeps the same color everywhere inside one group (avatars, balances, later the
 * split bars of expenses): the n-th member to join gets the n-th of six member colors.
 */
export function memberColor(index: number): string {
  return `var(--member-${(index % 6) + 1})`;
}

export function initials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
}

const ICON_RULES: [LucideIcon, string[]][] = [
  [House, ['stan', 'kuć', 'kuc', 'cimer', 'dom', 'home', 'flat', 'apartment', 'zgrad']],
  [Mountain, ['vikend', 'planin', 'tara', 'jahorin', 'zlatibor', 'kamp', 'weekend', 'hike', 'izlet']],
  [Plane, ['putovanj', 'put', 'trip', 'travel', 'ljetovanj', 'letovanj', 'odmor', 'more', 'vacation']],
  [Gift, ['rođendan', 'rodjendan', 'poklon', 'birthday', 'gift']],
  [PartyPopper, ['žurk', 'zurk', 'proslav', 'party', 'slavlj', 'svadb', 'novogodiš']],
  [BookOpen, ['studij', 'student', 'fakultet', 'projekat', 'project', 'škol', 'skol', 'kurs']],
  [Briefcase, ['posao', 'posl', 'kancelar', 'firma', 'work', 'office', 'kolege', 'tim']],
  [Utensils, ['ručak', 'rucak', 'večer', 'vecer', 'restoran', 'hrana', 'dinner', 'lunch']],
];

/** Groups have no icon on the backend; it is guessed from the name like for categories. */
export function groupIcon(name: string): LucideIcon {
  const words = toLatin(name).toLowerCase().split(/[\s,./&-]+/);
  for (const [icon, prefixes] of ICON_RULES) {
    if (prefixes.some((prefix) => words.some((word) => word.startsWith(prefix)))) {
      return icon;
    }
  }
  return Users;
}

/** Enum values may arrive as names or (older backend) as numbers. */
export function splitTypeName(value: string | number): 'Equal' | 'Percentage' | 'ExactAmount' {
  if (value === 'Percentage' || value === 1) return 'Percentage';
  if (value === 'ExactAmount' || value === 2) return 'ExactAmount';
  return 'Equal';
}

export function isSettledStatus(value: string | number): boolean {
  return value === 'Settled' || value === 1;
}

/**
 * Do the open settlement suggestions still match the balances? Each suggestion moves money
 * from the debtor to the creditor; after all of them everyone should be at zero. If an
 * expense was added or changed after the suggestion was calculated, that no longer holds.
 */
export function settlementsAreStale(
  proposed: { debtorUserId: string; creditorUserId: string; amount: number }[],
  balanceOf: Map<string, number>,
): boolean {
  const afterPayments = new Map(balanceOf);
  for (const settlement of proposed) {
    afterPayments.set(settlement.debtorUserId, (afterPayments.get(settlement.debtorUserId) ?? 0) + settlement.amount);
    afterPayments.set(settlement.creditorUserId, (afterPayments.get(settlement.creditorUserId) ?? 0) - settlement.amount);
  }
  return [...afterPayments.values()].some((value) => Math.abs(value) > SETTLED_TOLERANCE * 2);
}
