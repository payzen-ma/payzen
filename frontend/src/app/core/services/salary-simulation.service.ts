import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { AuthService } from './auth.service';

/**
 * Requête de simulation avec règles personnalisées
 */
export interface SimulationRequest {
  regleContent: string;
  instruction: string;
}

/**
 * Requête de simulation rapide avec règles par défaut
 */
export interface QuickSimulationRequest {
  instruction: string;
}

/**
 * Réponse de simulation de paie
 */
export interface SimulationResponse {
  success: boolean;
  result?: any;  // Changed from string to any to handle deserialized JSON
  errorMessage?: string;
  timestamp: string;
}

/**
 * Réponse contenant les règles DSL
 */
export interface RulesResponse {
  success: boolean;
  content?: string;
  filePath?: string;
  lastModified?: string;
}

/**
 * Service pour la simulation de salaire avec Claude via l'API backend
 */
@Injectable({
  providedIn: 'root'
})
export class SalarySimulationService {
  private readonly API_URL = `${environment.apiUrl}/claudesimulation`;
  private authService = inject(AuthService);

  constructor(private http: HttpClient) {}

  /**
   * Simule des compositions de salaire avec règles personnalisées
   * @param request Requête contenant les règles DSL et l'instruction
   */
  simulate(request: SimulationRequest): Observable<SimulationResponse> {
    return this.http.post<SimulationResponse>(
      `${this.API_URL}/simulate`,
      request
    );
  }

  /**
   * Simule des compositions de salaire avec les règles système (recommend)
   * @param instruction Instruction de l'utilisateur (ex: "Je veux un net de 10000 DH")
   */
  simulateQuick(instruction: string): Observable<SimulationResponse> {
    const request: QuickSimulationRequest = { instruction };
    return this.http.post<SimulationResponse>(
      `${this.API_URL}/simulate-quick`,
      request
    );
  }

  /**
   * Récupère le contenu des règles DSL de paie
   */
  getRules(): Observable<RulesResponse> {
    return this.http.get<RulesResponse>(`${this.API_URL}/rules`);
  }

  /**
   * Simule avec HTTP standard (anciennement streaming)
   * @param instruction Instruction de l'utilisateur
   * @param onChunk Callback appelé pour chaque chunk reçu (simulé pour compatibilité)
   * @param onComplete Callback appelé à la fin
   * @param onError Callback appelé en cas d'erreur
   */
  simulateStream(
    instruction: string,
    onChunk: (chunk: string) => void,
    onComplete: () => void,
    onError: (error: string) => void
  ): void {
    console.log('🚀 [simulateStream] Début de la requête avec instruction:', instruction);
    
    const request: QuickSimulationRequest = { instruction };
    
    this.http.post<SimulationResponse>(
      `${this.API_URL}/simulate-stream`,
      request
    ).subscribe({
      next: (response) => {
        console.log('📥 [simulateStream] Réponse brute reçue:', response);
        console.log('📊 [simulateStream] Type de response.result:', typeof response.result);
        console.log('📊 [simulateStream] Contenu de response.result:', response.result);
        
        if (!response.success) {
          console.error('❌ [simulateStream] Erreur dans la réponse:', response.errorMessage);
          onError(response.errorMessage || 'Erreur inconnue');
          return;
        }
        
        // Le result est déjà un objet désérialisé (pas une string)
        const jsonResult = response.result;
        console.log('📋 [simulateStream] JSON résultat:', jsonResult);
        
        // Convertir en string pour le callback onChunk (compatibilité)
        const jsonString = JSON.stringify(jsonResult, null, 2);
        console.log('📝 [simulateStream] JSON string à envoyer:', jsonString);
        
        // Simuler le streaming en envoyant le texte complet d'un coup
        onChunk(jsonString);
        onComplete();
      },
      error: (error) => {
        console.error('❌ [simulateStream] Erreur HTTP:', error);
        onError(error.error?.errorMessage || error.message || 'Erreur lors de la simulation');
      }
    });
  }
}
