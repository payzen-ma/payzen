import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { CardModule } from 'primeng/card';
import { TabsModule } from 'primeng/tabs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, TranslateModule, CardModule, TabsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent {}
