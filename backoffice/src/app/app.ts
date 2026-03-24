import { Component } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { ConfirmModalComponent } from './shared/confirm/confirm-modal.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ConfirmModalComponent],
  templateUrl:'./app.html'
})

export class App {}