import { createBrowserRouter } from 'react-router'
import { AppLayout } from '@/components/layout/AppLayout'
import { NotFoundPage } from '@/components/layout/NotFoundPage'
import { SignInPage } from '@/features/auth/SignInPage'
import { CallDetailsPage } from '@/features/calls/CallDetailsPage'
import { CallHistoryPage } from '@/features/calls/CallHistoryPage'
import { EvaluateCallPage } from '@/features/calls/EvaluateCallPage'
import { OverviewPage } from '@/features/overview/OverviewPage'
import { TemplateBuilderPage } from '@/features/templates/TemplateBuilderPage'
import { TemplatesPage } from '@/features/templates/TemplatesPage'

// Routes from the UI Page Specification. Pages inside AppLayout get the sidebar.
export const router = createBrowserRouter([
  { path: '/login', element: <SignInPage /> },
  {
    element: <AppLayout />,
    children: [
      { index: true, element: <OverviewPage /> },
      { path: 'templates', element: <TemplatesPage /> },
      { path: 'templates/:templateId', element: <TemplateBuilderPage /> }, // "new" or an id
      { path: 'evaluate', element: <EvaluateCallPage /> },
      { path: 'calls', element: <CallHistoryPage /> },
      { path: 'calls/:callId', element: <CallDetailsPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
