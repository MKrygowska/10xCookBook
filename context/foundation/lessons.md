# Lessons Learned

> Append-only register of recurring rules and patterns. Re-read at start by /10x-frame, /10x-research, /10x-plan, /10x-plan-review, /10x-implement, /10x-impl-review.

## Preferuj natywne funkcje JS/TS zamiast lodash

- **Context**: Implementacja funkcji w aplikacji TypeScript po stronie frontendu i backendu.
- **Problem**: Agent użył `_.filter()`, mimo że lodash nie jest częścią projektu. To dodałoby niepotrzebną zależność i rozjechało lokalną konwencję pracy z natywnymi API.
- **Rule**: Nie dodawaj lodash bez jasnego wskazania. Projekt preferuje natywne funkcje JS/TS w standardzie 2026+.
- **Applies to**: plan, implement, impl-review

## Twórz osobne pliki .html z kodem HTML zamiast w plikach .ts

- **Context**: Implementacja funkcji w aplikacji TypeScript po stronie frontendu.
- **Problem**: Agent umieścił kod HTML w pliku ts, zamiast utworzyć nowy plik html i tam umieścić kod.
- **Rule**: Przy tworzeniu kodu HTML zawsze dodawaj go w osobnym pliku .html.
- **Applies to**: plan, implement, impl-review

## Zawsze twórz osobne branche dla kolejnych sliceów

- **Context**: Rozpoczynanie pracy nad kolejnym slicem
- **Problem**: Agent rozpoczął pracę nad nowym slicem na starym branchu, który został już zmergowany do maina.
- **Rule**: Przy rozpoczynaniu pracy nad nowym slicem zawsze twórz nowy branch z najświeższego maina.
- **Applies to**: plan, implement, impl-review

## Obsługuj poprawnie Base64URL przy dekodowaniu JWT na frontendzie

- **Context**: Dekodowanie payloadu tokenów JWT (Base64URL) w TypeScript/JavaScript.
- **Problem**: Bezpośrednie użycie `atob(tokenPayload)` rzuca wyjątek `DOMException: The string to be decoded is not correctly encoded.` jeśli długość Base64URL nie jest wielokrotnością 4 (Base64URL usuwa dopełnienie `=`) lub zawiera znaki `-` / `_`.
- **Rule**: Zawsze zamieniaj znaki Base64URL (`-` na `+`, `_` na `/`) i dopełniaj ciąg znakami `=` do wielokrotności 4 przed wywołaniem `atob()`.
- **Applies to**: plan, implement, impl-review