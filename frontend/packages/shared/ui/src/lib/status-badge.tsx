import type { AbsenceStatus } from '@absence-management/shared-api-client';
import { useTranslation } from '@absence-management/shared-i18n';
import { Badge } from '@mantine/core';

import { absenceStatusColors, absenceStatusKey } from './absence-labels';

/** The status of a request as a coloured badge - the whole visual status display. */
export function StatusBadge({ status }: { status: AbsenceStatus }) {
  const { t } = useTranslation();

  return (
    <Badge color={absenceStatusColors[status]} variant="light">
      {t(absenceStatusKey(status))}
    </Badge>
  );
}
