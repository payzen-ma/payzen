import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface InviteAdminRequest {
  email: string;
  companyId: number;
  roleId: number;
}

export interface InviteAdminResponse {
  message?: string;
  token?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InvitationService {
  private readonly baseUrl = 'https://api-test.payzenhr.com';
  private readonly apiUrl = `${this.baseUrl}/api/invitations`;

  constructor(private readonly http: HttpClient) { }

  inviteAdmin(payload: InviteAdminRequest): Observable<InviteAdminResponse> {
    // Backend accepte PascalCase/CamelCase via JSON binder, on garde camelCase ici.
    return this.http.post<InviteAdminResponse>(`${this.apiUrl}/invite-admin`, payload);
  }
}

