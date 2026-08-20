import type { EmployeeDto } from '@absence-management/shared-api-client';
import { useTranslation } from '@absence-management/shared-i18n';
import { Alert, Center, Loader, Table, Text } from '@mantine/core';

/**
 * A presentational table - it renders what it is handed and makes no request of its own. Same
 * shape as AbsenceRequestTable: loading and error are props rather than components of their own,
 * so a page that shows employees is one element and cannot forget either state.
 */

export interface EmployeeTableProps {
  employees: readonly EmployeeDto[];
  /** While true the rows are replaced by a spinner. */
  isLoading?: boolean;
  /** When set the rows are replaced by the message - the caller decides what to say. */
  errorMessage?: string;
}

export function EmployeeTable({ employees, isLoading = false, errorMessage }: EmployeeTableProps) {
  const { t } = useTranslation();

  if (isLoading) {
    return (
      <Center py="xl">
        <Loader size="sm" />
      </Center>
    );
  }

  if (errorMessage !== undefined) {
    return (
      <Alert color="red" title={t('employees.loadFailed')}>
        {errorMessage}
      </Alert>
    );
  }

  if (employees.length === 0) {
    return (
      <Text c="dimmed" py="md">
        {t('employees.empty')}
      </Text>
    );
  }

  return (
    <Table striped highlightOnHover>
      <Table.Thead>
        <Table.Tr>
          <Table.Th>{t('employees.firstName')}</Table.Th>
          <Table.Th>{t('employees.lastName')}</Table.Th>
          <Table.Th>{t('employees.email')}</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {employees.map((employee) => (
          <Table.Tr key={employee.id}>
            <Table.Td>{employee.firstName}</Table.Td>
            <Table.Td>{employee.lastName}</Table.Td>
            <Table.Td>{employee.email}</Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}
