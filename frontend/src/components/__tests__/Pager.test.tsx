import { render, screen } from '@testing-library/react';
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
});
