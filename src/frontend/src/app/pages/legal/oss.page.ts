import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-oss-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>OSS ライセンス情報</h1>
    </main>
  `,
})
export class OssPage {}
