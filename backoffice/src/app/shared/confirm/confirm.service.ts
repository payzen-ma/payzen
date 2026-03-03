import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private request$ = new Subject<{ message: string; resolve: (v: boolean)=>void }>();
  get requests() { return this.request$.asObservable(); }
  confirm(message: string): Promise<boolean> {
    return new Promise(resolve => this.request$.next({ message, resolve }));
  }
}