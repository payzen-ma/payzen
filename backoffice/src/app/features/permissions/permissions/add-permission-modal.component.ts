import { Component, EventEmitter, Output, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Permission } from '../../../models/role.model';

@Component({
  selector: 'app-add-permission-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50">
      <div class="bg-white rounded-lg shadow-lg p-8 w-full max-w-md">
        <h2 class="text-xl font-bold mb-4">{{ permission ? 'Modifier une permission' : 'Ajouter une permission' }}</h2>
        <form (ngSubmit)="submit()" #form="ngForm">
          <div class="mb-4">
            <label class="block text-gray-700 mb-1">Nom</label>
            <input type="text" class="w-full border rounded px-3 py-2" [(ngModel)]="name" name="name" required />
            <div class="text-xs text-gray-500 mt-1">Nom unique de la permission, par exemple : <b>VIEW_USERS</b></div>
          </div>
          <div class="mb-4">
            <label class="block text-gray-700 mb-1">Description</label>
            <input type="text" class="w-full border rounded px-3 py-2" [(ngModel)]="description" name="description" />
            <div class="text-xs text-gray-500 mt-1">Courte explication de ce que permet cette permission.</div>
          </div>
          <div class="mb-4">
            <label class="block text-gray-700 mb-1">Ressource</label>
            <input type="text" class="w-full border rounded px-3 py-2" [(ngModel)]="resource" name="resource" required />
            <div class="text-xs text-gray-500 mt-1">Entité ou module concerné, par exemple : <b>Users</b>, <b>Companies</b>, <b>Roles</b>...</div>
          </div>
          <div class="mb-4">
            <label class="block text-gray-700 mb-1">Action</label>
            <input type="text" class="w-full border rounded px-3 py-2" [(ngModel)]="action" name="action" required />
            <div class="text-xs text-gray-500 mt-1">Type d'action autorisée, par exemple : <b>View</b>, <b>Edit</b>, <b>Delete</b>, <b>Create</b>...</div>
          </div>
          <div class="flex justify-end gap-2">
            <button type="button" class="px-4 py-2 bg-gray-200 rounded" (click)="close()">Annuler</button>
            <button type="submit" class="px-4 py-2 bg-primary-500 text-white rounded" [disabled]="form.invalid">{{ permission ? 'Enregistrer' : 'Ajouter' }}</button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class AddPermissionModalComponent {
  @Output() add = new EventEmitter<any>();
  @Output() update = new EventEmitter<any>();
  @Input() permission?: Permission | null;
  @Output() cancel = new EventEmitter<void>();

  name = '';
  description = '';
  resource = '';
  action = '';

  ngOnChanges(changes: SimpleChanges) {
    if (changes['permission'] && this.permission) {
      this.name = this.permission.name || '';
      this.description = this.permission.description || '';
      this.resource = this.permission.resource || '';
      this.action = this.permission.action || '';
    }
    if (changes['permission'] && !this.permission) {
      this.name = '';
      this.description = '';
      this.resource = '';
      this.action = '';
    }
  }

  submit() {
    if (this.name && this.resource && this.action) {
      const payload = {
        name: this.name,
        description: this.description,
        resource: this.resource,
        action: this.action
      };
      if (this.permission && this.permission.id) {
        this.update.emit({ id: this.permission.id, ...payload });
      } else {
        this.add.emit(payload);
      }
    }
  }

  close() {
    this.cancel.emit();
  }
}
