import { useEmployees } from '@absence-management/employees-data-access';
import { useTranslation } from '@absence-management/shared-i18n';
import { EmployeeTable } from '@absence-management/shared-ui';
import { Stack, Title } from '@mantine/core';

/**
 * Only the approver application mounts this page: it asks data-access for the employees and hands
 * them to the presentational table. Everything between the request and the markup lives in one
 * file.
 */
export function EmployeesPage() {
  const { t } = useTranslation();
  const { data, isPending, error } = useEmployees();

  return (
    <Stack gap="sm">
      <Title order={3}>{t('employees.title')}</Title>
      <EmployeeTable employees={data ?? []} isLoading={isPending} errorMessage={error?.message} />
    </Stack>
  );
}
