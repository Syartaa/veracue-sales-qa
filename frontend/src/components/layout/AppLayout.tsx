import { Outlet, ScrollRestoration } from 'react-router'
import { Sidebar } from './Sidebar'

// Shared shell for every signed-in page: sidebar + content area capped at ~1240px.
export function AppLayout() {
  return (
    <div className="min-h-screen md:flex">
      <Sidebar />
      <main className="min-w-0 flex-1 px-4 py-6 md:px-10 md:py-8">
        <div className="mx-auto max-w-[1240px]">
          <Outlet />
        </div>
      </main>
      <ScrollRestoration />
    </div>
  )
}
