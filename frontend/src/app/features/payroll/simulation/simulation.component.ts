import { Component, signal, inject, ViewChild, ElementRef, AfterViewChecked, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
  brut_imposable: number;
  total_retenues: number;
  cout_employeur: number;
  salaire_net: number;
  calcul_steps: CalculStep[];
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
  imports: [CommonModule, FormsModule],
  templateUrl: './simulation.component.html',
  styleUrls: ['./simulation.component.css']
})
export class SimulationComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;

  private simulationService = inject(SalarySimulationService);
  private zone = inject(NgZone);

  messages = signal<ChatMessage[]>([]);
  inputText = '';
  private messageCounter = 0;
  private shouldScroll = false;

  suggestions = [
    'Je veux un salaire net de 10 000 DH',
    'Proposer des formules pour un net de 15 000 DH',
    'Simuler un salaire net de 8 500 DH avec optimisation fiscale'
  ];

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
        this.zone.run(() => {
          accumulatedText += chunk;
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
        this.zone.run(() => {
          if (!cardsDisplayed) {
            this.finalizeCardDisplay(loadingId, accumulatedText);
          }
          this.shouldScroll = true;
        });
      },
      (error: string) => {
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
    try {
      const cleanedJson = this.cleanJsonResponse(text);
      if (!cleanedJson.endsWith('}}') && !cleanedJson.endsWith('}]}')) return;
      const jsonData: any = JSON.parse(cleanedJson);
      
      // Check if Claude returned an error response (unclear request)
      if (jsonData.error) {
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
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'success',
          compositions: jsonData.scenarios,
          timestamp: new Date()
        });
        onDisplayed();
        this.shouldScroll = true;
      }
    } catch { /* wait for more data */ }
  }

  private finalizeCardDisplay(loadingId: number, text: string): void {
    try {
      const cleanedJson = this.cleanJsonResponse(text);
      const jsonData: any = JSON.parse(cleanedJson);
      
      // Check if Claude returned an error response (unclear request)
      if (jsonData.error) {
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
        this.replaceMessage(loadingId, {
          id: loadingId,
          role: 'assistant',
          type: 'success',
          compositions: jsonData.scenarios,
          timestamp: new Date()
        });
      } else {
        throw new Error('scenarios manquant');
      }
    } catch {
      this.replaceMessage(loadingId, {
        id: loadingId,
        role: 'assistant',
        type: 'error',
        error: 'Erreur lors de l\'analyse de la réponse. Format JSON invalide.',
        timestamp: new Date()
      });
    }
  }

  private cleanJsonResponse(text: string): string {
    let cleaned = text.trim();
    if (cleaned.startsWith('```json')) cleaned = cleaned.substring(7);
    else if (cleaned.startsWith('```')) cleaned = cleaned.substring(3);
    if (cleaned.endsWith('```')) cleaned = cleaned.substring(0, cleaned.length - 3);
    const firstBrace = cleaned.indexOf('{');
    const lastBrace = cleaned.lastIndexOf('}');
    if (firstBrace !== -1 && lastBrace !== -1) cleaned = cleaned.substring(firstBrace, lastBrace + 1);
    return cleaned.trim();
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
    const labels = {
      base: 'Base',
      prime: 'Prime',
      deduction: 'Retenue',
      avantage: 'Avantage',
      ni: 'Non imposable'
    };
    return labels[type];
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

