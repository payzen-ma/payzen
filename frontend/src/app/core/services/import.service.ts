import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ModuleImportRequestParams {
    month: number;
    year: number;
    mode?: 'monthly' | 'bi_monthly';
    half?: number;
    companyId?: number;
    sendWelcomeEmail?: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class ImportService {

    private readonly baseUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    uploadModuleFile(file: File, params: ModuleImportRequestParams): Observable<any> {
        const formData = new FormData();
        formData.append('file', file, file.name);

        const search = new URLSearchParams();
        search.set('month', String(params.month));
        search.set('year', String(params.year));
        search.set('mode', params.mode ?? 'monthly');

        if (params.mode === 'bi_monthly' && (params.half === 1 || params.half === 2))
            search.set('half', String(params.half));
        if (params.companyId)
            search.set('companyId', String(params.companyId));
        search.set('sendWelcomeEmail', String(!!params.sendWelcomeEmail));

        return this.http.post(`${this.baseUrl}/import/module?${search.toString()}`, formData);
    }

    downloadModuleTemplate(companyId?: number): Observable<HttpResponse<Blob>> {
        const search = new URLSearchParams();
        if (companyId)
            search.set('companyId', String(companyId));

        const qs = search.toString();
        const url = `${this.baseUrl}/import/module/template${qs ? `?${qs}` : ''}`;
        return this.http.get(url, {
            responseType: 'blob',
            observe: 'response'
        });
    }

    /**
     * Get import details by ID
     * @param importId Import ID
     * @returns Observable with import details
     */
    getImportDetails(importId: string): Observable<any> {
        return this.http.get(`/api/import/${importId}`);
    }

    /**
     * Validate CSV file format
     * @param file CSV file to validate
     * @returns Promise with validation result
     */
    validateCSV(file: File): Promise<{ valid: boolean; errors: string[] }> {
        return new Promise((resolve) => {
            const reader = new FileReader();

            reader.onload = (event: any) => {
                const csv = event.target.result;
                const lines = csv.split('\n');
                const errors: string[] = [];

                // Check if file is empty
                if (lines.length < 2) {
                    errors.push('Le fichier CSV doit contenir au moins une ligne de données');
                    resolve({ valid: false, errors });
                    return;
                }

                // Check header
                const headers = lines[0].split(',');
                const requiredHeaders = ['Nom', 'Prenom', 'Email', 'Departement'];
                const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));

                if (missingHeaders.length > 0) {
                    errors.push(`En-têtes manquants: ${missingHeaders.join(', ')}`);
                }

                resolve({ valid: errors.length === 0, errors });
            };

            reader.readAsText(file);
        });
    }
}
