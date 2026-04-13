import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
    id: string;
    message: string;
    type: 'success' | 'error' | 'warning' | 'info';
    duration?: number;
}

@Injectable({
    providedIn: 'root'
})
export class ToastService {
    private toasts$ = new BehaviorSubject<Toast[]>([]);
    private idCounter = 0;

    getToasts(): Observable<Toast[]> {
        return this.toasts$.asObservable();
    }

    success(message: string, duration = 3000): void {
        this.addToast(message, 'success', duration);
    }

    error(message: string, duration = 4000): void {
        this.addToast(message, 'error', duration);
    }

    warning(message: string, duration = 3500): void {
        this.addToast(message, 'warning', duration);
    }

    info(message: string, duration = 3000): void {
        this.addToast(message, 'info', duration);
    }

    private addToast(message: string, type: Toast['type'], duration: number): void {
        const id = `toast-${++this.idCounter}`;
        const toast: Toast = { id, message, type, duration };

        const current = this.toasts$.value;
        this.toasts$.next([...current, toast]);

        if (duration > 0) {
            setTimeout(() => this.removeToast(id), duration);
        }
    }

    removeToast(id: string): void {
        const current = this.toasts$.value;
        this.toasts$.next(current.filter(t => t.id !== id));
    }

    clear(): void {
        this.toasts$.next([]);
    }
}
