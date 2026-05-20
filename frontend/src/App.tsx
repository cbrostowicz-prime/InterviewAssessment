import { useEffect, useState } from 'react';
import { UserPicker } from './components/UserPicker';
import { StatsPanel } from './components/StatsPanel';
import { OrderTable } from './components/OrderTable';
import { Pager } from './components/Pager';
import { useUsers } from './hooks/useUsers';
import { useOrders } from './hooks/useOrders';
import { useStats } from './hooks/useStats';
import { useCancelOrder } from './hooks/useCancelOrder';

const PAGE_SIZE = 5;

export default function App() {
  const usersQuery = useUsers();
  const [userId, setUserId] = useState<number | null>(null);
  const [page, setPage] = useState(1);

  useEffect(() => {
    if (userId === null && usersQuery.data && usersQuery.data.length > 0) {
      setUserId(usersQuery.data[0].id);
    }
  }, [usersQuery.data, userId]);

  const ordersQuery = useOrders(userId, page, PAGE_SIZE);
  const statsQuery = useStats(userId);
  const cancelMutation = useCancelOrder(userId);

  function selectUser(id: number) {
    setUserId(id);
    setPage(1);
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Order history</h1>
        {usersQuery.data && (
          <UserPicker users={usersQuery.data} value={userId} onChange={selectUser} />
        )}
      </header>

      <StatsPanel stats={statsQuery.data} loading={statsQuery.isPending} />

      <section className="space-y-3">
        {ordersQuery.isError && (
          <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">
            Failed to load orders.
          </div>
        )}
        {ordersQuery.data && (
          <>
            <OrderTable
              orders={ordersQuery.data.items}
              onCancel={(id) => cancelMutation.mutate(id)}
              cancellingId={cancelMutation.isPending ? cancelMutation.variables ?? null : null}
            />
            <Pager
              page={ordersQuery.data.page}
              pageSize={ordersQuery.data.pageSize}
              total={ordersQuery.data.total}
              onChange={setPage}
            />
          </>
        )}
      </section>
    </div>
  );
}
