import { apiClient } from './client';
import type { User } from './types';

export async function listUsers(): Promise<User[]> {
  const { data } = await apiClient.get<User[]>('/users');
  return data;
}
