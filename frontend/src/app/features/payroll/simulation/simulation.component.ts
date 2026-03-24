import { Component, signal, inject, ViewChild, ElementRef, AfterViewChecked, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SalarySimulationService } from '@app/core/services/salary-simulation.service';

interface PayElement {
  nom: string;
  type: 'base' | 'prime' | 'deduction' | 'avantage' | 'ni';
  montant: number;
}

interface CalculStep {
  label: string;
  value: string;
}

interface Composition {
  titre: string;
  description: string;
  elements: PayElement[];
  salaireBrut: number;
  totalIndemnites: number;
  brutImposable: number;
  totalRetenues: number;
  coutEmployeur: number;
  salaireNet: number;
  calculSteps: CalculStep[];
  avantages?: string[];
  inconvenients?: string[];
}

interface ClaudeJsonResponse {
  scenarios: Composition[];
}

interface ClaudeErrorResponse {
  error: string;
  message?: string;
  instructions?: string;
  exemples_valides?: string[];
}

interface ChatMessage {
  id: number;
  role: 'user' | 'assistant';
  type: 'text' | 'loading' | 'streaming' | 'success' | 'error';
  text?: string;
  compositions?: Composition[];
  error?: string;
  timestamp: Date;
}

@Component({
  selector: 'app-simulation',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './simulation.component.html',
  styleUrls: ['./simulation.component.css']
})
export class SimulationComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;

  private simulationService = inject(SalarySimulationService);
  private zone = inject(NgZone);
  private translate = inject(TranslateService);

  messages = signal<ChatMessage[]>([]);
  inputText = '';
  private messageCounter = 0;
  private shouldScroll = false;

  suggestions: string[] = [];

  constructor() {
    // Load suggestions from translations
    this.translate.get([
      'simulation.suggestions.suggestion1',
      'simulation.suggestions.suggestion2',
      'simulation.suggestions.suggestion3'
    ]).subscribe(translations => {
      this.suggestions = [
        translations['simulation.suggestions.suggestion1'],
        translations['simulation.suggestions.suggestion2'],
        translations['simulation.suggestions.suggestion3']
      ];
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  private scrollToBottom(): void {
    if (this.messagesContainer?.nativeElement) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }

  useSuggestion(suggestion: string): void {
    this.inputText = suggestion;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text) return;

    // Add user message
    const userMsg: ChatMessage = {
      id: ++this.messageCounter,
      role: 'user',
      type: 'text',
      text,
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, userMsg]);
    this.inputText = '';
    this.shouldScroll = true;

    // Check if it's a simple acknowledgment (thank you, etc.)
    if (this.isSimpleAcknowledgment(text)) {
      const responseId = ++this.messageCounter;
      const responses = [
        'De rien ! N\'hésitez pas si vous avez d\'autres questions sur la paie. 😊',
        'Avec plaisir ! Je reste à votre disposition pour d\'autres simulations.',
        'C\'est un plaisir de vous aider ! À bientôt ! 👋',
        'Je vous en prie ! N\'hésitez pas à revenir pour d\'autres simulations.'
      ];
      const responseMsg: ChatMessage = {
        id: responseId,
        role: 'assistant',
        type: 'text',
        text: responses[Math.floor(Math.random() * responses.length)],
        timestamp: new Date()
      };
      this.messages.update(msgs => [...msgs, responseMsg]);
      this.shouldScroll = true;
      return;
    }

    // Add streaming assistant message
    const loadingId = ++this.messageCounter;
    const loadingMsg: ChatMessage = {
      id: loadingId,
      role: 'assistant',
      type: 'streaming',
      text: '',
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, loadingMsg]);
    this.shouldScroll = true;

    let accumulatedText = '';
    let cardsDisplayed = false;

    this.simulationService.simulateStream(
      text,
      (chunk: string) => {
        console.log('💬 [Component] Chunk reçu:', chunk);
        console.log('📊 [Component] Type du chunk:', typeof chunk);
        this.zone.run(() => {
          accumulatedText += chunk;
          console.log('📄 [Component] Texte accumulé:', accumulatedText);
          // Update the streaming message with accumulated text
          this.replaceMessage(loadingId, {
            id: loadingId,
            role: 'assistant',
            type: 'streaming',
            text: accumulatedText,
            timestamp: new Date()
          });
          this.shouldScroll = true;
          // Try to parse and show cards if JSON is complete
          if (!cardsDisplayed) {
            this.tryParseAndDisplayCards(loadingId, accumulatedText, () => { cardsDisplayed = true; });
          }
        });
      },
      () => {
        console.log('✅ [Component] Streaming terminé');
        this.zone.run(() => {
          if (!cardsDisplayed) {
            console.log('🎴 [Component] Finalisation de l\'affichage des cartes');
            this.finalizeCardDisplay(loadingId, accumulatedText);
          }
          this.shouldScroll = true;
        });
      },
      (error: string) => {
        console.error('❌ [Component] Erreur reçue:', error);
        this.zone.run(() => {
          this.replaceMessage(loadingId, {
            id: loadingId,
            role: 'assistant',
            type: 'error',
            error,
            timestamp: new Date()
          });
          this.shouldScroll = true;
        });
      }
    );
  }

  private replaceMessage(id: number, newMsg: ChatMessage): void {
    this.messages.update(msgs => msgs.map(m => m.id === id ? newMsg : m));
  }

  private tryParseAndDisplayCards(loadingId: number, text: string, onDisplayed: () => void): void {
    console.log('🧪 [tryParseAndDisplayCards] Tentative de parsing du texte:', text.substring(0, 200) + '...');
    try {
      const cleanedJson = this.cleanJsonResponse(text);
      console.log('🧹 [tryParseAndDisplayCards] JSON nettoyé:', cleanedJson.substring(0, 200) + '...');
      
      if (!cleanedJson.endsWith('}}') && !cleanedJson.endsWith('}]}')) {
        console.log('⏳ [tryParseAndDisplayCards] JSON incomplet, en attente de plus de données');
        return;
      }
      
      const jsonData: any = JSON.parse(cleanedJson);
      console.log('✅ [tryParseAndDisplayCards] JSON parsé avec succès:', jsonData);
      
      // Check if Claude returned an error response (unclear request)
      if (jsonData.error) {
        console.log('⚠️ [tryParseAndDisplayCards] Réponse d\'erreur détectée:', jsonData.error);
        const errorMsg = this.extractClaudeErrorMessage(jsonData);
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'error',
          error: errorMsg,
          timestamp: new Date()
        });
        onDisplayed();
        this.shouldScroll = true;
        return;
      }
      
      if (jsonData.scenarios && Array.isArray(jsonData.scenarios) && jsonData.scenarios.length > 0) {
        console.log('✅ [tryParseAndDisplayCards] Scénarios détectés:', jsonData.scenarios.length);
        
        // Normaliser les scénarios
        const normalizedScenarios = jsonData.scenarios.map((scenario: any) => this.normalizeScenario(scenario));
        console.log('🔄 [tryParseAndDisplayCards] Scénarios normalisés:', normalizedScenarios);
        
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'success',
          compositions: normalizedScenarios,
          timestamp: new Date()
        });
        onDisplayed();
        this.shouldScroll = true;
      }
    } catch (error) {
      console.log('⏳ [tryParseAndDisplayCards] Erreur de parsing (en attente):', error);
      /* wait for more data */
    }
  }

  /**
   * Normalise un scénario Gemini vers le format attendu par le frontend
   */
  private normalizeScenario(scenario: any): Composition {
    console.log('🔍 [normalizeScenario] Propriétés du scénario:', Object.keys(scenario));
    console.log('🔍 [normalizeScenario] Valeurs:', {
      brutImposable: scenario.brutImposable,
      totalRetenues: scenario.totalRetenues,
      totalRetenuesSalariales: scenario.totalRetenuesSalariales,
      coutEmployeur: scenario.coutEmployeur,
      coutEmployeurTotal: scenario.coutEmployeurTotal,
      salaireNet: scenario.salaireNet
    });
    const elements: PayElement[] = scenario.elements || [];
    const brutImposable = scenario.brutImposable || 0;
    const totalRetenues = scenario.totalRetenuesSalariales || scenario.totalRetenues || 0;
    // Total indemnités = somme des éléments non imposables (type 'ni')
    const totalIndemnites = elements
      .filter((el: PayElement) => el.type === 'ni')
      .reduce((sum: number, el: PayElement) => sum + Math.abs(el.montant), 0);
    // Salaire brut = brut imposable + indemnités non imposables
    const salaireBrut = brutImposable + totalIndemnites;
    // NET à payer = Brut imposable - Total retenues + Indemnités NI
    // (car les indemnités NI ne sont pas soumises aux retenues)
    const salaireNet = brutImposable - totalRetenues + totalIndemnites;

    console.log('📊 [normalizeScenario] Calculs:', {
      salaireBrut,
      totalIndemnites,
      brutImposable,
      totalRetenues,
      salaireNetCalculé: salaireNet,
      salaireNetGemini: scenario.salaireNet
    });

    return {
      titre: scenario.titre || '',
      description: scenario.description || '',
      elements,
      salaireBrut,
      totalIndemnites,
      brutImposable,
      totalRetenues,
      coutEmployeur: scenario.coutEmployeurTotal || scenario.coutEmployeur || 0,
      salaireNet,
      calculSteps: scenario.calculSteps || [],
      avantages: scenario.avantages || [],
      inconvenients: scenario.inconvenients || []
    };
  }

  private finalizeCardDisplay(loadingId: number, text: string): void {
    console.log('🎯 [finalizeCardDisplay] Finalisation avec le texte:', text.substring(0, 200) + '...');
    try {
      const cleanedJson = this.cleanJsonResponse(text);
      console.log('🧹 [finalizeCardDisplay] JSON nettoyé:', cleanedJson.substring(0, 200) + '...');
      
      const jsonData: any = JSON.parse(cleanedJson);
      console.log('✅ [finalizeCardDisplay] JSON parsé:', jsonData);
      
      // Check if Claude returned an error response (unclear request)
      if (jsonData.error) {
        console.log('⚠️ [finalizeCardDisplay] Erreur détectée:', jsonData.error);
        const errorMsg = this.extractClaudeErrorMessage(jsonData);
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'error',
          error: errorMsg,
          timestamp: new Date()
        });
        return;
      }
      
      if (jsonData.scenarios && Array.isArray(jsonData.scenarios)) {
        console.log('✅ [finalizeCardDisplay] Affichage de', jsonData.scenarios.length, 'scénarios');
        
        // Normaliser les scénarios pour mapper les propriétés Gemini vers le format attendu
        const normalizedScenarios = jsonData.scenarios.map((scenario: any) => this.normalizeScenario(scenario));
        console.log('🔄 [finalizeCardDisplay] Scénarios normalisés:', normalizedScenarios);
        
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'success',
          compositions: normalizedScenarios,
          timestamp: new Date()
        });
      } else {
        console.error('❌ [finalizeCardDisplay] Scénarios manquants dans:', jsonData);
        throw new Error('scenarios manquant');
      }
    } catch (error) {
      console.error('❌ [finalizeCardDisplay] Erreur lors du parsing:', error);
      console.error('📄 [finalizeCardDisplay] Texte problématique:', text);
      this.replaceMessage(loadingId, {
        id: loadingId,
        role: 'assistant',
        type: 'error',
        error: this.translate.instant('simulation.errors.invalidFormat'),
        timestamp: new Date()
      });
    }
  }

  private cleanJsonResponse(text: string): string {
    console.log('🧼 [cleanJsonResponse] Texte original:', text.substring(0, 100) + '...');
    let cleaned = text.trim();
    if (cleaned.startsWith('```json')) cleaned = cleaned.substring(7);
    else if (cleaned.startsWith('```')) cleaned = cleaned.substring(3);
    if (cleaned.endsWith('```')) cleaned = cleaned.substring(0, cleaned.length - 3);
    const firstBrace = cleaned.indexOf('{');
    const lastBrace = cleaned.lastIndexOf('}');
    if (firstBrace !== -1 && lastBrace !== -1) cleaned = cleaned.substring(firstBrace, lastBrace + 1);
    const result = cleaned.trim();
    console.log('✨ [cleanJsonResponse] Texte nettoyé:', result.substring(0, 100) + '...');
    return result;
  }

  private extractClaudeErrorMessage(errorData: ClaudeErrorResponse): string {
    let msg = errorData.message || errorData.error || 'Demande non claire';
    
    if (errorData.instructions) {
      msg += '\n\n' + errorData.instructions;
    }
    
    if (errorData.exemples_valides && errorData.exemples_valides.length > 0) {
      msg += '\n\nExemples :';
      errorData.exemples_valides.forEach(ex => {
        msg += '\n• ' + ex;
      });
    }
    
    return msg;
  }

  formatAmount(amount: number): string {
    // Handle undefined, null, or NaN values
    if (amount === undefined || amount === null || isNaN(amount)) {
      return '0.00 DH';
    }
    return new Intl.NumberFormat('fr-MA', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(Math.abs(amount)) + ' DH';
  }

  getTypeBadgeClass(type: PayElement['type']): string {
    const classes = {
      base: 'bg-green-100 text-green-800 border-green-200',
      prime: 'bg-yellow-100 text-yellow-800 border-yellow-200',
      deduction: 'bg-red-100 text-red-800 border-red-200',
      avantage: 'bg-purple-100 text-purple-800 border-purple-200',
      ni: 'bg-blue-100 text-blue-800 border-blue-200'
    };
    return classes[type];
  }

  getTypeLabel(type: PayElement['type']): string {
    const labelKey = `simulation.elementTypes.${type}`;
    return this.translate.instant(labelKey);
  }

  getAmountClass(amount: number): string {
    return amount < 0 ? 'text-red-600' : 'text-green-600';
  }

  getAmountSign(amount: number): string {
    return amount < 0 ? '−' : '+';
  }

  private isSimpleAcknowledgment(text: string): boolean {
    const lower = text.toLowerCase().trim();
    const acknowledgments = [
      'merci', 'thanks', 'thank you', 'thx', 'ty',
      'شكرا', 'choukran', 'baraka allaho fik',
      'ok merci', 'parfait', 'super', 'génial',
      'd\'accord', 'ok', 'oki', 'okay'
    ];
    return acknowledgments.some(ack => 
      lower === ack || 
      lower.startsWith(ack + ' ') || 
      lower.endsWith(' ' + ack) ||
      lower.startsWith(ack + '!') ||
      lower.startsWith(ack + '.')
    );
  }
}

