import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>カレンダー</h1>
      <p>週・月カレンダービューで発売予定を確認できます。</p>
    </main>
  `,
})
export class CalendarPage {}
