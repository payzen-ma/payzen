import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageSwitcher } from '../../shared/components/language-switcher/language-switcher';

@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet, TranslateModule, LanguageSwitcher],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.css',
})
export class AuthLayout {
  readonly currentYear = new Date().getFullYear();
}
