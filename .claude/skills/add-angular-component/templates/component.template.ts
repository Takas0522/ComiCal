import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-{{name}}',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './{{name}}.component.html',
})
export class {{Name}}Component {
  readonly value = input.required<string>();
  readonly clicked = output<void>();

  protected onClick(): void {
    this.clicked.emit();
  }
}
