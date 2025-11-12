import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

interface EnumDisplayData {
  name: string;
  values: { name: string; value: number }[];
  valueCount: number;
}

@Component({
  selector: 'app-view-enum-code-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatSnackBarModule],
  templateUrl: './view-enum-code-dialog.component.html',
  styleUrl: './view-enum-code-dialog.component.scss',
})
export class ViewEnumCodeDialogComponent {
  generatedCode: string;

  constructor(
    private dialogRef: MatDialogRef<ViewEnumCodeDialogComponent>,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: EnumDisplayData
  ) {
    this.generatedCode = this.generateCSharpCode(data);
  }

  private generateCSharpCode(enumData: EnumDisplayData): string {
    const sortedValues = enumData.values.sort((a, b) => a.value - b.value);
    const currentDate = new Date().toISOString().split('T')[0];
    const currentTime = new Date().toTimeString().split(' ')[0];
    
    let code = `// Auto-generated file - do not modify manually\n`;
    code += `// Generated: ${currentDate} ${currentTime} UTC\n\n`;
    code += `namespace MyCompany.MyProject.WebApi.Models;\n\n`;
    code += `public enum ${enumData.name}\n`;
    code += `{\n`;

    sortedValues.forEach((value, index) => {
      code += `    ${value.name} = ${value.value}`;
      if (index < sortedValues.length - 1) {
        code += `,\n`;
      } else {
        code += `\n`;
      }
    });

    code += `}`;
    
    return code;
  }

  copyToClipboard(): void {
    navigator.clipboard.writeText(this.generatedCode).then(() => {
      this.snackBar.open('Code copied to clipboard!', 'Close', { duration: 2000 });
    }).catch(() => {
      this.snackBar.open('Failed to copy code', 'Close', { duration: 3000 });
    });
  }

  onClose(): void {
    this.dialogRef.close();
  }
}