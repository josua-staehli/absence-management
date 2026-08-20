/**
 * English, the reference language: its shape is the key type the whole frontend is checked
 * against (see `i18n.ts`), and `de.ts` has to match it exactly.
 *
 * The keys under `absences.type` and `absences.status` are the values of the generated enums, so
 * a label can be looked up with the value that comes out of the API.
 */
export const en = {
  app: {
    employeeTitle: 'Absence Management',
    adminTitle: 'Absence Management – Administration',
    language: 'Language',
  },
  // dayjs format, used wherever a date is shown or picked.
  formats: {
    date: 'MM/DD/YYYY',
  },
  errors: {
    unexpected: 'The request failed.',
  },
  employees: {
    title: 'Employees',
    loadFailed: 'The employees could not be loaded',
    empty: 'No employees yet.',
    firstName: 'First name',
    lastName: 'Last name',
    email: 'Email',
  },
  absences: {
    title: 'Absence requests',
    loadFailed: 'The absence requests could not be loaded',
    decisionFailed: 'The request could not be decided',
    saveFailed: 'The request could not be saved',
    empty: 'No absence requests yet.',
    newRequest: 'New request',
    editRequest: 'Edit request',
    columns: {
      employee: 'Employee',
      type: 'Type',
      from: 'From',
      to: 'To',
      status: 'Status',
      comment: 'Comment',
    },
    actions: {
      edit: 'Edit',
      approve: 'Approve',
      reject: 'Reject',
      create: 'Create request',
      save: 'Save',
    },
    form: {
      employee: 'Employee',
      employeePlaceholder: 'Select an employee',
      employeesLoading: 'Loading …',
      type: 'Absence type',
      from: 'From',
      to: 'To',
      datePlaceholder: 'Select a date',
      comment: 'Comment',
      commentPlaceholder: 'Optional',
    },
    validation: {
      employeeRequired: 'Please select an employee.',
      startDateRequired: 'Please select a start date.',
      endDateRequired: 'Please select an end date.',
      endDateBeforeStartDate: 'The end date must not be before the start date.',
    },
    type: {
      Vacation: 'Vacation',
      Sickness: 'Sickness',
      Training: 'Training',
      Other: 'Other',
    },
    status: {
      Open: 'Open',
      Approved: 'Approved',
      Rejected: 'Rejected',
    },
  },
};
