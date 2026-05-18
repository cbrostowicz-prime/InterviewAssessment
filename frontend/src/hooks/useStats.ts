import { useQuery } from '@tanstack/react-query';
import { getStats } from '../api/orders';

export function useStats(userId: number | null) {
  return useQuery({
    queryKey: ['stats', userId],
    queryFn: () => getStats(userId!),
    enabled: userId !== null,
  });
}
