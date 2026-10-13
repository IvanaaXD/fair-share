import { useCallback, useEffect, useState } from 'react';
import { paymentsApi } from '../../api/payments';
import type { PaymentCard } from '../../api/types';

/** The signed-in user's saved cards. */
export function useCards() {
  const [version, setVersion] = useState(0);
  const [state, setState] = useState<{ version: number; cards: PaymentCard[]; error: unknown }>({
    version: -1,
    cards: [],
    error: null,
  });

  useEffect(() => {
    const controller = new AbortController();
    paymentsApi.cards(controller.signal).then(
      (cards) => setState({ version, cards, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setState((current) => ({ ...current, version, error }));
      },
    );
    return () => controller.abort();
  }, [version]);

  const reload = useCallback(() => setVersion((value) => value + 1), []);
  return { cards: state.cards, error: state.error, loading: state.version !== version, reload };
}
