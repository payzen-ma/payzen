import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmService } from './confirm.service';

@Component({
  selector: 'app-confirm-modal',
  templateUrl: './confirm-modal.component.html',
  standalone: true,
  imports: [CommonModule]
})
export class ConfirmModalComponent implements OnInit {
  visible = false;
  message = '';
  private pendingResolve?: (v:boolean)=>void;
  constructor(private confirm: ConfirmService) {}
  ngOnInit() {
    this.confirm.requests.subscribe(req => {
      this.message = req.message;
      this.pendingResolve = req.resolve;
      this.visible = true;
    });
  }
  ok() { this.pendingResolve?.(true); this.close(); }
  cancel() { this.pendingResolve?.(false); this.close(); }
  close(){ this.visible = false; this.pendingResolve = undefined; }
}