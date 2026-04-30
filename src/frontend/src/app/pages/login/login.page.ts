import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>ログイン</h1>
      <p>Microsoft / Google / X(Twitter) でログインできます。</p>
    </main>
  `,
})
export class LoginPage {}
