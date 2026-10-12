/**
 * TEMPORARY sample data for the sidebar, until the groups and notifications steps connect it
 * to the API (GET /api/groups, GET /api/notifications). Only the sidebar imports this file,
 * so it is deleted in one place later. The signed-in user already comes from the API.
 */
export interface SidebarGroup {
  id: string;
  name: string;
  /** Current user's net balance in the group: negative = owes, positive = is owed. */
  balance: number;
}

export const sampleGroups: SidebarGroup[] = [
  { id: 'stan-pajsijeva', name: 'Стан Пајсијева 7', balance: -35.75 },
  { id: 'vikend-tara', name: 'Викенд на Тари', balance: 22 },
  { id: 'studentski-projekat', name: 'Студентски пројекат', balance: -8.4 },
];

export const sampleUnreadNotifications = 3;
