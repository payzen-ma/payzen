import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

export interface InvitationInfo {
  companyName: string;
  roleName: string;
  maskedEmail: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class InvitationService {
  private http = inject(HttpClient);
  private api = environment.apiUrl;

  validate(token: string): Observable<InvitationInfo> {
    return this.http.get<InvitationInfo>(
      `${this.api}/invitations/validate`, 
      { params: { token } }
    );
  }

  acceptViaIdp(token: string): Observable<any> {
    return this.http.post(`${this.api}/invitations/accept-via-idp`, { token });
  }
}