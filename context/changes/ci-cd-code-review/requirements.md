## Overall concept

- GHA workflow run for every new pull request to master
- composite action for the review itself so that main workflow is easy to reason about

## Input parameters

- pull request title
- pull request description (?? cost tradeoff)
- git diff

## Code Review Criteria

Each criterion is scored on a 1–10 scale, where 1 is the worst outcome and 10 is the best.

- **Implementation correctness**: Poprawność implementacji — czy kod robi dokładnie to, co deklaruje. (Skala 1–10)
- **Readability & clean code**: Czytelność kodu — przejrzystość nazewnictwa, organizacja kodu, formatowanie oraz brak martwego/zbędnego kodu. (Skala 1–10)
- **Complexity**: Złożoność — prostota rozwiązania w odniesieniu do złożoności problemu. (Skala 1–10)
- **Performance & resource efficiency**: Wydajność — optymalne wykorzystanie zasobów, zapytania do bazy danych oraz unikanie blokujących wywołań synchronicznych. (Skala 1–10)
- **Security & safety**: Bezpieczeństwo — brak podatności (np. SQL Injection, XSS), bezpieczna obsługa danych i brak zahardkodowanych sekretów/danych logowania. (Skala 1–10)
- **Idiomaticity**: Idiomatyczność — zgodność z konwencjami wybranego języka i wytycznymi projektu. (Skala 1–10)

## Parked for later

- business alignment (require broader context)
- architectural fit (require broader context)

## Expected side-effects

- PR comment with summary
- labels: `ai-cr:failed` (red) OR `ai-cr:passed` (green)

## Expected behavior

- on-demand retry when label `ai-cr:review` is added
