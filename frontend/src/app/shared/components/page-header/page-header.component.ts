import { Component, ChangeDetectionStrategy, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { AuthService } from '@app/core/services/auth.service';
import { PageHeaderService } from '../../services/page-header.service';

@Component({
    selector: 'app-page-header',
    standalone: true,
    imports: [CommonModule, FormsModule, SelectModule],
    templateUrl: './page-header.component.html',
    styleUrl: './page-header.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageHeaderComponent {
    private authService = inject(AuthService);
    private pageHeaderService = inject(PageHeaderService);

    // Inputs
    departmentOptions = input<Array<{ label: string; value: string }>>([]);
    parityOptions = input<Array<{ label: string; value: string }>>([]);
    monthOptions = input<Array<{ label: string; value: string }>>([]);

    selectedDepartments = input<string[]>([]);
    selectedParity = input<string>('all');
    selectedMonth = input<string>('');

    // Outputs
    departmentsChange = output<string[]>();
    parityChange = output<string>();
    monthChange = output<string>();

    // Public signals from service
    config = this.pageHeaderService.config;

    // Computed user greeting
    userGreeting(): string {
        const user = this.authService.currentUser();
        if (user) {
            return `Bon retour, ${user.firstName}`;
        }
        return 'Bon retour';
    }

    // Event handlers
    onDepartmentsChange(values: string[] | null | undefined): void {
        this.departmentsChange.emit(values ?? []);
        this.pageHeaderService.setSelectedDepartments(values ?? []);
    }

    onParityChange(value: string | null | undefined): void {
        if (value) {
            this.parityChange.emit(value);
            this.pageHeaderService.setSelectedParity(value);
        }
    }

    onMonthChange(value: string | null | undefined): void {
        if (value) {
            this.monthChange.emit(value);
            this.pageHeaderService.setSelectedMonth(value);
        }
    }
}
