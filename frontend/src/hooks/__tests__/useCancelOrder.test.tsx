import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { vi } from 'vitest';
import { useCancelOrder } from '../useCancelOrder';

vi.mock('../../api/orders', () => ({
  cancelOrder: vi.fn().mockResolvedValue(undefined),
}));

function makeWrapper(client: QueryClient) {
  return function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
  };
}

describe('useCancelOrder', () => {
  it('invalidates the stats query for the affected user after a successful cancel', async () => {
    const userId = 101;
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    client.setQueryData(['stats', userId], { orderCount: 7, lifetimeSpend: 1234.56 });
    client.setQueryData(['orders', userId, 1, 10], { items: [], page: 1, pageSize: 10, total: 0 });

    const { result } = renderHook(() => useCancelOrder(userId), {
      wrapper: makeWrapper(client),
    });

    await act(async () => {
      await result.current.mutateAsync(42);
    });

    await waitFor(() => {
      expect(client.getQueryState(['stats', userId])?.isInvalidated).toBe(true);
    });
  });
});
