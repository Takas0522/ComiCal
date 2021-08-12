import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataMigrationSerivce } from '../data-migration.service';

@Component({
  selector: 'app-code-register',
  templateUrl: './code-register.component.html',
  styleUrls: ['./code-register.component.scss']
})
export class CodeRegisterComponent {


  formGroup = new FormGroup({
    id: new FormControl('', Validators.required)
  });

  constructor(
    @Inject(MAT_DIALOG_DATA) private data: string[],
    private dialogRef: MatDialogRef<CodeRegisterComponent>,
    private service: DataMigrationSerivce
  ) { }

  onSubmit() {
    if (this.formGroup.invalid) {
      return;
    }
    const id = this.formGroup.value.id;
    this.service.registerMigrationCode(id).subscribe(x => {
      if (!x) {
        alert('入力された連携コードのデータは存在しませんでした。\n再度コードを生成してください。');
        this.dialogRef.close();
      }
      if (this.data && this.data.length > 0) {
        const res = confirm('既に登録されているデータを上書きします。\nよろしいですか?');
        if (res) {
          this.dialogRef.close(x);
        }
        this.dialogRef.close();
      }
      this.dialogRef.close(x);
    });
  }

}
