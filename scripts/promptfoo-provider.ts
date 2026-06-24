import { ApiProvider, ProviderResponse } from 'promptfoo';
import { review } from './review-agent.js';

export default class CodeReviewerProvider implements ApiProvider {
  private model: string;

  constructor(options: { id: string; config?: { model?: string } }) {
    this.model = options.config?.model || 'gemini-3.1-flash-lite';
  }

  id() {
    return `code-reviewer:${this.model}`;
  }

  async callApi(prompt: string, options?: any, context?: any): Promise<ProviderResponse> {
    const diff = context?.vars?.diff || '';
    try {
      const result = await review(diff, { model: this.model });
      return {
        output: JSON.stringify(result)
      };
    } catch (error: any) {
      return {
        error: error.message
      };
    }
  }
}
