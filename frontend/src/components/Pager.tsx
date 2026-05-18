type Props = {
  page: number;
  pageSize: number;
  total: number;
  onChange: (page: number) => void;
};

export function Pager({ page, pageSize, total, onChange }: Props) {
  const lastPage = Math.max(1, Math.ceil(total / pageSize));
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);
  return (
    <div className="flex items-center justify-between text-sm">
      <div className="text-slate-500">
        {from}–{to} of {total}
      </div>
      <div className="flex gap-2">
        <button
          className="rounded border border-slate-300 px-3 py-1 disabled:opacity-50"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
        >
          Previous
        </button>
        <button
          className="rounded border border-slate-300 px-3 py-1 disabled:opacity-50"
          disabled={page >= lastPage}
          onClick={() => onChange(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
