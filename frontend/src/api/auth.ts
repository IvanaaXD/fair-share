import { apiRequest } from './client';
import type {
  AuthResponse,
  EmailRequest,
  LoginRequest,
  MessageResponse,
  Profile,
  RegisterRequest,
  ResetPasswordRequest,
  TokenRequest,
} from './types';

/** One function per backend endpoint used by the authentication pages. */
export const authApi = {
  login: (request: LoginRequest) =>
    apiRequest<AuthResponse>('/auth/login', { method: 'POST', body: request, auth: false }),

  register: (request: RegisterRequest) =>
    apiRequest<MessageResponse>('/auth/register', { method: 'POST', body: request, auth: false }),

  confirmEmail: (request: TokenRequest) =>
    apiRequest<MessageResponse>('/auth/confirm-email', { method: 'POST', body: request, auth: false }),

  resendConfirmation: (request: EmailRequest) =>
    apiRequest<MessageResponse>('/auth/resend-confirmation', { method: 'POST', body: request, auth: false }),

  forgotPassword: (request: EmailRequest) =>
    apiRequest<MessageResponse>('/auth/forgot-password', { method: 'POST', body: request, auth: false }),

  resetPassword: (request: ResetPasswordRequest) =>
    apiRequest<MessageResponse>('/auth/reset-password', { method: 'POST', body: request, auth: false }),

  logout: (refreshToken: string) =>
    apiRequest<void>('/auth/logout', { method: 'POST', body: { refreshToken }, auth: false }),

  getProfile: () => apiRequest<Profile>('/profile'),
};
