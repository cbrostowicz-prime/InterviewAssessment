import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cancelOrder } from '../api/orders';

export function useCancelOrder(userId: number | null) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (orderId: number) => cancelOrder(orderId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['orders', userId] });
    },
  });
}
