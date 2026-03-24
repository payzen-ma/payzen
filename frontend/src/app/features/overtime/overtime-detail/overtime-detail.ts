import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { TranslateModule } from '@ngx-translate/core';
import { OvertimeService } from '@app/core/services/overtime.service';

@Component({
  selector: 'app-overtime-detail',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, ToastModule, TranslateModule],
  templateUrl: './overtime-detail.html',
  styleUrls: ['./overtime-detail.css']
})
export class OvertimeDetailComponent implements OnInit {
  private readonly overtimeService = inject(OvertimeService);
  private readonly route = inject(ActivatedRoute);

  readonly overtime = signal<any | null>(null);
  readonly isLoading = signal<boolean>(false);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) return;
    this.isLoading.set(true);
    this.overtimeService.getOvertimeById(id).subscribe({
      next: (o) => { this.overtime.set(o); this.isLoading.set(false); },
      error: (err) => { console.error('Error loading overtime', err); this.isLoading.set(false); }
    });
  }
}
