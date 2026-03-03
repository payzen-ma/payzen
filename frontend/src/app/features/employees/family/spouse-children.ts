import { Component, Input, Output, EventEmitter, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FamilyService } from '@app/core/services/family.service';
import { ReferenceDataService } from '@app/core/services/reference-data.service';
import { Spouse, Child } from '@app/core/models/employee.model';

@Component({
  selector: 'app-spouse-children',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, ButtonModule, TableModule, InputTextModule, SelectModule],
  templateUrl: './spouse-children.html'
})
export class SpouseChildrenComponent {
  @Input() employeeId!: string | number;
  @Input() isEditMode: boolean = false;
  @Input() set spouses(value: Spouse[]) {
    this.spousesSignal.set(value || []);
  }
  @Input() set children(value: Child[]) {
    this.childrenSignal.set(value || []);
  }
  
  @Output() spousesChange = new EventEmitter<Spouse[]>();
  @Output() childrenChange = new EventEmitter<Child[]>();
  
  private familyService = inject(FamilyService);
  private referenceDataService = inject(ReferenceDataService);
  private translate = inject(TranslateService);

  readonly spousesSignal = signal<Spouse[]>([]);
  readonly childrenSignal = signal<Child[]>([]);
  readonly loading = signal(false);
  readonly genders = signal<any[]>([]);
  readonly genderOptions = computed(() => {
    const lang = this.translate?.currentLang || 'fr';
    return this.genders().map(g => {
      let label = g.nameFr; // default
      if (lang.startsWith('en')) label = g.nameEn || g.nameFr;
      else if (lang.startsWith('ar')) label = g.nameAr || g.nameFr;
      return { id: g.id, label: label || g.nameEn || String(g.id) };
    });
  });
  readonly genderMap = computed(() => {
    const lang = this.translate?.currentLang || 'fr';
    return new Map(this.genders().map(g => {
      let label = g.nameFr;
      if (lang.startsWith('en')) label = g.nameEn || g.nameFr;
      else if (lang.startsWith('ar')) label = g.nameAr || g.nameFr;
      return [g.id, label || g.nameEn || String(g.id)];
    }));
  });

  // Dialog state
  readonly showSpouseDialog = signal(false);
  readonly editingSpouse = signal<Partial<Spouse> | null>(null);
  readonly showChildDialog = signal(false);
  readonly editingChild = signal<Partial<Child> | null>(null);

  constructor() {
    // Load genders
    this.referenceDataService.getGenders().subscribe({
      next: (data) => {
        console.log('[SpouseChildren] Loaded genders from API:', data);
        this.genders.set(data);
        console.log('[SpouseChildren] Gender options computed:', this.genderOptions());
      },
      error: (err) => {
        console.error('[SpouseChildren] Failed to load genders:', err);
        this.genders.set([]);
      }
    });

    // Keep genderName populated on spouse/child records when genders or records change
    effect(() => {
      const map = this.genderMap();

      const spouses = this.spousesSignal();
      const updatedSpouses = spouses.map(s => {
        const name = map.get((s as any).genderId);
        return name && name !== s.genderName ? { ...s, genderName: name } : s;
      });
      if (!this.arraysEqual(spouses, updatedSpouses)) this.spousesSignal.set(updatedSpouses);

      const children = this.childrenSignal();
      const updatedChildren = children.map(c => {
        const name = map.get((c as any).genderId);
        return name && name !== c.genderName ? { ...c, genderName: name } : c;
      });
      if (!this.arraysEqual(children, updatedChildren)) this.childrenSignal.set(updatedChildren);
    });
  }

  private arraysEqual(a: any[], b: any[]) {
    try {
      return JSON.stringify(a) === JSON.stringify(b);
    } catch {
      return false;
    }
  }

  openCreateSpouse() {
    this.editingSpouse.set({ employeeId: this.employeeId });
    this.showSpouseDialog.set(true);
  }

  openEditSpouse(spouse: Spouse) {
    this.editingSpouse.set({ ...spouse });
    this.showSpouseDialog.set(true);
  }

  updateSpouseField(field: string, value: any) {
    const current = this.editingSpouse();
    if (current) {
      this.editingSpouse.set({ ...current, [field]: value });
    }
  }

  saveSpouse() {
    const payload = this.editingSpouse();
    if (!payload) return;
    
    console.log('[SpouseChildren] Emitting spouse change:', payload);
    const currentSpouses = [...this.spousesSignal()];
    
    if (payload.id) {
      // Update existing
      const index = currentSpouses.findIndex(s => s.id === payload.id);
      if (index >= 0) {
        currentSpouses[index] = payload as Spouse;
      }
    } else {
      // Add new (assign temporary ID)
      currentSpouses.push({ ...payload, id: Date.now() } as Spouse);
    }
    
    this.spousesSignal.set(currentSpouses);
    this.spousesChange.emit(currentSpouses);
    this.showSpouseDialog.set(false);
  }

  deleteSpouse(spouse: Spouse) {
    if (!confirm('Supprimer le conjoint ?')) return;
    console.log('[SpouseChildren] Emitting spouse deletion');
    const currentSpouses = this.spousesSignal().filter(s => s.id !== spouse.id);
    this.spousesSignal.set(currentSpouses);
    this.spousesChange.emit(currentSpouses);
  }

  openCreateChild() {
    this.editingChild.set({ employeeId: this.employeeId });
    this.showChildDialog.set(true);
  }

  openEditChild(child: Child) {
    this.editingChild.set({ ...child });
    this.showChildDialog.set(true);
  }

  updateChildField(field: string, value: any) {
    const current = this.editingChild();
    if (current) {
      this.editingChild.set({ ...current, [field]: value });
    }
  }

  saveChild() {
    const payload = this.editingChild();
    if (!payload) return;
    
    console.log('[SpouseChildren] Emitting child change:', payload);
    const currentChildren = [...this.childrenSignal()];
    
    if (payload.id) {
      // Update existing
      const index = currentChildren.findIndex(c => c.id === payload.id);
      if (index >= 0) {
        currentChildren[index] = payload as Child;
      }
    } else {
      // Add new (assign temporary ID)
      currentChildren.push({ ...payload, id: Date.now() } as Child);
    }
    
    this.childrenSignal.set(currentChildren);
    this.childrenChange.emit(currentChildren);
    this.showChildDialog.set(false);
  }

  deleteChild(child: Child) {
    if (!confirm('Supprimer l\'enfant ?')) return;
    console.log('[SpouseChildren] Emitting child deletion');
    const currentChildren = this.childrenSignal().filter(c => c.id !== child.id);
    this.childrenSignal.set(currentChildren);
    this.childrenChange.emit(currentChildren);
  }
}
