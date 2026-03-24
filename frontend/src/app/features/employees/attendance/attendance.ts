import { Component, OnInit, OnDestroy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { AuthService } from '@app/core/services/auth.service';
import { AttendanceBreakService } from '@app/core/services/attendance-break.service';

interface AttendanceRecord {
  id?: string;
  date: string;
  timeIn: string | null;
  timeOut: string | null;
  status: 'present' | 'absent' | 'holiday' | 'leave' | 'late';
  duration?: string;
  breakMinutesApplied?: number;
  breaks?: Array<{ id?: string; start: string; end: string; type?: string; duration?: string }>;
}

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [CommonModule, TranslateModule, ButtonModule, CardModule, ToastModule],
  providers: [MessageService],
  templateUrl: './attendance.html',
  styleUrls: ['./attendance.css']
})
export class AttendancePage implements OnInit {
  // timer id for live elapsed display
  private timerId: any = null;
  readonly elapsed = signal<string>('0h00');
  readonly elapsedMinutes = signal<number>(0);
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);
  private readonly http = inject(HttpClient);
  private readonly breakService = inject(AttendanceBreakService);

  readonly currentUser = this.authService.currentUser;
  readonly todayAttendance = signal<AttendanceRecord | null>(null);
  readonly attendanceHistory = signal<AttendanceRecord[]>([]);
  readonly isLoading = signal(false);

  readonly todayStatus = computed(() => {
    const today = this.todayAttendance();
    if (!today) return 'notCheckedIn';
    if (today.timeOut) return 'checkedOut';
    if (today.timeIn) return 'checkedIn';
    return 'notCheckedIn';
  });

  readonly canCheckIn = computed(() => this.todayStatus() === 'notCheckedIn');
  readonly canCheckOut = computed(() => this.todayStatus() === 'checkedIn');
  readonly isOnBreak = computed(() => {
    const breaks = this.todayAttendance()?.breaks ?? [];
    if (!breaks.length) return false;
    const last = breaks[breaks.length - 1];
    return !last.end;
  });

  ngOnInit(): void {
    this.loadTodayAttendance();
    this.loadAttendanceHistory();
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  private formatElapsedFromMinutes(totalMinutes: number): string {
    if (totalMinutes < 0) totalMinutes = 0;
    const hours = Math.floor(totalMinutes / 60);
    const minutes = Math.floor(totalMinutes % 60);
    return `${hours}h${String(minutes).padStart(2, '0')}`;
  }

  private updateElapsed(): void {
    const today = this.todayAttendance();
    if (!today || !today.timeIn) {
      this.elapsed.set('0h00');
      return;
    }
    // parse timeIn (HH:MM)
    const parts = today.timeIn.split(':');
    const h = Number(parts[0] ?? 0);
    const m = Number(parts[1] ?? 0);
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), h, m, 0);
    const diffMs = now.getTime() - start.getTime();
    const minutes = Math.floor(diffMs / 60000);
    this.elapsedMinutes.set(minutes);
    this.elapsed.set(this.formatElapsedFromMinutes(minutes));
  }

  private startTimerIfNeeded(): void {
    const today = this.todayAttendance();
    if (today && today.timeIn && !today.timeOut) {
      // start/refresh timer
      this.updateElapsed();
      if (this.timerId) clearInterval(this.timerId);
      this.timerId = setInterval(() => this.updateElapsed(), 1000);
    } else {
      this.stopTimer();
      // if already checked out, set elapsed to recorded duration if present
      if (today?.duration) this.elapsed.set(today.duration);
    }
  }

  private stopTimer(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
      this.timerId = null;
    }
  }

  private formatWorkedHours(value: number | string | undefined): string | undefined {
    if (value === null || value === undefined) return undefined;
    const num = Number(value);
    if (isNaN(num)) return undefined;
    const hours = Math.floor(num);
    const minutes = Math.round((num - hours) * 60);
    const mm = String(minutes).padStart(2, '0');
    return `${hours}h${mm}`;
  }

  private mapStatus(value: any): 'present' | 'absent' | 'holiday' | 'leave' | 'late' {
    if (value === null || value === undefined) return 'present';
    if (typeof value === 'number') {
      switch (value) {
        case 1: return 'present';
        case 2: return 'absent';
        case 3: return 'holiday';
        case 4: return 'leave';
        default: return 'present';
      }
    }
    const s = String(value).toLowerCase();
    if (s === 'present' || s === 'absent' || s === 'holiday' || s === 'leave' || s === 'late') return s as any;
    return 'present';
  }

  private formatBreakDuration(start: string, end: string): string {
    const [sh, sm] = start.split(':').map(Number);
    const [eh, em] = end.split(':').map(Number);
    const startMin = sh * 60 + sm;
    const endMin = eh * 60 + em;
    const diff = Math.max(0, endMin - startMin);
    const h = Math.floor(diff / 60);
    const m = diff % 60;
    return `${h}h${String(m).padStart(2, '0')}`;
  }

  private loadBreaksForAttendance(attendanceId: string): void {
    this.breakService.getByAttendance(Number(attendanceId)).subscribe({
      next: (breaks) => {
        const mapped = breaks.map(b => ({
          id: String(b.id),
          start: b.breakStart.slice(0, 5),
          end: b.breakEnd ? b.breakEnd.slice(0, 5) : '',
          type: b.breakType,
          duration: b.breakStart && b.breakEnd ? 
            this.formatBreakDuration(b.breakStart.slice(0, 5), b.breakEnd.slice(0, 5)) : 
            undefined
        }));
        this.todayAttendance.update(curr => curr ? { ...curr, breaks: mapped } : curr);
      },
      error: (err) => console.error('Failed to load breaks', err)
    });
  }

  loadTodayAttendance(): void {
    const user = this.currentUser();
    const employeeId = Number(user?.employee_id ?? user?.id ?? 0);
    if (!employeeId) {
      this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'attendance.noRecords' });
      this.todayAttendance.set(null);
      return;
    }

    const today = new Date().toISOString().slice(0, 10);
    const url = `${environment.apiUrl}/employee-attendance/employee/${employeeId}?startDate=${today}&endDate=${today}`;
    this.http.get<any[]>(url).subscribe({
      next: (res) => {
        const first = Array.isArray(res) && res.length ? res[0] : null;
        if (first) {
          this.todayAttendance.set({
            id: String(first.id ?? first.Id ?? ''),
            date: first.workDate ?? first.WorkDate ?? today,
            timeIn: first.checkIn ? String(first.checkIn).slice(0,5) : null,
            timeOut: first.checkOut ? String(first.checkOut).slice(0,5) : null,
            status: this.mapStatus(first.status),
            duration: this.formatWorkedHours(first.workedHours),
            breaks: []
          });
          // start live timer if applicable
          this.startTimerIfNeeded();
          // load breaks
          const atId = String(first.id ?? first.Id ?? '');
          if (atId) this.loadBreaksForAttendance(atId);
        } else {
          this.todayAttendance.set(null);
          this.startTimerIfNeeded();
        }
      },
      error: (err) => {
        console.error('Failed to load today attendance', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || err.message || 'Failed to load attendance' });
        this.todayAttendance.set(null);
      }
    });
  }

  loadAttendanceHistory(): void {
    const user = this.currentUser();
    const employeeId = Number(user?.employee_id ?? user?.id ?? 0);
    if (!employeeId) {
      this.attendanceHistory.set([]);
      return;
    }

    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - 30);
    const startDate = start.toISOString().slice(0,10);
    const endDate = end.toISOString().slice(0,10);

    const url = `${environment.apiUrl}/employee-attendance/employee/${employeeId}?startDate=${startDate}&endDate=${endDate}`;
    this.http.get<any[]>(url).subscribe({
      next: (res) => {
        const mapped = (res || []).map(r => ({
          id: String(r.id ?? r.Id ?? ''),
          date: r.workDate ?? r.WorkDate,
          timeIn: r.checkIn ? String(r.checkIn).slice(0,5) : null,
          timeOut: r.checkOut ? String(r.checkOut).slice(0,5) : null,
          status: this.mapStatus(r.status),
          duration: this.formatWorkedHours(r.workedHours),
          breakMinutesApplied: r.breakMinutesApplied ?? r.BreakMinutesApplied ?? undefined
        }));
        this.attendanceHistory.set(mapped);
      },
      error: (err) => {
        console.error('Failed to load attendance history', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || err.message || 'Failed to load attendance history' });
        this.attendanceHistory.set([]);
      }
    });
  }

  checkIn(): void {
    if (!this.canCheckIn()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'attendance.alreadyCheckedIn'
      });
      return;
    }
    const user = this.currentUser();
    const employeeId = Number(user?.employee_id ?? user?.id ?? 0);
    if (!employeeId) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Employee id not found' });
      return;
    }

    // optimistic update: show current time immediately and start timer
    const now = new Date();
    const optimisticTimeIn = now.toTimeString().slice(0,5);
    const optimisticDate = now.toISOString().slice(0,10);
    const previous = this.todayAttendance();
    this.todayAttendance.set({
      id: previous?.id ?? '',
      date: previous?.date ?? optimisticDate,
      timeIn: optimisticTimeIn,
      timeOut: null,
      status: 'present',
      duration: undefined
    });
    this.startTimerIfNeeded();

    this.isLoading.set(true);
    const url = `${environment.apiUrl}/employee-attendance/check-in`;
    this.http.post<any>(url, { employeeId }).subscribe({
      next: (res) => {
        // Map response to todayAttendance (overwrite optimistic)
        this.todayAttendance.set({
          id: String(res.id ?? res.Id ?? ''),
          date: res.workDate ?? res.WorkDate ?? optimisticDate,
          timeIn: res.checkIn ? String(res.checkIn).slice(0,5) : optimisticTimeIn,
          timeOut: res.checkOut ? String(res.checkOut).slice(0,5) : null,
          status: this.mapStatus(res.status),
          duration: this.formatWorkedHours(res.workedHours),
          breaks: []
        });
        this.startTimerIfNeeded();
        const atId = String(res.id ?? res.Id ?? '');
        if (atId) this.loadBreaksForAttendance(atId);
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'attendance.checkInSuccess' });
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Check-in failed', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || err.message || 'Check-in failed' });
        // revert optimistic update
        if (previous) this.todayAttendance.set(previous); else this.todayAttendance.set(null);
        this.stopTimer();
        this.isLoading.set(false);
      }
    });
  }

  checkOut(): void {
    if (!this.canCheckOut()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'attendance.alreadyCheckedOut'
      });
      return;
    }
    const user = this.currentUser();
    const employeeId = Number(user?.employee_id ?? user?.id ?? 0);
    if (!employeeId) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Employee id not found' });
      return;
    }
    // capture whether there's an open break before checkout
    const hadOpenBreak = this.isOnBreak();
    const previousBreaks = this.todayAttendance()?.breaks ?? [];
    const attendanceId = Number(this.todayAttendance()?.id ?? 0) || 0;

    this.isLoading.set(true);
    const url = `${environment.apiUrl}/employee-attendance/check-out`;
    this.http.post<any>(url, { employeeId }).subscribe({
      next: (res) => {
        this.todayAttendance.update(current => ({
          id: current?.id ?? String(res.id ?? res.Id ?? ''),
          date: current?.date ?? new Date().toISOString().slice(0,10),
          timeIn: current?.timeIn ?? (res.checkIn ? String(res.checkIn).slice(0,5) : null),
          timeOut: res.checkOut ? String(res.checkOut).slice(0,5) : current?.timeOut ?? null,
          status: (res.status ?? 'present').toString().toLowerCase() as any,
          duration: this.formatWorkedHours(res.workedHours) ?? current?.duration
        }));

        // stop timer when checked out and set elapsed to final duration
        this.stopTimer();
        const final = this.todayAttendance();
        if (final?.duration) this.elapsed.set(final.duration);

        // If there was an open break before checkout, end it at checkout time
        if (hadOpenBreak && attendanceId) {
          // determine checkout timestamp (prefer server-provided value)
          let checkoutDate = new Date();
          if (res && res.checkOut) {
            const parsed = new Date(res.checkOut);
            if (!isNaN(parsed.getTime())) checkoutDate = parsed;
          }
          const pad = (v: number) => String(v).padStart(2, '0');
          const checkoutTimeFull = `${pad(checkoutDate.getHours())}:${pad(checkoutDate.getMinutes())}:00`;

          this.breakService.endBreak(attendanceId, { breakEnd: checkoutTimeFull }).subscribe({
            next: () => {
              this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Break ended at checkout' });
              // reload to refresh breaks and recalculated worked hours
              this.loadTodayAttendance();
            },
            error: (err) => {
              console.error('Failed to end open break at checkout', err);
              this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Failed to close open break automatically' });
              // still reload to keep UI in sync
              this.loadTodayAttendance();
            }
          });
        }

        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'attendance.checkOutSuccess' });
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Check-out failed', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || err.message || 'Check-out failed' });
        this.isLoading.set(false);
      }
    });
  }

  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      present: 'bg-green-100 text-green-700',
      absent: 'bg-red-100 text-red-700',
      late: 'bg-yellow-100 text-yellow-700',
      holiday: 'bg-blue-100 text-blue-700',
      leave: 'bg-indigo-100 text-indigo-700'
    };
    return classes[status] || 'bg-gray-100 text-gray-700';
  }

  startBreak(): void {
    const today = this.todayAttendance();
    if (!today || !today.id) {
      this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Please check in first' });
      return;
    }
    if (this.isOnBreak()) {
      this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Break already started' });
      return;
    }

    const now = new Date();
    const startTime = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:00`;
    
    // Optimistic update
    const breaks = today.breaks ?? [];
    const optimisticBreak = { start: startTime.slice(0, 5), end: '', type: 'Manual' };
    this.todayAttendance.update(curr => 
      curr ? { ...curr, breaks: [...breaks, optimisticBreak] } : curr
    );

    this.breakService.startBreak({
      attendanceId: Number(today.id),
      breakStart: startTime,
      breakType: 'Manual'
    }).subscribe({
      next: (res) => {
        // Update with actual break data from backend
        const newBreak = {
          id: String(res.id),
          start: res.breakStart.slice(0, 5),
          end: res.breakEnd ? res.breakEnd.slice(0, 5) : '',
          type: res.breakType
        };
        const updatedBreaks = [...breaks, newBreak];
        this.todayAttendance.update(curr => 
          curr ? { ...curr, breaks: updatedBreaks } : curr
        );
        this.messageService.add({ 
          severity: 'success', 
          summary: 'Success', 
          detail: 'Break started successfully' 
        });
      },
      error: (err) => {
        console.error('Failed to start break', err);
        // Revert optimistic update
        this.todayAttendance.update(curr => 
          curr ? { ...curr, breaks } : curr
        );
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Error', 
          detail: err?.error || 'Failed to start break' 
        });
      }
    });
  }

  endBreak(): void {
    const today = this.todayAttendance();
    if (!today || !today.id) return;
    const breaks = today.breaks ?? [];
    if (!breaks.length) return;
    const lastBreak = breaks[breaks.length - 1];
    if (lastBreak.end) return;

    const now = new Date();
    const endTime = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:00`;
    
    // Optimistic update
    const optimisticEndTime = endTime.slice(0, 5);
    const optimisticBreak = { 
      ...lastBreak, 
      end: optimisticEndTime,
      duration: this.formatBreakDuration(lastBreak.start, optimisticEndTime)
    };
    const optimisticBreaks = [...breaks.slice(0, -1), optimisticBreak];
    this.todayAttendance.update(curr => 
      curr ? { ...curr, breaks: optimisticBreaks } : curr
    );

    this.breakService.endBreak(Number(today.id), {
      breakEnd: endTime
    }).subscribe({
      next: (res) => {
        const savedBreak = {
          id: String(res.id),
          start: res.breakStart.slice(0, 5),
          end: res.breakEnd ? res.breakEnd.slice(0, 5) : optimisticEndTime,
          type: res.breakType,
          duration: this.formatBreakDuration(
            res.breakStart.slice(0, 5), 
            res.breakEnd ? res.breakEnd.slice(0, 5) : optimisticEndTime
          )
        };
        const updatedBreaks = [...breaks.slice(0, -1), savedBreak];
        this.todayAttendance.update(curr => 
          curr ? { ...curr, breaks: updatedBreaks } : curr
        );
        this.messageService.add({ 
          severity: 'success', 
          summary: 'Success', 
          detail: 'Break ended successfully' 
        });
        // Reload to get updated worked hours
        this.loadTodayAttendance();
      },
      error: (err) => {
        console.error('Failed to end break', err);
        // Revert optimistic update
        this.todayAttendance.update(curr => 
          curr ? { ...curr, breaks } : curr
        );
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Error', 
          detail: err?.error || 'Failed to end break' 
        });
      }
    });
  }
}
