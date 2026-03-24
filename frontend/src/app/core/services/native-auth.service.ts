import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

@Injectable({ providedIn: 'root' })
export class NativeAuthService {
  private readonly api = `${environment.apiUrl}/auth/native`;
  private readonly http = inject(HttpClient);

  signIn(email: string, password: string): Observable<any> {
    return this.http.post(`${this.api}/signin`, { email, password });
  }

  signUp(email: string, password: string, invitationToken?: string): Observable<any> {
    return this.http.post(`${this.api}/signup`, { email, password, invitationToken });
  }
}