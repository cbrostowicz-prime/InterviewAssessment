const moneyFmt = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

export function formatMoney(value: number): string {
  return moneyFmt.format(value);
}

export function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString();
}
