import { GoogleGenAI } from "@google/genai";

export const SYSTEM_PROMPT = `Jesteś precyzyjnym, konstruktywnym recenzentem kodu oceniającym pull request.
Oceń podany diff w sześciu kryteriach w skali 1-10 (1 = poważne braki, 10 = wzorowo):
poprawność implementacji, czytelność kodu, złożoność, wydajność, bezpieczeństwo, idiomatyczność.
Następnie wydaj wiążący werdykt (pass/fail) dla całej zmiany i dołącz krótkie podsumowanie (2-3 zdania)
w Markdown, na podstawie którego autor PR-a będzie mógł działać.`;

export const REVIEW_JSON_SCHEMA = {
  type: "object",
  properties: {
    implementationCorrectness: {
      type: "number",
      description: "Poprawność implementacji: czy kod robi to, co deklaruje (skala 1-10)"
    },
    readabilityAndCleanCode: {
      type: "number",
      description: "Czytelność kodu: przejrzystość nazewnictwa, organizacja kodu, formatowanie, brak martwego kodu (skala 1-10)"
    },
    complexity: {
      type: "number",
      description: "Złożoność: prostota rozwiązania względem problemu (skala 1-10)"
    },
    performanceAndResourceEfficiency: {
      type: "number",
      description: "Wydajność: optymalne wykorzystanie zasobów i unikanie blokujących wywołań synchronicznych (skala 1-10)"
    },
    securitySafety: {
      type: "number",
      description: "Bezpieczeństwo: brak podatności i wycieków sekretów (skala 1-10)"
    },
    idiomaticity: {
      type: "number",
      description: "Idiomatyczność: zgodność z konwencjami języka i projektu (skala 1-10)"
    },
    verdict: {
      type: "string",
      enum: ["pass", "fail"],
      description: "Wiążący werdykt dla całej zmiany"
    },
    summary: {
      type: "string",
      description: "Podsumowanie w Markdown, gotowe jako komentarz do PR-a"
    }
  },
  required: [
    "implementationCorrectness",
    "readabilityAndCleanCode",
    "complexity",
    "performanceAndResourceEfficiency",
    "securitySafety",
    "idiomaticity",
    "verdict",
    "summary"
  ]
} as const;

export interface Review {
  implementationCorrectness: number;
  readabilityAndCleanCode: number;
  complexity: number;
  performanceAndResourceEfficiency: number;
  securitySafety: number;
  idiomaticity: number;
  verdict: "pass" | "fail";
  summary: string;
}

// Proces review na podstawie git diffa
export async function review(diff: string, options?: { model?: string }): Promise<Review> {
  const apiKey = process.env.GEMINI_API_KEY || process.env.GOOGLE_API_KEY;
  if (!apiKey) {
    throw new Error("Brak klucza API. Ustaw zmienną środowiskową GEMINI_API_KEY lub GOOGLE_API_KEY.");
  }

  const ai = new GoogleGenAI({ apiKey });
  const modelName = options?.model || "gemini-3.1-flash-lite";

  const prTitle = process.env.PR_TITLE || "brak";
  let prDescription = process.env.PR_DESCRIPTION || "brak";
  if (prDescription.length > 2000) {
    prDescription = prDescription.substring(0, 2000) + "... (obcięto ze względu na rozmiar)";
  }

  // Wypisujemy pobrane zmienne do stderr, aby nie zakłócić parsowania JSON na stdout
  console.error(`[INFO] Odczytane metadane PR w skrypcie:`);
  console.error(`  - Tytuł: "${prTitle}"`);
  console.error(`  - Opis: "${prDescription}"\n`);

  const promptContent = `Oto Pull Request do zrecenzowania.

Tytuł PR: ${prTitle}
Opis PR: ${prDescription}

Diff zmian:
\`\`\`diff
${diff}
\`\`\``;

  const response = await ai.models.generateContent({
    model: modelName,
    contents: promptContent,
    config: {
      systemInstruction: SYSTEM_PROMPT,
      responseMimeType: "application/json",
      responseSchema: REVIEW_JSON_SCHEMA as any,
    },
  });

  const text = response.text;
  if (!text) {
    throw new Error("Model Gemini nie zwrócił żadnego wyniku.");
  }

  // Parsowanie odpowiedzi do typu Review
  const parsedJson = JSON.parse(text) as Review;

  // Prosta walidacja obecności wymaganych pól (fail-fast) bez użycia Zoda
  const requiredKeys: (keyof Review)[] = [
    "implementationCorrectness",
    "readabilityAndCleanCode",
    "complexity",
    "performanceAndResourceEfficiency",
    "securitySafety",
    "idiomaticity",
    "verdict",
    "summary"
  ];

  for (const key of requiredKeys) {
    if (parsedJson[key] === undefined) {
      throw new Error(`Niepoprawny structured output od Gemini: brak pola '${key}'`);
    }
  }

  return parsedJson;
}
