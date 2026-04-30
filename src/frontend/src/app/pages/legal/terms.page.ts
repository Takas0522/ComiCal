import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>利用規約</h1>
    </main>
  `,
})
export class TermsPage {}
