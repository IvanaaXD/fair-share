import { apiRequest } from './client';
import type {
  AddGroupMemberRequest,
  CreateGroupRequest,
  Group,
  GroupMember,
  MemberBalance,
  UpdateGroupRequest,
} from './types';

/** One function per endpoint of GroupController, plus the balances from SettlementController. */
export const groupsApi = {
  mine: (signal?: AbortSignal) => apiRequest<Group[]>('/group/mine', { signal }),

  get: (groupId: string, signal?: AbortSignal) => apiRequest<Group>(`/group/${groupId}`, { signal }),

  create: (request: CreateGroupRequest) => apiRequest<Group>('/group', { method: 'POST', body: request }),

  update: (groupId: string, request: UpdateGroupRequest) =>
    apiRequest<Group>(`/group/${groupId}`, { method: 'PUT', body: request }),

  remove: (groupId: string) => apiRequest<void>(`/group/${groupId}`, { method: 'DELETE' }),

  addMember: (groupId: string, request: AddGroupMemberRequest) =>
    apiRequest<GroupMember>(`/group/${groupId}/members`, { method: 'POST', body: request }),

  removeMember: (groupId: string, userId: string) =>
    apiRequest<void>(`/group/${groupId}/members/${userId}`, { method: 'DELETE' }),

  leave: (groupId: string) => apiRequest<void>(`/group/${groupId}/leave`, { method: 'POST' }),

  balances: (groupId: string, signal?: AbortSignal) =>
    apiRequest<MemberBalance[]>(`/group/${groupId}/settlements/balances`, { signal }),
};
