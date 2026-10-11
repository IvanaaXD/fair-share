import type { Translation } from './srCyrl';

/** English. Typed as Translation, so a missing or misspelled key is a compile error. */
export const en: Translation = {
  app: {
    skipToContent: 'Skip to content',
  },
  nav: {
    mainMenu: 'Main menu',
    overview: 'Overview',
    expenses: 'My expenses',
    groups: 'Groups',
    budgets: 'Budgets',
    analytics: 'Analytics',
    notifications: 'Notifications',
    profile: 'Profile',
    yourGroups: 'Your groups',
    allGroups: 'All groups',
    openMenu: 'Open menu',
    closeMenu: 'Close menu',
    unread: 'Unread: {{count}}',
  },
  settings: {
    language: 'Language',
    themeToDark: 'Switch to dark theme',
    themeToLight: 'Switch to light theme',
  },
  groupStatus: {
    owe: 'You owe',
    owed: 'You are owed',
    settled: 'Settled',
  },
  overview: {
    title: 'Overview',
    addExpense: 'Add expense',
    createGroup: 'Create a group',
    emptyTitle: 'No expenses yet',
    emptyText:
      'Add your first expense or create a group with your flatmates and friends. Your spending, budgets and who owes whom will show up here.',
  },
  placeholder: {
    text: 'This page is coming soon.',
    back: 'Back to overview',
  },
  notFound: {
    title: 'Page not found',
    text: 'The address may be mistyped, or the page has moved.',
    back: 'Back to overview',
  },
};
