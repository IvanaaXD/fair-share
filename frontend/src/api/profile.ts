import { apiRequest } from './client';
import type { ChangePasswordRequest, Profile, UpdateProfileRequest } from './types';

/** ProfileController, plus the password change. */
export const profileApi = {
  update: (request: UpdateProfileRequest) => apiRequest<Profile>('/profile', { method: 'PUT', body: request }),

  /** multipart/form-data with the field "file"; returns the profile with the new image address. */
  uploadImage: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiRequest<Profile>('/profile/image', { method: 'PUT', body: form });
  },

  removeImage: () => apiRequest<void>('/profile/image', { method: 'DELETE' }),

  /** The one place to change if the backend route differs. */
  changePassword: (request: ChangePasswordRequest) =>
    apiRequest<void>('/profile/password', { method: 'POST', body: request }),
};
