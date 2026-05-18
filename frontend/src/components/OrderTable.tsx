import { statusName, type OrderSummary } from '../api/types';
import { formatDate, formatMoney } from '../lib/format';

type Props = {
  orders: OrderSummary[];
  onCancel: (orderId: number) => void;
  cancellingId: number | null;
};

const TERMINAL_STATUSES = new Set([3, 4, 5]); // Delivered, Cancelled, Refunded

export function OrderTable({ orders, onCancel, cancellingId }: Props) {
  if (orders.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
        No orders to show.
      </div>
    );
  }
  return (
    <table className="w-full overflow-hidden rounded-lg border border-slate-200 bg-white text-sm">
      <thead className="bg-slate-100 text-left">
        <tr>
          <th className="px-3 py-2 font-medium">Order #</th>
          <th className="px-3 py-2 font-medium">Created</th>
          <th className="px-3 py-2 font-medium">Status</th>
          <th className="px-3 py-2 text-right font-medium">Total</th>
          <th className="px-3 py-2" />
        </tr>
      </thead>
      <tbody>
        {orders.map((o) => {
          const cancellable = !TERMINAL_STATUSES.has(o.status);
          return (
            <tr key={o.id} className="border-t border-slate-100">
              <td className="px-3 py-2 font-mono text-slate-700">{o.id}</td>
              <td className="px-3 py-2 text-slate-600">{formatDate(o.createdAt)}</td>
              <td className="px-3 py-2">{statusName(o.status)}</td>
              <td className="px-3 py-2 text-right tabular-nums">{formatMoney(o.total)}</td>
              <td className="px-3 py-2 text-right">
                <button
                  className="rounded border border-slate-300 px-2 py-1 text-xs hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                  disabled={!cancellable || cancellingId === o.id}
                  onClick={() => onCancel(o.id)}
                >
                  {cancellingId === o.id ? 'Cancelling…' : 'Cancel'}
                </button>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
