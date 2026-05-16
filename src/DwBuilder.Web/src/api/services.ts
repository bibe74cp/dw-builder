import apiClient from './axios';
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  Source,
  SourceCreateDto,
  SourceUpdateDto,
  TestConnectionResponse,
  SourceTable,
  SourceTableUpsertDto,
  SourceField,
  SourceFieldUpsertDto,
  AvailableTable,
  AvailableField,
  DdlResult,
  ApplyDdlRequest,
  ApplyDdlResponse,
} from '@/types/api';

// Auth Service
export const authService = {
  login: (data: LoginRequest) => apiClient.post<LoginResponse>('/auth/login', data),
  register: (data: RegisterRequest) => apiClient.post<LoginResponse>('/auth/register', data),
};

// Source Service
export const sourceService = {
  getAll: () => apiClient.get<Source[]>('/sources'),
  getById: (id: number) => apiClient.get<Source>(`/sources/${id}`),
  create: (data: SourceCreateDto) => apiClient.post<Source>('/sources', data),
  update: (id: number, data: SourceUpdateDto) => apiClient.put<Source>(`/sources/${id}`, data),
  delete: (id: number) => apiClient.delete(`/sources/${id}`),
  testConnection: (id: number) => apiClient.post<TestConnectionResponse>(`/sources/${id}/test-connection`),
  getAvailableTables: (id: number) => apiClient.get<AvailableTable[]>(`/sources/${id}/available-tables`),
};

// Source Table Service
export const sourceTableService = {
  getBySourceId: (sourceId: number) => apiClient.get<SourceTable[]>(`/source-tables/sources/${sourceId}/tables`),
  bulkUpsert: (sourceId: number, tables: SourceTableUpsertDto[]) => 
    apiClient.put(`/source-tables/sources/${sourceId}/tables`, tables),
  getAvailableFields: (tableId: number) => apiClient.get<AvailableField[]>(`/source-tables/${tableId}/available-fields`),
  getFields: (tableId: number) => apiClient.get<SourceField[]>(`/source-tables/${tableId}/fields`),
  bulkUpsertFields: (tableId: number, fields: SourceFieldUpsertDto[]) => 
    apiClient.put(`/source-tables/${tableId}/fields`, fields),
};

// DDL Service
export const ddlService = {
  generate: (sourceId: number, tableId: number) => 
    apiClient.get<DdlResult>(`/ddl/sources/${sourceId}/tables/${tableId}/ddl`),
  apply: (sourceId: number, tableId: number, options: ApplyDdlRequest) => 
    apiClient.post<ApplyDdlResponse>(`/ddl/sources/${sourceId}/tables/${tableId}/apply-ddl`, options),
};

// BIML Service
export const bimlService = {
  download: () => apiClient.get('/biml', { responseType: 'blob' }),
};
