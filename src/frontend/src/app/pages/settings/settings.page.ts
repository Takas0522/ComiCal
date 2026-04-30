import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>設定</h1>
      <p>テーマ・アフィリエイト・アカウント設定ができます。</p>
    </main>
  `,
})
export class SettingsPage {}
