export type OrderStatus =
  | 'Placed'
  | 'Paid'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Refunded';

const STATUS_NAMES: OrderStatus[] = [
  'Placed',
  'Paid',
  'Shipped',
  'Delivered',
  'Cancelled',
  'Refunded',
];

export function statusName(value: number): OrderStatus {
  return STATUS_NAMES[value] ?? 'Placed';
}

export type User = {
  id: number;
  name: string;
  email: string;
};

export type OrderSummary = {
  id: number;
  userId: number;
  total: number;
  status: number;
  createdAt: string;
};

export type PagedOrders = {
  items: OrderSummary[];
  page: number;
  pageSize: number;
  total: number;
};

export type UserStats = {
  userId: number;
  orderCount: number;
  lifetimeSpend: number;
};
