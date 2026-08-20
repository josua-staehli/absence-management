import { AbsencesPage } from '@absence-management/absences-feature';
import { EmployeesPage } from '@absence-management/employees-feature';
import { useTranslation } from '@absence-management/shared-i18n';
import { AppLayout } from '@absence-management/shared-ui';

/**
 * The shell of the approver application: same shape as the employee application, another accent
 * color, and it is the only kind of project allowed to mount pages of several feature areas.
 * `canDecideRequest` is what the employee application does not pass - here a request can be
 * approved or rejected, but not created or edited.
 */
export function App() {
  const { t } = useTranslation();

  return (
    <AppLayout title={t('app.adminTitle')} accentColor="indigo">
      <AbsencesPage canDecideRequest />
      <EmployeesPage />
    </AppLayout>
  );
}

export default App;
