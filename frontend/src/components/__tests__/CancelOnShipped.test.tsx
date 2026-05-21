import { useState } from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AxiosError, type AxiosResponse } from 'axios';
import { apiClient } from '../../api/client';
import { OrderTable } from '../OrderTable';
import { useCancelOrder } from '../../hooks/useCancelOrder';
import type { OrderSummary } from '../../api/types';

// ORD-1671 — Cancelling a Shipped order silently fails.
//
// The backend correctly returns 409 Conflict for a cancel on a Shipped
// order, but the frontend gives the user no feedback at all: the spinner
// stops and the order row is unchanged. The fix can land in two places:
//
//   (a) The Cancel button is disabled for Shipped (matches the existing
//       Delivered / Cancelled / Refunded handling).
//   (b) A 409 from POST /orders/{id}/cancel surfaces a visible error
//       message (toast, banner, inline, anything role="alert" or text
//       that mentions "cannot cancel" / "could not cancel" / similar).
//
// Either is an acceptable fix; both is the strongest. This test passes
// if EITHER condition holds and fails if NEITHER does (today's state).

const SHIPPED: OrderSummary = {
  id: 4242,
  userId: 101,
  total: 129.95,
  status: 2, // Shipped
  createdAt: '2026-05-10T12:00:00Z',
};

function CancelHarness() {
  const cancelMutation = useCancelOrder(101);
  const [orders] = useState<OrderSummary[]>([SHIPPED]);
  return (
    <OrderTable
      orders={orders}
      onCancel={(id) => cancelMutation.mutate(id)}
      cancellingId={cancelMutation.isPending ? cancelMutation.variables ?? null : null}
    />
  );
}

function renderHarness() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={qc}>
      <CancelHarness />
    </QueryClientProvider>,
  );
}

function makeConflictError(): AxiosError {
  const response = {
    status: 409,
    statusText: 'Conflict',
    data: { error: 'Shipped or delivered orders cannot be cancelled.' },
    headers: {},
    config: { headers: {} },
  } as unknown as AxiosResponse;
  const err = new AxiosError(
    'Request failed with status code 409',
    'ERR_BAD_REQUEST',
    undefined,
    undefined,
    response,
  );
  return err;
}

describe('Cancel on a Shipped order', () => {
  it('either disables the Cancel button or surfaces a visible error when the API rejects with 409', async () => {
    const postSpy = vi.spyOn(apiClient, 'post').mockRejectedValue(makeConflictError());

    renderHarness();

    const cancelButton = screen.getByRole('button', { name: /cancel/i });

    if ((cancelButton as HTMLButtonElement).disabled) {
      // Fix (a): button disabled for Shipped. Nothing else to assert.
      expect(postSpy).not.toHaveBeenCalled();
      return;
    }

    // Button is enabled today — the only acceptable outcome is that
    // clicking it surfaces a visible error to the user.
    fireEvent.click(cancelButton);

    await waitFor(() => expect(postSpy).toHaveBeenCalledTimes(1));

    await waitFor(
      () => {
        const alert = screen.queryByRole('alert');
        const inlineMessage = screen.queryByText(
          /cannot be cancel|cannot cancel|could not cancel|couldn[’']?t cancel|failed to cancel|unable to cancel|already shipped|shipped or delivered/i,
        );
        expect(alert ?? inlineMessage).not.toBeNull();
      },
      { timeout: 2000 },
    );
  });
});
