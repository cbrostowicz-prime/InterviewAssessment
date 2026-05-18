import { apiClient } from './client';
import type { PagedOrders, UserStats } from './types';

export async function listOrders(params: {
  userId: number;
  page: number;
  pageSize: number;
}): Promise<PagedOrders> {
  const { data } = await apiClient.get<PagedOrders>('/orders', { params });
  return data;
}

export async function cancelOrder(orderId: number): Promise<void> {
  await apiClient.post(`/orders/${orderId}/cancel`);
}

export async function getStats(userId: number): Promise<UserStats> {
  const { data } = await apiClient.get<UserStats>(`/users/${userId}/stats`);
  return data;
}
