import {
  type AbsenceRequestDto,
  AbsenceStatus,
  AbsenceType,
} from '@absence-management/shared-api-client';
import type { Meta, StoryObj } from '@storybook/react-vite';

import { AbsenceRequestTable } from './absence-request-table';

/**
 * The states of the table, without an API and without an app around it. The sample rows are
 * typed as AbsenceRequestDto, so a renamed property in the contract fails the typecheck here too.
 */

const requests: AbsenceRequestDto[] = [
  {
    id: '7a2c0d1e-0000-4000-8000-000000000001',
    employeeId: '5f1c0d1e-0000-4000-8000-000000000001',
    employeeName: 'Anna Müller',
    type: AbsenceType.VACATION,
    startDate: '2026-07-06',
    endDate: '2026-07-17',
    status: AbsenceStatus.OPEN,
    comment: 'Sommerferien',
    createdAt: '2026-06-01T08:00:00+02:00',
    updatedAt: null,
  },
  {
    id: '7a2c0d1e-0000-4000-8000-000000000002',
    employeeId: '5f1c0d1e-0000-4000-8000-000000000002',
    employeeName: 'Beat Schneider',
    type: AbsenceType.SICKNESS,
    startDate: '2026-06-02',
    endDate: '2026-06-04',
    status: AbsenceStatus.APPROVED,
    comment: null,
    createdAt: '2026-06-02T07:15:00+02:00',
    updatedAt: '2026-06-02T09:30:00+02:00',
  },
  {
    id: '7a2c0d1e-0000-4000-8000-000000000003',
    employeeId: '5f1c0d1e-0000-4000-8000-000000000003',
    employeeName: 'Chiara Rossi',
    type: AbsenceType.TRAINING,
    startDate: '2026-09-14',
    endDate: '2026-09-15',
    status: AbsenceStatus.REJECTED,
    comment: 'Kurs bereits ausgebucht',
    createdAt: '2026-08-20T11:45:00+02:00',
    updatedAt: '2026-08-21T10:00:00+02:00',
  },
];

const meta: Meta<typeof AbsenceRequestTable> = {
  title: 'Shared UI/AbsenceRequestTable',
  component: AbsenceRequestTable,
  args: { requests },
};

export default meta;

type Story = StoryObj<typeof AbsenceRequestTable>;

/** What the employee application shows: editing an open request, no decisions. */
export const WithRequests: Story = {
  args: { onEdit: () => {} },
};

/** What the approver application shows: the same rows plus the two decisions. */
export const WithDecisions: Story = {
  args: { onApprove: () => {}, onReject: () => {} },
};

export const Empty: Story = {
  args: { requests: [] },
};

export const Loading: Story = {
  args: { isLoading: true },
};

export const Failed: Story = {
  args: { errorMessage: 'Die Anfrage ist fehlgeschlagen.' },
};
