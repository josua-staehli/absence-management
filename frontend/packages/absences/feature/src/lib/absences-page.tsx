import {
  type AbsenceRequestDto,
  useAbsenceRequests,
  useApproveAbsenceRequest,
  useRejectAbsenceRequest,
} from '@absence-management/absences-data-access';
import { useTranslation } from '@absence-management/shared-i18n';
import { AbsenceRequestTable } from '@absence-management/shared-ui';
import { Alert, Button, Group, Modal, Stack, Title } from '@mantine/core';
import { useState } from 'react';

import { AbsenceRequestForm } from './absence-request-form';

/**
 * The page both applications mount: it asks data-access for the requests and hands them to the
 * presentational table. The form is shown in a dialog, which keeps list and form on one screen
 * and saves a router for two views.
 */

/**
 * Which actions the page offers. The employee application passes the first two, the approver
 * application the last one - the same page, two roles, without a permission system.
 */
export interface AbsencesPageProps {
  /** Whether the "new request" button is shown. */
  canAddRequest?: boolean;
  /** Whether an open request can be edited. */
  canEditRequest?: boolean;
  /** Whether an open request can be approved or rejected. */
  canDecideRequest?: boolean;
}

export function AbsencesPage({
  canAddRequest = false,
  canEditRequest = false,
  canDecideRequest = false,
}: AbsencesPageProps) {
  const { t } = useTranslation();
  const { data, isPending, error } = useAbsenceRequests();
  const approve = useApproveAbsenceRequest();
  const reject = useRejectAbsenceRequest();

  /** The request being edited, `undefined` for a new one, `null` while the dialog is closed. */
  const [editedRequest, setEditedRequest] = useState<AbsenceRequestDto | undefined | null>(null);

  // A decision the backend refused, e.g. because somebody else decided the request first.
  const decisionError = approve.error ?? reject.error;
  const isDeciding = approve.isPending || reject.isPending;

  return (
    <Stack gap="sm">
      <Group justify="space-between">
        <Title order={3}>{t('absences.title')}</Title>
        {canAddRequest && (
          <Button onClick={() => setEditedRequest(undefined)}>{t('absences.newRequest')}</Button>
        )}
      </Group>

      {decisionError && (
        <Alert color="red" title={t('absences.decisionFailed')}>
          {decisionError.message}
        </Alert>
      )}

      <AbsenceRequestTable
        requests={data ?? []}
        isLoading={isPending}
        errorMessage={error?.message}
        isBusy={isDeciding}
        onEdit={canEditRequest ? setEditedRequest : undefined}
        onApprove={canDecideRequest ? (request) => approve.mutate(request.id) : undefined}
        onReject={canDecideRequest ? (request) => reject.mutate(request.id) : undefined}
      />

      <Modal
        // The dialog unmounts its content when it closes, so the form starts empty every time.
        opened={editedRequest !== null}
        onClose={() => setEditedRequest(null)}
        title={editedRequest ? t('absences.editRequest') : t('absences.newRequest')}
      >
        <AbsenceRequestForm
          request={editedRequest ?? undefined}
          onSaved={() => setEditedRequest(null)}
        />
      </Modal>
    </Stack>
  );
}
