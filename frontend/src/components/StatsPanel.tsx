import type { UserStats } from '../api/types';
import { formatMoney } from '../lib/format';

type Props = {
  stats: UserStats | undefined;
  loading: boolean;
};

export function StatsPanel({ stats, loading }: Props) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">
        Lifetime stats
      </h2>
      {loading || !stats ? (
        <div className="mt-2 h-10 animate-pulse rounded bg-slate-100" />
      ) : (
        <div className="mt-2 flex gap-8">
          <div>
            <div className="text-xs text-slate-500">Orders</div>
            <div className="text-2xl font-semibold">{stats.orderCount}</div>
          </div>
          <div>
            <div className="text-xs text-slate-500">Lifetime spend</div>
            <div className="text-2xl font-semibold">{formatMoney(stats.lifetimeSpend)}</div>
          </div>
        </div>
      )}
    </div>
  );
}
