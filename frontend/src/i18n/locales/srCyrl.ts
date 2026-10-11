/**
 * Serbian (Cyrillic) - the source language of the app. The Latin version is generated from
 * this file automatically (see transliterate.ts), so only this file and en.ts are maintained.
 * The English file must have exactly the same keys - TypeScript checks that.
 */
export const srCyrl = {
  app: {
    skipToContent: 'Прескочи на садржај',
  },
  nav: {
    mainMenu: 'Главни мени',
    overview: 'Преглед',
    expenses: 'Лични трошкови',
    groups: 'Групе',
    budgets: 'Буџети',
    analytics: 'Аналитика',
    notifications: 'Обавјештења',
    profile: 'Профил',
    yourGroups: 'Твоје групе',
    allGroups: 'Све групе',
    openMenu: 'Отвори мени',
    closeMenu: 'Затвори мени',
    unread: 'Непрочитано: {{count}}',
  },
  settings: {
    language: 'Писмо и језик',
    themeToDark: 'Укључи тамну тему',
    themeToLight: 'Укључи свијетлу тему',
  },
  groupStatus: {
    owe: 'Дугујеш',
    owed: 'Дугују ти',
    settled: 'Измирено',
  },
  overview: {
    title: 'Преглед',
    addExpense: 'Додај трошак',
    createGroup: 'Направи групу',
    emptyTitle: 'Још нема трошкова',
    emptyText:
      'Додај свој први трошак или направи групу са цимерима и пријатељима. Овдје ће се онда појавити потрошња, буџети и ко коме дугује.',
  },
  placeholder: {
    text: 'Ова страница стиже ускоро.',
    back: 'Назад на преглед',
  },
  notFound: {
    title: 'Страница не постоји',
    text: 'Адреса је можда погрешно укуцана или је страница премјештена.',
    back: 'Назад на преглед',
  },
};

export type Translation = typeof srCyrl;
