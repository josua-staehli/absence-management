import {
  type AbsenceRequestDto,
  AbsenceType,
  useCreateAbsenceRequest,
  useUpdateAbsenceRequest,
} from '@absence-management/absences-data-access';
import { useEmployees } from '@absence-management/employees-data-access';
import { useTranslation } from '@absence-management/shared-i18n';
import { absenceTypeKey } from '@absence-management/shared-ui';
import { Alert, Button, Group, Select, Stack, Textarea } from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { useForm } from '@mantine/form';

/**
 * One form for both use cases: without a request it creates a new one, with a request it edits
 * that one. The employee cannot be changed while editing - an absence for somebody else is a new
 * request, which is why the update contract has no employee at all.
 *
 * The validation here only catches what can be decided in the browser (something is missing, the
 * end date is before the start date). Everything else - overlapping absences, a request that is
 * no longer open - is decided by the backend and shown as it comes back.
 */

export interface AbsenceRequestFormProps {
  /** The request to edit. Omitted for a new one. */
  request?: AbsenceRequestDto;
  /** Called after the backend accepted the request, e.g. to close the dialog. */
  onSaved: () => void;
}

interface AbsenceRequestFormValues {
  employeeId: string;
  type: string;
  /** ISO dates (YYYY-MM-DD), the format both Mantine and the API use. Empty means not chosen. */
  startDate: string;
  endDate: string;
  comment: string;
}

export function AbsenceRequestForm({ request, onSaved }: AbsenceRequestFormProps) {
  const { t } = useTranslation();
  const { data: employees, isPending: areEmployeesPending } = useEmployees();
  const create = useCreateAbsenceRequest();
  const update = useUpdateAbsenceRequest();

  const form = useForm<AbsenceRequestFormValues>({
    initialValues: {
      employeeId: request?.employeeId ?? '',
      type: request?.type ?? AbsenceType.VACATION,
      startDate: request?.startDate ?? '',
      endDate: request?.endDate ?? '',
      comment: request?.comment ?? '',
    },
    validate: {
      employeeId: (value) => (value ? null : t('absences.validation.employeeRequired')),
      startDate: (value) => (value ? null : t('absences.validation.startDateRequired')),
      endDate: (value, values) => {
        if (!value) return t('absences.validation.endDateRequired');

        // Mirror the date-order rule for immediate feedback.
        return value < values.startDate ? t('absences.validation.endDateBeforeStartDate') : null;
      },
    },
  });

  const employeeOptions = (employees ?? []).map((employee) => ({
    value: employee.id,
    label: `${employee.firstName} ${employee.lastName}`,
  }));

  const absenceTypeOptions = Object.values(AbsenceType).map((type) => ({
    value: type,
    label: t(absenceTypeKey(type)),
  }));

  const mutation = request ? update : create;

  const handleSubmit = form.onSubmit((values) => {
    const editableFields = {
      type: values.type as AbsenceType,
      startDate: values.startDate,
      endDate: values.endDate,
      comment: values.comment.trim() || null,
    };

    if (request) {
      update.mutate({ id: request.id, ...editableFields }, { onSuccess: onSaved });
    } else {
      create.mutate({ employeeId: values.employeeId, ...editableFields }, { onSuccess: onSaved });
    }
  });

  return (
    <form onSubmit={handleSubmit}>
      <Stack gap="sm">
        {mutation.error && (
          <Alert color="red" title={t('absences.saveFailed')}>
            {mutation.error.message}
          </Alert>
        )}

        <Select
          label={t('absences.form.employee')}
          placeholder={
            areEmployeesPending
              ? t('absences.form.employeesLoading')
              : t('absences.form.employeePlaceholder')
          }
          withAsterisk
          searchable
          data={employeeOptions}
          // Rule: the employee of an existing request does not change.
          disabled={request !== undefined}
          {...form.getInputProps('employeeId')}
        />

        <Select
          label={t('absences.form.type')}
          withAsterisk
          allowDeselect={false}
          data={absenceTypeOptions}
          {...form.getInputProps('type')}
        />

        <Group grow align="flex-start">
          <DatePickerInput
            label={t('absences.form.from')}
            placeholder={t('absences.form.datePlaceholder')}
            withAsterisk
            valueFormat={t('formats.date')}
            value={form.values.startDate || null}
            error={form.errors.startDate}
            onChange={(value) => form.setFieldValue('startDate', value ?? '')}
          />
          <DatePickerInput
            label={t('absences.form.to')}
            placeholder={t('absences.form.datePlaceholder')}
            withAsterisk
            valueFormat={t('formats.date')}
            value={form.values.endDate || null}
            error={form.errors.endDate}
            onChange={(value) => form.setFieldValue('endDate', value ?? '')}
          />
        </Group>

        <Textarea
          label={t('absences.form.comment')}
          placeholder={t('absences.form.commentPlaceholder')}
          autosize
          minRows={2}
          {...form.getInputProps('comment')}
        />

        <Group justify="flex-end">
          <Button type="submit" loading={mutation.isPending}>
            {request ? t('absences.actions.save') : t('absences.actions.create')}
          </Button>
        </Group>
      </Stack>
    </form>
  );
}
