import { Injectable } from '@angular/core';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { filter } from 'rxjs/operators';

export interface DraftMetadata {
  entityId: string;
  entityType: string;
  tabId: string;
  savedAt: string;
  version: number;
}

export interface Draft<T = any> {
  data: T;
  metadata: DraftMetadata;
}

@Injectable({
  providedIn: 'root'
})
export class DraftService {
  private readonly STORAGE_PREFIX = 'draft_';
  private readonly VERSION = 1;
  private tabId: string;
  
  // Observable for cross-tab draft updates
  private draftUpdated$ = new BehaviorSubject<{ key: string; draft: Draft | null }>({ key: '', draft: null });

  constructor() {
    this.tabId = this.generateTabId();
    this.listenToStorageChanges();
  }

  /**
   * Save draft to localStorage with metadata
   */
  saveDraft<T>(entityType: string, entityId: string, data: T): void {
    const key = this.buildKey(entityType, entityId);
    const draft: Draft<T> = {
      data,
      metadata: {
        entityId,
        entityType,
        tabId: this.tabId,
        savedAt: new Date().toISOString(),
        version: this.VERSION
      }
    };

    try {
      localStorage.setItem(key, JSON.stringify(draft));
      this.draftUpdated$.next({ key, draft });
    } catch (error) {
      console.error('Failed to save draft:', error);
      this.handleStorageQuotaExceeded();
    }
  }

  /**
   * Load draft from localStorage
   */
  loadDraft<T>(entityType: string, entityId: string): Draft<T> | null {
    const key = this.buildKey(entityType, entityId);
    const raw = localStorage.getItem(key);
    
    if (!raw) return null;

    try {
      const draft = JSON.parse(raw) as Draft<T>;
      
      // Version check
      if (draft.metadata.version !== this.VERSION) {
        console.warn('Draft version mismatch, discarding');
        this.clearDraft(entityType, entityId);
        return null;
      }

      return draft;
    } catch (error) {
      console.error('Failed to parse draft:', error);
      this.clearDraft(entityType, entityId);
      return null;
    }
  }

  /**
   * Clear specific draft
   */
  clearDraft(entityType: string, entityId: string): void {
    const key = this.buildKey(entityType, entityId);
    localStorage.removeItem(key);
    this.draftUpdated$.next({ key, draft: null });
  }

  /**
   * Check if draft exists
   */
  hasDraft(entityType: string, entityId: string): boolean {
    const key = this.buildKey(entityType, entityId);
    return localStorage.getItem(key) !== null;
  }

  /**
   * Get all drafts for an entity type
   */
  getAllDrafts<T>(entityType: string): Draft<T>[] {
    const drafts: Draft<T>[] = [];
    const prefix = this.buildKey(entityType, '');

    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(prefix)) {
        const raw = localStorage.getItem(key);
        if (raw) {
          try {
            drafts.push(JSON.parse(raw));
          } catch (error) {
            console.error('Failed to parse draft:', error);
          }
        }
      }
    }

    return drafts;
  }

  /**
   * Observable for cross-tab draft updates
   */
  onDraftUpdated() {
    return this.draftUpdated$.asObservable().pipe(
      filter(update => update.key !== '')
    );
  }

  /**
   * Get current tab ID
   */
  getTabId(): string {
    return this.tabId;
  }

  private buildKey(entityType: string, entityId: string): string {
    return `${this.STORAGE_PREFIX}${entityType}_${entityId}`;
  }

  private generateTabId(): string {
    return `tab_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private listenToStorageChanges(): void {
    fromEvent<StorageEvent>(window, 'storage')
      .pipe(filter(event => event.key?.startsWith(this.STORAGE_PREFIX) ?? false))
      .subscribe(event => {
        if (!event.key) return;

        const draft = event.newValue ? JSON.parse(event.newValue) : null;
        this.draftUpdated$.next({ key: event.key, draft });
      });
  }

  private handleStorageQuotaExceeded(): void {
    // Clean up old drafts if quota exceeded
    const allKeys: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(this.STORAGE_PREFIX)) {
        allKeys.push(key);
      }
    }

    // Sort by savedAt and remove oldest
    const draftsWithKeys = allKeys
      .map(key => {
        const raw = localStorage.getItem(key);
        if (!raw) return null;
        try {
          const draft = JSON.parse(raw);
          return { key, savedAt: draft.metadata.savedAt };
        } catch {
          return null;
        }
      })
      .filter(Boolean) as { key: string; savedAt: string }[];

    draftsWithKeys.sort((a, b) => 
      new Date(a.savedAt).getTime() - new Date(b.savedAt).getTime()
    );

    // Remove oldest 25%
    const removeCount = Math.ceil(draftsWithKeys.length * 0.25);
    for (let i = 0; i < removeCount; i++) {
      localStorage.removeItem(draftsWithKeys[i].key);
    }
  }
}
