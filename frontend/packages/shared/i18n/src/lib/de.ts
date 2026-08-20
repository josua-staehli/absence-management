import type { en } from './en';

/**
 * German. `satisfies typeof en` is the whole quality gate of this file: a key that is missing
 * here, or one that exists here and not in English, fails `pnpm typecheck`.
 */
export const de = {
  app: {
    employeeTitle: 'Abwesenheitsverwaltung',
    adminTitle: 'Abwesenheitsverwaltung – Administration',
    language: 'Sprache',
  },
  formats: {
    date: 'DD.MM.YYYY',
  },
  errors: {
    unexpected: 'Die Anfrage ist fehlgeschlagen.',
  },
  employees: {
    title: 'Mitarbeiter',
    loadFailed: 'Mitarbeiter konnten nicht geladen werden',
    empty: 'Keine Mitarbeiter vorhanden.',
    firstName: 'Vorname',
    lastName: 'Nachname',
    email: 'E-Mail',
  },
  absences: {
    title: 'Abwesenheitsanträge',
    loadFailed: 'Abwesenheitsanträge konnten nicht geladen werden',
    decisionFailed: 'Der Antrag konnte nicht entschieden werden',
    saveFailed: 'Antrag konnte nicht gespeichert werden',
    empty: 'Keine Abwesenheitsanträge vorhanden.',
    newRequest: 'Neuer Antrag',
    editRequest: 'Antrag bearbeiten',
    columns: {
      employee: 'Mitarbeiter',
      type: 'Typ',
      from: 'Von',
      to: 'Bis',
      status: 'Status',
      comment: 'Kommentar',
    },
    actions: {
      edit: 'Bearbeiten',
      approve: 'Genehmigen',
      reject: 'Ablehnen',
      create: 'Antrag erstellen',
      save: 'Speichern',
    },
    form: {
      employee: 'Mitarbeiter',
      employeePlaceholder: 'Mitarbeiter wählen',
      employeesLoading: 'Wird geladen …',
      type: 'Abwesenheitsart',
      from: 'Von',
      to: 'Bis',
      datePlaceholder: 'Datum wählen',
      comment: 'Kommentar',
      commentPlaceholder: 'Optional',
    },
    validation: {
      employeeRequired: 'Bitte einen Mitarbeiter wählen.',
      startDateRequired: 'Bitte ein Startdatum wählen.',
      endDateRequired: 'Bitte ein Enddatum wählen.',
      endDateBeforeStartDate: 'Das Enddatum darf nicht vor dem Startdatum liegen.',
    },
    type: {
      Vacation: 'Ferien',
      Sickness: 'Krankheit',
      Training: 'Weiterbildung',
      Other: 'Sonstiges',
    },
    status: {
      Open: 'Offen',
      Approved: 'Genehmigt',
      Rejected: 'Abgelehnt',
    },
  },
} satisfies typeof en;
