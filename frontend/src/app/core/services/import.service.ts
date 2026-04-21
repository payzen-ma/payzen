import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ImportService {

    private readonly baseUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    /**
     * Upload CSV file for import
     * @param file CSV file to upload
     * @returns Observable with import result
     */
    uploadCSV(file: File): Observable<any> {
        const formData = new FormData();
        formData.append('file', file, file.name);

        return this.http.post(`${this.baseUrl}/import/absences`, formData);
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
