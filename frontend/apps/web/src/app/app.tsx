import { AbsencesPage } from '@absence-management/absences-feature';
import { useTranslation } from '@absence-management/shared-i18n';
import { AppLayout } from '@absence-management/shared-ui';

/**
 * The shell of the employee application: it picks a layout and mounts the pages of the feature
 * libraries - no Mantine component and no query hook of its own. Requests can be created and
 * edited here; deciding them belongs to the approver application.
 */
export function App() {
  const { t } = useTranslation();

  return (
    <AppLayout title={t('app.employeeTitle')} accentColor="teal">
      <AbsencesPage canAddRequest canEditRequest />
    </AppLayout>
  );
}

export default App;
