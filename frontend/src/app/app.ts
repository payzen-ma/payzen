import { Component, signal, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LanguageService } from './shared/utils/language.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('payzen-frontend');
  private languageService = inject(LanguageService);

  ngOnInit(): void {
    // Language service is automatically initialized via constructor
  }
}
