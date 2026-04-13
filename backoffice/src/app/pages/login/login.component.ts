import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthCardComponent } from '../../shared/components/auth-card/auth-card.component';
import { PasswordInputComponent } from '../../shared/components/password-input/password-input.component';
import { PrimaryButtonComponent } from '../../shared/components/primary-button/primary-button.component';
import { TextInputComponent } from '../../shared/components/text-input/text-input.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    FormsModule,
    AuthCardComponent,
    TextInputComponent,
    PasswordInputComponent,
    PrimaryButtonComponent
  ],
  templateUrl: './login.component.html'
})
export class Login {
  email = '';
  password = '';

  onSubmit() {
    if (this.email && this.password) {
    }
  }
}
