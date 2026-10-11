/**
 * TEMPORARY sample data for the sidebar, until the login and groups steps connect it to the
 * API (GET /api/profile, GET /api/groups). Only the sidebar imports this file, so it is
 * deleted in one place later.
 */
export interface SidebarUser {
  firstName: string;
  lastName: string;
  email: string;
}

export interface SidebarGroup {
  id: string;
  name: string;
  /** Current user's net balance in the group: negative = owes, positive = is owed. */
  balance: number;
}

export const sampleUser: SidebarUser = {
  firstName: 'Јелена',
  lastName: 'Савић',
  email: 'jelena.savic@gmail.com',
};

export const sampleGroups: SidebarGroup[] = [
  { id: 'stan-pajsijeva', name: 'Стан Пајсијева 7', balance: -35.75 },
  { id: 'vikend-tara', name: 'Викенд на Тари', balance: 22 },
  { id: 'studentski-projekat', name: 'Студентски пројекат', balance: -8.4 },
];

export const sampleUnreadNotifications = 3;
