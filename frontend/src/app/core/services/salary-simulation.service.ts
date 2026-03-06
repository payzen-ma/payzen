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
  result?: string;
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
   * Simule avec streaming (Server-Sent Events)
   * @param instruction Instruction de l'utilisateur
   * @param onChunk Callback appelé pour chaque chunk reçu
   * @param onComplete Callback appelé à la fin
   * @param onError Callback appelé en cas d'erreur
   */
  simulateStream(
    instruction: string,
    onChunk: (chunk: string) => void,
    onComplete: () => void,
    onError: (error: string) => void
  ): EventSource {
    const url = `${this.API_URL}/simulate-stream`;
    
    // Construire les headers avec authentification
    const headers: Record<string, string> = {
      'Content-Type': 'application/json'
    };
    
    const token = this.authService.getToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    
    // Option simple : utiliser fetch avec stream
    fetch(url, {
      method: 'POST',
      headers,
      body: JSON.stringify({ instruction })
    }).then(async (response) => {
      const reader = response.body?.getReader();
      const decoder = new TextDecoder();

      if (!reader) {
        onError('Impossible de lire le stream');
        return;
      }

      try {
        while (true) {
          const { done, value } = await reader.read();
          
          if (done) {
            onComplete();
            break;
          }

          // Décoder et traiter les données SSE
          const text = decoder.decode(value, { stream: true });
          const lines = text.split('\n');

          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const data = line.substring(6);
              try {
                const parsed = JSON.parse(data);
                
                if (parsed.error) {
                  onError(parsed.error);
                  reader.cancel();
                  return;
                }
                
                if (parsed.done) {
                  onComplete();
                  return;
                }
                
                if (parsed.chunk) {
                  onChunk(parsed.chunk);
                }
              } catch (e) {
                // Ignorer les erreurs de parsing
              }
            }
          }
        }
      } catch (error) {
        onError('Erreur lors de la lecture du stream');
      }
    }).catch((error) => {
      onError(`Erreur réseau: ${error.message}`);
    });

    // Retourner un objet vide pour la compatibilité
    return null as any;
  }
}
