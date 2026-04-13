import { Injectable, signal, computed } from '@angular/core';

export interface PageHeaderConfig {
    showDepartments: boolean;
    departmentOptions: Array<{ label: string; value: string }>;
    showParity: boolean;
    parityOptions: Array<{ label: string; value: string }>;
    showMonth: boolean;
    monthOptions: Array<{ label: string; value: string }>;
}

@Injectable({
    providedIn: 'root'
})
export class PageHeaderService {
    // Visibility signal
    private isVisibleSignal = signal<boolean>(false);
    public isVisible = computed(() => this.isVisibleSignal());

    // Configuration signal
    private configSignal = signal<PageHeaderConfig | null>(null);
    public config = computed(() => this.configSignal());

    // Filter selections
    private selectedDepartmentsSignal = signal<string[]>([]);
    public selectedDepartments = computed(() => this.selectedDepartmentsSignal());

    private selectedParitySignal = signal<string>('all');
    public selectedParity = computed(() => this.selectedParitySignal());

    private selectedMonthSignal = signal<string>('');
    public selectedMonth = computed(() => this.selectedMonthSignal());

    constructor() { }

    // Public methods
    registerConfig(config: PageHeaderConfig): void {
        this.configSignal.set(config);
        this.show();
    }

    show(): void {
        this.isVisibleSignal.set(true);
    }

    hide(): void {
        this.isVisibleSignal.set(false);
    }

    setSelectedDepartments(departments: string[]): void {
        this.selectedDepartmentsSignal.set(departments);
    }

    setSelectedParity(parity: string): void {
        this.selectedParitySignal.set(parity);
    }

    setSelectedMonth(month: string): void {
        this.selectedMonthSignal.set(month);
    }

    reset(): void {
        this.selectedDepartmentsSignal.set([]);
        this.selectedParitySignal.set('all');
        this.selectedMonthSignal.set('');
    }
}
