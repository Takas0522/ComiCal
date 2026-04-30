import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-privacy-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>プライバシーポリシー</h1>
    </main>
  `,
})
export class PrivacyPage {}
