import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>まんがリマインダー</h1>
      <p>直近の発売予定を確認できます。</p>
    </main>
  `,
})
export class HomePage {}
