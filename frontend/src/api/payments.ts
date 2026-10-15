import { apiRequest } from './client';
import type { AddPaymentCardRequest, ParsedQr, PaymentCard, PaymentResult, QrPayload } from './types';

const settlement = (groupId: string, settlementId: string) => `/groups/${groupId}/settlements/${settlementId}`;

/** QrController - IPS QR code of a settlement and reading a scanned code. */
export const qrApi = {
  /** Address of the PNG (needs the token, see useProtectedImage). */
  imageUrl: (groupId: string, settlementId: string) => `/api${settlement(groupId, settlementId)}/qr`,

  payload: (groupId: string, settlementId: string, signal?: AbortSignal) =>
    apiRequest<QrPayload>(`${settlement(groupId, settlementId)}/qr/payload`, { signal }),

  parse: (payload: string) => apiRequest<ParsedQr>('/qr/parse', { method: 'POST', body: { payload } }),
};

/** PaymentCardController and PaymentController - the payment simulator behind the backend. */
export const paymentsApi = {
  cards: (signal?: AbortSignal) => apiRequest<PaymentCard[]>('/payment-cards', { signal }),

  addCard: (request: AddPaymentCardRequest) =>
    apiRequest<PaymentCard>('/payment-cards', { method: 'POST', body: request }),

  removeCard: (cardId: string) => apiRequest<void>(`/payment-cards/${cardId}`, { method: 'DELETE' }),

  /**
   * One idempotency key per payment attempt. If the request has to be repeated (network
   * error), the SAME key is sent again and the backend returns the first result instead of
   * charging a second time.
   */
  payWithCard: (groupId: string, settlementId: string, paymentCardId: string, idempotencyKey: string) =>
    apiRequest<PaymentResult>(`${settlement(groupId, settlementId)}/pay`, {
      method: 'POST',
      body: { paymentCardId, idempotencyKey },
    }),

  /** Current state of the card payment (Pending until the simulator's webhook arrives). */
  status: (groupId: string, settlementId: string, signal?: AbortSignal) =>
    apiRequest<PaymentResult>(`${settlement(groupId, settlementId)}/pay`, { signal }),
};
