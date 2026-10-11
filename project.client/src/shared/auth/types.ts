export interface AuthUser {
  id: string;
  userName: string;
  email: string;
  tenantId: string | null;
  organizationUnitId: string | null;
  mustChangePassword: boolean;
  roles: ReadonlyArray<string>;
}

export interface AuthSession {
  user: AuthUser;
  permissions: ReadonlyArray<string>;
  expiresAt: string;
}
