import { type AbsenceRequestDto, AbsenceStatus } from '@absence-management/shared-api-client';
import { useTranslation } from '@absence-management/shared-i18n';
import { Alert, Button, Center, Group, Loader, Table, Text } from '@mantine/core';
import dayjs from 'dayjs';

import { absenceTypeKey } from './absence-labels';
import { StatusBadge } from './status-badge';

/**
 * A presentational table - it renders what it is handed and makes no request of its own. Same
 * shape as EmployeeTable: loading and error are props, so a page cannot forget either state.
 *
 * The three actions are optional callbacks. An action that is not passed is not rendered, which
 * is how the two applications differ: the approver application passes onApprove and onReject,
 * the employee application does not. They are offered for open requests only - deciding or
 * editing a decided request is refused by the backend (rules 6 and 9).
 */

export interface AbsenceRequestTableProps {
  requests: readonly AbsenceRequestDto[];
  /** While true the rows are replaced by a spinner. */
  isLoading?: boolean;
  /** When set the rows are replaced by the message - the caller decides what to say. */
  errorMessage?: string;
  onEdit?: (request: AbsenceRequestDto) => void;
  onApprove?: (request: AbsenceRequestDto) => void;
  onReject?: (request: AbsenceRequestDto) => void;
  /** While true the actions are disabled, e.g. because a decision is still being saved. */
  isBusy?: boolean;
}

export function AbsenceRequestTable({
  requests,
  isLoading = false,
  errorMessage,
  onEdit,
  onApprove,
  onReject,
  isBusy = false,
}: AbsenceRequestTableProps) {
  const { t } = useTranslation();
  const formatDate = (date: string) => dayjs(date).format(t('formats.date'));

  if (isLoading) {
    return (
      <Center py="xl">
        <Loader size="sm" />
      </Center>
    );
  }

  if (errorMessage !== undefined) {
    return (
      <Alert color="red" title={t('absences.loadFailed')}>
        {errorMessage}
      </Alert>
    );
  }

  if (requests.length === 0) {
    return (
      <Text c="dimmed" py="md">
        {t('absences.empty')}
      </Text>
    );
  }

  return (
    <Table striped highlightOnHover>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>{t('absences.columns.employee')}</Table.Th>
          <Table.Th>{t('absences.columns.type')}</Table.Th>
          <Table.Th>{t('absences.columns.from')}</Table.Th>
          <Table.Th>{t('absences.columns.to')}</Table.Th>
          <Table.Th>{t('absences.columns.status')}</Table.Th>
          <Table.Th>{t('absences.columns.comment')}</Table.Th>
          <Table.Th />
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {requests.map((request) => {
          const isOpen = request.status === AbsenceStatus.OPEN;

          return (
            <Table.Tr key={request.id}>
              <Table.Td>{request.employeeName}</Table.Td>
              <Table.Td>{t(absenceTypeKey(request.type))}</Table.Td>
              <Table.Td>{formatDate(request.startDate)}</Table.Td>
              <Table.Td>{formatDate(request.endDate)}</Table.Td>
              <Table.Td>
                <StatusBadge status={request.status} />
              </Table.Td>
              <Table.Td>{request.comment}</Table.Td>
              <Table.Td>
                <Group gap="xs" justify="flex-end" wrap="nowrap">
                  {isOpen && onEdit && (
                    <Button
                      size="compact-sm"
                      variant="default"
                      disabled={isBusy}
                      onClick={() => onEdit(request)}
                    >
                      {t('absences.actions.edit')}
                    </Button>
                  )}
                  {isOpen && onApprove && (
                    <Button
                      size="compact-sm"
                      color="green"
                      disabled={isBusy}
                      onClick={() => onApprove(request)}
                    >
                      {t('absences.actions.approve')}
                    </Button>
                  )}
                  {isOpen && onReject && (
                    <Button
                      size="compact-sm"
                      color="red"
                      disabled={isBusy}
                      onClick={() => onReject(request)}
                    >
                      {t('absences.actions.reject')}
                    </Button>
                  )}
                </Group>
              </Table.Td>
            </Table.Tr>
          );
        })}
      </Table.Tbody>
    </Table>
  );
}
