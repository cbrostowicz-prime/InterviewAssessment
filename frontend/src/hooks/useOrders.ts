import { useQuery } from '@tanstack/react-query';
import { listOrders } from '../api/orders';

export function useOrders(userId: number | null, page: number, pageSize: number) {
  return useQuery({
    queryKey: ['orders', userId, page, pageSize],
    queryFn: () => listOrders({ userId: userId!, page, pageSize }),
    enabled: userId !== null,
    placeholderData: (previous) => previous,
  });
}
