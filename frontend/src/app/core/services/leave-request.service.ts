import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { 
  LeaveRequest, 
  LeaveRequestCreateDto, 
  LeaveRequestCreateForEmployeeDto,
  LeaveRequestPatchDto, 
  LeaveRequestStatus,
  ApprovalDto 
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class LeaveRequestService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/leave-requests`;

  // Récupérer toutes les demandes avec filtres
  getAll(companyId?: number, employeeId?: number, status?: LeaveRequestStatus): Observable<LeaveRequest[]> {
    let params = new URLSearchParams();
    if (companyId) params.append('companyId', companyId.toString());
    if (employeeId) params.append('employeeId', employeeId.toString());
    if (status !== undefined) params.append('status', status.toString());

    const url = params.toString() ? `${this.apiUrl}?${params}` : this.apiUrl;
    
    return this.http.get<any>(url).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((item: any) => this.mapLeaveRequestFromDto(item));
      }),
    );
  }

  // Récupérer une demande par ID
  getById(id: number): Observable<LeaveRequest> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Récupérer les demandes d'un employé
  getByEmployee(employeeId: number): Observable<LeaveRequest[]> {
    return this.http.get<any>(`${this.apiUrl}/employee/${employeeId}`).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((item: any) => this.mapLeaveRequestFromDto(item));
      })
    );
  }

  // Récupérer les demandes en attente d'approbation
  getPendingApproval(companyId?: number): Observable<LeaveRequest[]> {
    const url = companyId ? `${this.apiUrl}/pending-approval?companyId=${companyId}` : `${this.apiUrl}/pending-approval`;
    return this.http.get<any>(url).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((item: any) => this.mapLeaveRequestFromDto(item));
      })
    );
  }

  // Créer une nouvelle demande
  create(dto: LeaveRequestCreateDto): Observable<LeaveRequest> {
    return this.http.post<any>(this.apiUrl, dto).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Créer une demande pour un employé (RH/Manager)
  createForEmployee(employeeId: number, dto: LeaveRequestCreateForEmployeeDto): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/create-for-employee/${employeeId}`, dto);
  }

  // Mettre à jour une demande
  update(id: number, dto: LeaveRequestPatchDto): Observable<LeaveRequest> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, dto).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Soumettre une demande
  submit(id: number): Observable<LeaveRequest> {
    return this.http.post<any>(`${this.apiUrl}/${id}/submit`, {}).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Approuver une demande
  approve(id: number, dto: ApprovalDto): Observable<LeaveRequest> {
    return this.http.post<any>(`${this.apiUrl}/${id}/approve`, dto).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Rejeter une demande
  reject(id: number, dto: ApprovalDto): Observable<LeaveRequest> {
    return this.http.post<any>(`${this.apiUrl}/${id}/reject`, dto).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Annuler une demande
  cancel(id: number, dto: ApprovalDto): Observable<LeaveRequest> {
    return this.http.post<any>(`${this.apiUrl}/${id}/cancel`, dto).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Renoncer à une demande
  renounce(id: number): Observable<LeaveRequest> {
    return this.http.post<any>(`${this.apiUrl}/${id}/renounce`, {}).pipe(
      map(item => this.mapLeaveRequestFromDto(item))
    );
  }

  // Supprimer une demande
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Mapping helper pour convertir les DTOs backend en modèles frontend
  private mapLeaveRequestFromDto(dto: any): LeaveRequest {
    if (!dto) return null as any;
    const get = (camel: string, pascal: string) => dto?.[camel] ?? dto?.[pascal];
    const getBool = (camel: string, pascal: string) => Boolean(get(camel, pascal));
    const getNum = (camel: string, pascal: string, def: number = 0) => Number(get(camel, pascal)) || def;
    const getDate = (camel: string, pascal: string) => {
      const value = get(camel, pascal);
      return value ? new Date(value) : null;
    };
    const getDateOnly = (camel: string, pascal: string) => {
      const value = get(camel, pascal);
      if (!value) return null;
      // Créer une date locale pour éviter les problèmes de fuseau horaire
      if (typeof value === 'string') {
        const parts = value.split('-');
        if (parts.length === 3) {
          return new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
        }
      }
      return new Date(value);
    };

    // Mapping correct du status depuis l'API
    const getStatus = (camel: string, pascal: string): LeaveRequestStatus => {
      const statusValue = get(camel, pascal);
      
      // Si c'est déjà un nombre, on l'utilise directement
      if (typeof statusValue === 'number') {
        return statusValue as LeaveRequestStatus;
      }
      
      // Si c'est une chaîne, on fait le mapping
      if (typeof statusValue === 'string') {
        const statusMap: Record<string, LeaveRequestStatus> = {
          'Draft': LeaveRequestStatus.Draft,
          'Submitted': LeaveRequestStatus.Submitted,
          'Approved': LeaveRequestStatus.Approved,
          'Rejected': LeaveRequestStatus.Rejected,
          'Cancelled': LeaveRequestStatus.Cancelled,
          'Renounced': LeaveRequestStatus.Renounced
        };
        return statusMap[statusValue] ?? LeaveRequestStatus.Draft;
      }
      
      return LeaveRequestStatus.Draft;
    };

    return {
      id: get('id', 'Id'),
      employeeId: get('employeeId', 'EmployeeId'),
      leaveTypeId: get('leaveTypeId', 'LeaveTypeId'),
      startDate: getDateOnly('startDate', 'StartDate') || new Date(),
      endDate: getDateOnly('endDate', 'EndDate') || new Date(),
      reason: get('employeeNote', 'EmployeeNote') || get('reason', 'Reason') || '',
      status: getStatus('status', 'Status'),
      createdAt: getDate('createdAt', 'CreatedAt') || new Date(),
      updatedAt: getDate('updatedAt', 'UpdatedAt') || getDate('createdAt', 'CreatedAt') || new Date(),
      
      // Navigation properties (optional)
      employee: undefined,
      leaveType: undefined,
      
      // Calculated properties
      durationDays: getNum('calendarDays', 'CalendarDays'),
      workingDaysDeducted: getNum('workingDaysDeducted', 'WorkingDaysDeducted'),
      statusLabel: undefined
    } as LeaveRequest;
  }
}