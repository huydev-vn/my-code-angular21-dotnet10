import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { email, form, FormField, minLength, required, submit } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent, MatCardHeader, MatCardTitle } from '@angular/material/card';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';

import { IdentityFacade } from '../../state/identity.facade';

@Component({
  selector: 'app-register-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    FormField,
    MatButton,
    MatCard,
    MatCardContent,
    MatCardHeader,
    MatCardTitle,
    MatFormField,
    MatLabel,
    MatError,
    MatInput,
    MatProgressBar,
  ],
  templateUrl: './register-page.html',
  styleUrl: './register-page.css',
})
export class RegisterPage {
  private readonly identity = inject(IdentityFacade);

  protected readonly authenticating = this.identity.authenticating;
  protected readonly error = this.identity.error;

  private readonly model = signal({
    email: '',
    password: '',
  });

  protected readonly registerForm = form(this.model, (schemaPath) => {
    required(schemaPath.email, { message: 'Email is required.' });
    email(schemaPath.email, { message: 'Enter a valid email.' });
    required(schemaPath.password, { message: 'Password is required.' });
    minLength(schemaPath.password, 8, { message: 'Use at least 8 characters.' });
  });

  protected onSubmit(event: Event): void {
    event.preventDefault();

    void submit(this.registerForm, async () => {
      this.identity.register(this.model());
    });
  }
}
