// Authentication DTOs
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  email: string;
}

// Source DTOs
export interface Source {
  id: number;
  name: string;
  serverName: string;
  instanceName?: string;
  databaseName: string;
  landingSchema: string;
  connectionUser: string;
  connectionPasswordEncrypted?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SourceCreateDto {
  name: string;
  serverName: string;
  instanceName?: string;
  databaseName: string;
  landingSchema: string;
  connectionUser: string;
  connectionPassword: string;
  isActive: boolean;
}

export interface SourceUpdateDto {
  name: string;
  serverName: string;
  instanceName?: string;
  databaseName: string;
  landingSchema: string;
  connectionUser: string;
  connectionPassword?: string;
  isActive: boolean;
}

export interface TestConnectionResponse {
  success: boolean;
  message: string;
}

// Source Table DTOs
export interface SourceTable {
  id: number;
  sourceId: number;
  schemaName: string;
  tableName: string;
  landingTableName: string;
  isActive: boolean;
  lastSyncAt?: string;
  lastSyncStatus?: string;
  lastSyncMessage?: string;
}

export interface SourceTableUpsertDto {
  id?: number;
  schemaName: string;
  tableName: string;
  landingTableName: string;
  isActive: boolean;
}

// Source Field DTOs
export interface SourceField {
  id: number;
  sourceTableId: number;
  sourceColumnName: string;
  landingColumnName: string;
  sqlDataType: string;
  isBusinessKey: boolean;
  isNullable: boolean;
  ordinalPosition: number;
}

export interface SourceFieldUpsertDto {
  id?: number;
  sourceColumnName: string;
  landingColumnName: string;
  isBusinessKey: boolean;
  ordinalPosition: number;
}

// Available Tables and Fields
export interface AvailableTable {
  schemaName: string;
  tableName: string;
}

export interface AvailableField {
  columnName: string;
  dataType: string;
  isNullable: boolean;
  ordinalPosition: number;
}

// DDL DTOs
export interface DdlResult {
  createLandingTableDdl: string;
  createStagingTableDdl: string;
  alterLandingTableDdl?: string;
}

export interface ApplyDdlRequest {
  applyLanding: boolean;
  applyStaging: boolean;
  applyAlter: boolean;
}

export interface ApplyDdlResponse {
  success: boolean;
  message: string;
  appliedScripts: string[];
}
