import { fireEvent, render, screen } from '@testing-library/react';
import { Pager } from '../Pager';

describe('Pager', () => {
  it('disables Previous on first page', () => {
    render(<Pager page={1} pageSize={10} total={30} onChange={() => {}} />);
    expect(screen.getByRole('button', { name: /previous/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /next/i })).toBeEnabled();
  });

  it('disables Next on last page', () => {
    render(<Pager page={3} pageSize={10} total={30} onChange={() => {}} />);
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();
  });

  it('renders the row range', () => {
    render(<Pager page={2} pageSize={10} total={30} onChange={() => {}} />);
    expect(screen.getByText('11–20 of 30')).toBeInTheDocument();
  });

  it('does not emit a page beyond the last page when Next is clicked on the last page', () => {
    const onChange = vi.fn();
    const total = 10;
    const pageSize = 5;
    const page = 2;
    const lastPage = Math.ceil(total / pageSize);

    render(<Pager page={page} pageSize={pageSize} total={total} onChange={onChange} />);
    fireEvent.click(screen.getByRole('button', { name: /next/i }));

    const overshoots = onChange.mock.calls
      .map((args) => args[0] as number)
      .filter((p) => p > lastPage);
    expect(overshoots, `Pager emitted page(s) past lastPage=${lastPage}: ${overshoots.join(', ')}`).toEqual([]);
  });
});
