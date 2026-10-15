import { apiRequest, toQueryString } from './client';
import type { ExchangeRate } from './types';

/** ExchangeRateController - today's rate, the same one the server converts with. */
export const exchangeRatesApi = {
  get: (from: string, to: string, signal?: AbortSignal) =>
    apiRequest<ExchangeRate>(`/exchange-rates${toQueryString({ from, to })}`, { signal }),
};
