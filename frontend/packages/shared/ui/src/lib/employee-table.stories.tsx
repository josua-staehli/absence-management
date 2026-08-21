import type { EmployeeDto } from '@absence-management/shared-api-client';
import type { Meta, StoryObj } from '@storybook/react-vite';

import { EmployeeTable } from './employee-table';

/**
 * The states of the table, without an API and without an app around it. The sample rows are
 * typed as EmployeeDto, so a renamed property in the contract fails the typecheck here too.
 */

const employees: EmployeeDto[] = [
  {
    id: '5f1c0d1e-0000-4000-8000-000000000001',
    firstName: 'Anna',
    lastName: 'Müller',
    email: 'anna.mueller@example.com',
  },
  {
    id: '5f1c0d1e-0000-4000-8000-000000000002',
    firstName: 'Beat',
    lastName: 'Schneider',
    email: 'beat.schneider@example.com',
  },
  {
    id: '5f1c0d1e-0000-4000-8000-000000000003',
    firstName: 'Chiara',
    lastName: 'Rossi',
    email: 'chiara.rossi@example.com',
  },
];

const meta: Meta<typeof EmployeeTable> = {
  title: 'Shared UI/EmployeeTable',
  component: EmployeeTable,
  args: { employees },
};

export default meta;

type Story = StoryObj<typeof EmployeeTable>;

export const WithEmployees: Story = {};

export const Empty: Story = {
  args: { employees: [] },
};

export const Loading: Story = {
  args: { isLoading: true },
};

export const Failed: Story = {
  args: { errorMessage: 'The request failed.' },
};
