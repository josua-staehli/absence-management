import type { AbsenceStatus, AbsenceType } from '@absence-management/shared-api-client';
import type { MantineColor } from '@mantine/core';

/**
 * The two enums as translation keys and colours. The wording itself lives in
 * `@absence-management/shared-i18n`; what belongs here is the mapping. Both directions are
 * checked: the key functions return a literal type, so a value without a translation fails at the
 * `t()` call, and `absenceStatusColors` is keyed by the generated enum, so a new status has to be
 * given a colour before it compiles.
 */

export function absenceTypeKey(type: AbsenceType) {
  return `absences.type.${type}` as const;
}

export function absenceStatusKey(status: AbsenceStatus) {
  return `absences.status.${status}` as const;
}

export const absenceStatusColors: Record<AbsenceStatus, MantineColor> = {
  Open: 'blue',
  Approved: 'green',
  Rejected: 'red',
};
