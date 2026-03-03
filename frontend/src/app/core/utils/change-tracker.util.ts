/**
 * Field-level change tracking utility
 * Generates diffs with before/after values for modified fields
 */

export interface FieldChange<T = any> {
  field: string;
  label: string;
  oldValue: T;
  newValue: T;
  type: 'string' | 'number' | 'boolean' | 'date' | 'object' | 'array';
}

export interface ChangeSet {
  changes: FieldChange[];
  hasChanges: boolean;
  modifiedFields: string[];
  changeCount: number;
}

export class ChangeTracker {
  /**
   * Compare two objects and generate a field-level change set
   */
  static trackChanges<T extends Record<string, any>>(
    original: T,
    current: T,
    fieldLabels: Record<keyof T, string> = {} as any,
    excludeFields: string[] = []
  ): ChangeSet {
    const changes: FieldChange[] = [];
    const modifiedFields: string[] = [];

    for (const key in current) {
      if (excludeFields.includes(key)) continue;
      
      const oldValue = original[key];
      const newValue = current[key];

      if (this.hasValueChanged(oldValue, newValue)) {
        modifiedFields.push(key);
        changes.push({
          field: key,
          label: fieldLabels[key] || this.formatFieldName(key),
          oldValue,
          newValue,
          type: this.getValueType(newValue)
        });
      }
    }

    return {
      changes,
      hasChanges: changes.length > 0,
      modifiedFields,
      changeCount: changes.length
    };
  }

  /**
   * Generate a patch object containing only changed fields
   */
  static generatePatch<T extends Record<string, any>>(
    original: T,
    current: T,
    excludeFields: string[] = []
  ): Partial<T> {
    const patch: Partial<T> = {};

    for (const key in current) {
      if (excludeFields.includes(key)) continue;
      
      if (this.hasValueChanged(original[key], current[key])) {
        patch[key] = current[key];
      }
    }

    return patch;
  }

  /**
   * Check if a value has changed (handles different types)
   */
  private static hasValueChanged(oldValue: any, newValue: any): boolean {
    // Handle null/undefined
    if (oldValue === newValue) return false;
    if (oldValue == null && newValue == null) return false;
    if (oldValue == null || newValue == null) return true;

    // Handle dates
    if (oldValue instanceof Date && newValue instanceof Date) {
      return oldValue.getTime() !== newValue.getTime();
    }

    // Handle arrays
    if (Array.isArray(oldValue) && Array.isArray(newValue)) {
      if (oldValue.length !== newValue.length) return true;
      return JSON.stringify(oldValue) !== JSON.stringify(newValue);
    }

    // Handle objects
    if (typeof oldValue === 'object' && typeof newValue === 'object') {
      return JSON.stringify(oldValue) !== JSON.stringify(newValue);
    }

    // Handle primitives
    return oldValue !== newValue;
  }

  /**
   * Determine value type for display purposes
   */
  private static getValueType(value: any): FieldChange['type'] {
    if (value instanceof Date) return 'date';
    if (Array.isArray(value)) return 'array';
    if (value === null || value === undefined) return 'string';
    
    const type = typeof value;
    if (type === 'string' || type === 'number' || type === 'boolean') {
      return type;
    }
    
    return 'object';
  }

  /**
   * Format field name for display (camelCase -> Camel Case)
   */
  private static formatFieldName(field: string): string {
    return field
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  /**
   * Format value for display
   */
  static formatValue(value: any, type: FieldChange['type']): string {
    if (value === null || value === undefined || value === '') {
      return '—';
    }

    switch (type) {
      case 'date':
        return value instanceof Date 
          ? value.toLocaleDateString() 
          : new Date(value).toLocaleDateString();
      
      case 'boolean':
        return value ? 'Yes' : 'No';
      
      case 'array':
        return Array.isArray(value) ? value.join(', ') : String(value);
      
      case 'object':
        return JSON.stringify(value, null, 2);
      
      case 'number':
        return String(value);
      
      default:
        return String(value);
    }
  }

  /**
   * Group changes by category (optional categorization)
   */
  static categorizeChanges(
    changes: FieldChange[],
    categories: Record<string, string[]>
  ): Record<string, FieldChange[]> {
    const categorized: Record<string, FieldChange[]> = {};

    for (const [category, fields] of Object.entries(categories)) {
      categorized[category] = changes.filter(change => 
        fields.includes(change.field)
      );
    }

    // Uncategorized changes
    const allCategorizedFields = Object.values(categories).flat();
    categorized['Other'] = changes.filter(change => 
      !allCategorizedFields.includes(change.field)
    );

    // Remove empty categories
    for (const key in categorized) {
      if (categorized[key].length === 0) {
        delete categorized[key];
      }
    }

    return categorized;
  }
}
