import type { User } from '../api/types';

type Props = {
  users: User[];
  value: number | null;
  onChange: (id: number) => void;
};

export function UserPicker({ users, value, onChange }: Props) {
  return (
    <label className="flex items-center gap-2 text-sm">
      <span className="font-medium">User</span>
      <select
        className="rounded border border-slate-300 bg-white px-2 py-1"
        value={value ?? ''}
        onChange={(e) => onChange(Number(e.target.value))}
      >
        <option value="" disabled>
          Select a user…
        </option>
        {users.map((u) => (
          <option key={u.id} value={u.id}>
            {u.name} (#{u.id})
          </option>
        ))}
      </select>
    </label>
  );
}
