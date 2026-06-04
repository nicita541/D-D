import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { ResourceManager } from './management';

it('submits a normal resource form through the create operation', async () => {
  const create = vi.fn(async () => ({ id: 'new' }));
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <ResourceManager
        title="Локации"
        queryKey={['locations']}
        list={async () => []}
        create={create}
        defaultValue={{ name: '', description: '' }}
      />
    </QueryClientProvider>,
  );

  fireEvent.change(await screen.findByLabelText('Имя / название'), { target: { value: 'Старая дорога' } });
  fireEvent.click(screen.getByRole('button', { name: 'Создать' }));
  await waitFor(() => expect(create).toHaveBeenCalledWith(expect.objectContaining({ name: 'Старая дорога' })));
});
