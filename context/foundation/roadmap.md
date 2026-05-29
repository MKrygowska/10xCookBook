---
project: "10xCookBook"
version: 1
status: draft
created: 2026-05-25
updated: 2026-05-29
prd_version: 1
main_goal: low-complexity
top_blocker: time
---

# Mapa drogowa: 10xCookBook

> Opracowano na podstawie `context/foundation/prd.md` (v1) oraz automatycznej analizy stanu kodu bazy.
> Dokument podlega edycji w miejscu realizacji projektu; zostanie zarchiwizowany po zakończeniu prac.
> Wycinki funkcjonalne i fundamenty są wymienione w porządku ich zależności technologicznych. Tabela "W skrócie" stanowi indeks mapy.

## Podsumowanie wizji

10xCookBook rozwiązuje problem marnowania żywności i paraliżu decyzyjnego domowych kucharzy stojących przed otwartą lodówką. Poprzez podejście „najpierw składniki” (ingredient-first), aplikacja dopasowuje dostępne produkty do przepisów, sortując je według procentu wykorzystania posiadanych zapasów. Pozwala to na szybkie i ekonomiczne przygotowanie posiłków bez konieczności robienia dodatkowych zakupów.

## Gwiazda przewodnia

**S-01: Wyszukiwanie przepisów po składnikach (publicznych)** — Użytkownik loguje się, wpisuje posiadane składniki i otrzymuje posortowaną listę publicznych przepisów według procentu dopasowania. 

> *Gwiazda przewodnia* to najmniejszy pionowy wycinek funkcjonalny (vertical slice), którego pomyślne dostarczenie udowadnia podstawową hipotezę biznesową aplikacji. Jest umieszczony w mapie drogowej tak wcześnie, jak to możliwe ze względu na zależności, ponieważ cała reszta prac ma sens tylko wtedy, gdy ten rdzeń działa poprawnie.

## W skrócie

| ID | Change ID | Rezultat (użytkownik może...) | Wymagania | Powiązanie z PRD | Status |
|---|---|---|---|---|---|
| F-01 | auth-foundation | (fundament) Autoryzacja JWT i schemat użytkownika | — | FR-007, FR-008 | done |
| F-02 | database-foundation | (fundament) Konfiguracja bazy danych EF Core i migracje | F-01 | FR-004 | proposed |
| F-03 | deployment-ci-cd | (fundament) Automatyzacja wdrożenia GitHub Actions na Azure | — | NFR (Dostępność) | done |
| S-01 | public-recipe-matching | Wyszukiwanie przepisów po składnikach (publiczne) | F-01, F-02 | US-01, FR-001, FR-002 | done |
| S-02 | private-recipe-crud | Tworzenie i edycja własnych prywatnych przepisów (CRUD) | S-01 | FR-005, FR-006 | done |
| S-03 | unified-matching | Wyszukiwanie przepisów po składnikach (publiczne + prywatne) | S-01, S-02 | FR-003, Guardrail (Izolacja) | done |
| S-04 | user-data-retention | Retencja danych użytkownika (samodzielne usuwanie i czyszczenie nieaktywnych kont) | F-01, F-02 | RODO/GDPR Compliance | done |
| S-05 | ui-button-alignment | Poprawienie spójności i wyrównania przycisków na dashboardzie | S-03 | UX/UI Polish | done |
| F-04 | backend-controllers-refactor | (fundament) Refaktoryzacja API do klasycznych kontrolerów i odchudzenie punktów końcowych | S-03 | Refaktoryzacja architektury | done |

## Strumienie

Nawigacja ułatwiająca czytanie — grupuje elementy współdzielące łańcuch wymagań wstępnych. Porządek kanoniczny wciąż definiuje graf zależności w dalszych sekcjach; ta tabela to sugerowana kolejność pracy na równoległych torach.

| Strumień | Temat | Łańcuch ID | Uwaga |
|---|---|---|---|
| A | Autoryzacja i Rdzeń Dopasowywania | `F-01` → `F-02` → `S-01` | Budowa kręgosłupa aplikacji i wdrożenie naszej *gwiazdy przewodniej*. |
| B | Personalizacja Książki Kucharskiej | `S-02` → `S-03` | Zarządzanie prywatnymi przepisami i ich scalenie z silnikiem wyszukiwania. Łączy się ze Strumieniem A w punkcie `S-01`. |
| C | Automatyzacja Operacyjna | `F-03` | Konfiguracja CI/CD na Azure. Niezależny tor, może działać w pełni równolegle. |
| D | Prywatność i Zgodność z RODO | `S-04` | Samodzielne usuwanie konta oraz retencja nieaktywnych danych. Niezależny tor po wdrożeniu `F-01`/`F-02`. |

## Stan bazowy

Bieżący stan kodu zweryfikowany automatycznie na dzień 2026-05-25. Fundamenty opisane poniżej bazują na tych komponentach i nie będą ich powtórnie tworzyć.

- **Frontend:** **obecny** — Gotowy szkielet Angular 17 client SPA, skonfigurowany router (pusty) w [package.json](file:///C:/Users/reade/Documents/10xDev%20Project/frontend/package.json).
- **Backend / API:** **obecny** — Działający szablon ASP.NET Core Web API z włączonym Swaggerem, minimalnym WeatherForecast API i skonfigurowaną polityką CORS w [Program.cs](file:///C:/Users/reade/Documents/10xDev%20Project/backend/Program.cs).
- **Dane:** **nieobecne** — Brak integracji z EF Core, bazy danych, DbContext czy migracji po stronie backendu C#.
- **Autoryzacja:** **nieobecne** — Brak obsługi rejestracji, sesji/tokenów JWT oraz middleware sprawdzającego tożsamość.
- **Wdrożenie / Infrastruktura:** **częściowe** — Istnieje w pełni przygotowany, darmowy plan wdrożenia infrastruktury Azure w [deploy-plan.md](file:///C:/Users/reade/Documents/10xDev%20Project/context/deployment/deploy-plan.md) (środowiska są już częściowo wdrożone/zweryfikowane live), jednak w kodzie nie ma jeszcze skryptów CI/CD.
- **Obserwowalność:** **nieobecne** — Brak podłączonego Application Insights, zewnętrznych loggerów czy metryk.

## Fundamenty

### F-01: Autoryzacja JWT i Schemat Użytkownika

- **Rezultat:** (fundament) Standardowe mechanizmy rejestracji i logowania użytkownika za pomocą adresu e-mail i hasła. Konfiguracja JWT na backendzie (ASP.NET Core) i interceptora HTTP po stronie frontendu (Angular) do przesyłania tokena.
- **Change ID:** `auth-foundation`
- **Powiązanie z PRD:** FR-007, FR-008, Sekcja Access Control (Authentication)
- **Odblokowuje:** F-02, S-01, S-02
- **Wymagania:** —
- **Równolegle z:** F-03
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Pierwsza integracja autoryzacji tokenowej JWT z Angular 17. Zgodnie z celem `low-complexity` musimy wdrożyć standardowe, lekkie rozwiązanie bez wprowadzania ciężkich struktur zewnętrznych.
- **Status:** done

### F-02: Konfiguracja Bazy Danych i ORM (EF Core)

- **Rezultat:** (fundament) Integracja Entity Framework Core (EF Core) z dostawcą SQL Server w backendzie ASP.NET Core. Konfiguracja połączenia z bazą danych Azure SQL Serverless. Utworzenie pierwszych migracji dla struktur bazodanowych (Users, Recipes, Ingredients) oraz zasianie (seed) bazy danych początkowym zestawem publicznych przepisów.
- **Change ID:** `database-foundation`
- **Powiązanie z PRD:** FR-004, Sekcja Access Control (Global/Shared Recipes)
- **Odblokowuje:** S-01, S-02
- **Wymagania:** F-01
- **Równolegle z:** F-03
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Uruchamianie automatycznych migracji w runtime z poziomu kodu API na Azure SQL Serverless może napotkać błędy uprawnień (ograniczenia darmowych subskrypcji). Mitygacja: wygenerowanie skryptów migracji SQL lokalnie i ich aplikacja przez Query Editor.
- **Status:** proposed

### F-03: Automatyzacja Wdrożenia (GitHub Actions CI/CD)

- **Rezultat:** (fundament) Skonfigurowane i w pełni przetestowane przepływy pracy (workflows) w folderze `.github/workflows/` umożliwiające budowanie oraz wdrażanie aplikacji: Angular do Azure Static Web Apps, a C# Web API do Azure App Service przy każdym scaleniu do gałęzi `main`.
- **Change ID:** `deployment-ci-cd`
- **Powiązanie z PRD:** NFR (Dostępność, 99.5%), `deploy-plan.md`
- **Odblokowuje:** Ciągłą weryfikację na środowisku staging/produkcyjnym
- **Wymagania:** —
- **Równolegle z:** F-01, F-02
- **Blokady:** Konieczność ręcznej konfiguracji sekretów wdrożeniowych (secrets) po stronie repozytorium GitHub użytkownika.
- **Niewiadome:** Czy repozytorium zdalne GitHub zostało już przygotowane.
- **Ryzyko:** Przekroczenie darmowych minut kompilacji GitHub Actions lub limity czasu wdrożenia Azure Static Web Apps. Mitygacja: czyste skrypty budujące bez uruchamiania ciężkich testów integracyjnych w chmurze.
- **Status:** done

## Wycinki

### S-01: Wyszukiwanie przepisów po składnikach (publiczne)

- **Rezultat:** Zalogowany użytkownik może wpisać listę posiadanych składników w panelu wyszukiwarki (frontend) i otrzymać listę przepisów publicznych z bazy danych, posortowanych malejąco według procentu dopasowania. Aplikacja graficznie wyróżnia posiadane składniki oraz te, których brakuje.
- **Change ID:** `public-recipe-matching`
- **Powiązanie z PRD:** US-01, FR-001, FR-002, Sukcesy Główne (Match Rate, Speed <500ms)
- **Wymagania:** F-01, F-02
- **Równolegle z:** —
- **Blokady:** —
- **Niewiadome:**
  - Jaki typ UI (np. tagi z autouzupełnianiem) będzie najbardziej ergonomiczny na urządzeniach mobilnych o szerokości od 360px bez TailwindCSS? — Właściciel: deweloper. Blokuje: nie.
- **Ryzyko:** Wykonywanie skomplikowanych złączeń (JOIN) na bazie SQL przy wyliczaniu przecięcia zbiorów w locie może przekroczyć limit 500ms. Mitygacja: pobranie przepisów i składników do pamięci backendu i wykonanie prostego, szybkiego wyliczenia matematycznego w C# (zgodnie z decyzją z `shape-notes`).
- **Status:** proposed

### S-02: Tworzenie i edycja własnych prywatnych przepisów (CRUD)

- **Rezultat:** Zalogowany użytkownik może przejść do swojego konta, wyświetlić listę prywatnych przepisów i zarządzać nimi (dodawanie z formularzem autouzupełniania składników, edycja tytułu/instrukcji, usuwanie). 
- **Change ID:** `private-recipe-crud`
- **Powiązanie z PRD:** FR-005, FR-006, Sekcja Access Control (User-Created Recipes), Sukcesy Poboczne (Custom Cookbook Curation)
- **Wymagania:** S-01
- **Równolegle z:** —
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Modyfikacje i usuwanie przepisów mogą prowadzić do błędów w silniku wyszukiwania (desynchronizacja danych). Mitygacja: natychmiastowe unieważnianie (invalidating) i czyszczenie lokalnej pamięci podatnej przepisów po stronie backendu C# przy każdej operacji zapisu (zgodnie z FR-006).
- **Status:** done

### S-03: Wyszukiwanie przepisów po składnikach (publiczne + prywatne)

- **Rezultat:** Silnik wyszukiwania przepisów po składnikach pobiera zarówno globalne przepisy publiczne, jak i prywatne przepisy zalogowanego użytkownika, prezentując je w jednej, spójnej, posortowanej liście. Zapewniona jest ścisła kontrola uprawnień i separacja danych.
- **Change ID:** `unified-matching`
- **Powiązanie z PRD:** FR-003, Guardrail (Izolacja danych)
- **Wymagania:** S-01, S-02
- **Równolegle z:** —
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Potencjalny wyciek danych (użytkownik widzi prywatne przepisy innej osoby w wynikach wyszukiwania). Mitygacja: wprowadzenie rygorystycznych testów jednostkowych (Unit Tests) w C# weryfikujących separację identyfikatorów użytkowników (`UserId`) na poziomie repozytorium danych.
- **Status:** done

### S-04: Retencja danych użytkownika (user-data-retention)

- **Rezultat:** Zalogowany użytkownik może samodzielnie usunąć swoje konto z poziomu interfejsu (wraz ze wszystkimi powiązanymi danymi osobowymi i prywatnymi przepisami), a konta nieaktywne od ponad 2 lat (24 miesięcy) są automatycznie usuwane przez zadanie w tle po stronie backendu API.
- **Change ID:** `user-data-retention`
- **Powiązanie z PRD:** RODO/GDPR Compliance (Art. 5 ust. 1 lit. e, Art. 17)
- **Wymagania:** F-01, F-02
- **Równolegle z:** S-02, S-03, F-03
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Ryzyko utraty danych aktywnych użytkowników w wyniku błędnego wyliczenia czasu nieaktywności. Mitygacja: restrykcyjne testy jednostkowe na logice filtrującej czas nieaktywności.
- **Status:** done

### S-05: Poprawienie spójności i wyrównania przycisków na dashboardzie (ui-button-alignment)

- **Rezultat:** Wszystkie przyciski interfejsu (dodawanie, usuwanie, dopasowanie, rozwijanie) posiadają spójny i jednolity rozmiar, wypełnienie, kolory, zaokrąglenia krawędzi oraz są precyzyjnie wyrównane w pionie i poziomie z towarzyszącym tekstem, poprawiając ogólny UX aplikacji.
- **Change ID:** `ui-button-alignment`
- **Powiązanie z PRD:** UX/UI Polish
- **Wymagania:** S-03
- **Równolegle z:** —
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Zmiany mogą zaburzyć mobilną responsywność. Mitygacja: Testowanie na ekranach o szerokości od 360px w trybie Chrome DevTools.
- **Status:** done

### F-04: Refaktoryzacja API do kontrolerów i odchudzenie punktów końcowych (backend-controllers-refactor)

- **Rezultat:** Przeniesienie całej logiki biznesowej z minimal API (RecipeEndpoints.cs) bezpośrednio do odpowiednich metod serwisu (RecipeService). Zastąpienie minimal API klasycznymi kontrolerami ASP.NET Core (`ControllerBase`) z jawnie zdefiniowanymi trasami, atrybutami autoryzacji i czytelnym wstrzykiwaniem zależności.
- **Change ID:** `backend-controllers-refactor`
- **Powiązanie z PRD:** Refaktoryzacja architektury i czysty kod (Clean Code)
- **Wymagania:** S-03
- **Równolegle z:** —
- **Blokady:** —
- **Niewiadome:** —
- **Ryzyko:** Ryzyko regresji działania istniejących tras API. Mitygacja: Pełne uruchomienie testów integracyjnych w pliku `.http` oraz testów jednostkowych.
- **Status:** done

## Przekazanie do backlogu

| ID mapy | Change ID | Sugerowany tytuł zadania w Jira / Linear | Gotowe do `/10x-plan` | Uwagi |
|---|---|---|---|---|
| F-01 | auth-foundation | Setup autoryzacji JWT i profilu użytkownika (.NET + Angular) | tak | Uruchom jako pierwsze zadanie programistyczne |
| F-02 | database-foundation | Konfiguracja EF Core i migracje bazy danych na Azure SQL | nie | Wymaga wcześniejszego wdrożenia F-01 |
| F-03 | deployment-ci-cd | Konfiguracja automatyzacji GitHub Actions CI/CD dla Azure | tak | Tor operacyjny, można wdrażać równolegle |
| S-01 | public-recipe-matching | Implementacja wyszukiwania przepisów po składnikach (baza publiczna) | tak | Pierwszy wycinek pionowy (Gwiazda Przewodnia) |
| S-02 | private-recipe-crud | Tworzenie, edycja i usuwanie przepisów prywatnych (CRUD) | nie | Wymaga S-01 |
| S-03 | unified-matching | Integracja i unifikacja dopasowania przepisów publicznych i prywatnych | nie | Wymaga S-01 i S-02 |
| S-04 | user-data-retention | Retencja danych użytkownika i usuwanie nieaktywnych kont | nie | Wymaga F-01 i F-02 |
| S-05 | ui-button-alignment | Ujednolicenie i wyrównanie przycisków na stronie głównej | tak | Samodzielne zadanie UI |
| F-04 | backend-controllers-refactor | Refaktoryzacja API z minimal endpoints do klasycznych kontrolerów ASP.NET Core | tak | Zadanie architektoniczne, redukuje dług techniczny |

## Otwarte pytania

1. **Brak konfiguracji sekretów repozytorium GitHub** — Kto skonfiguruje dane uwierzytelniające w GitHub Secrets do wdrożenia na Azure? — Właściciel: Użytkownik. Blok: nie (blokuje tylko tor CI/CD, nie wstrzymuje prac programistycznych).

## Zaparkowane

- **Polubienia i ulubione przepisy** — Powód: Wyłączone z MVP zgodnie z PRD §Non-Goals dla zachowania niskiej złożoności.
- **Wyszukiwanie po nazwie, kategoriach i tagach** — Powód: Wyłączone z MVP zgodnie z PRD §Non-Goals. Aplikacja wyszukuje wyłącznie po składnikach.
- **Współdzielenie prywatnych przepisów** — Powód: Wyłączone z MVP zgodnie z PRD §Non-Goals (wszystkie własne przepisy są w pełni prywatne).
- **Integracje smart z AGD i zewnętrznymi API przepisów** — Powód: Wyłączone z MVP zgodnie z PRD §Non-Goals.
- **Aplikacje mobilne iOS/Android** — Powód: Wyłączone z MVP zgodnie z PRD §Non-Goals (MVP jest wyłącznie aplikacją webową).

## Gotowe

- **F-01: (fundament) Autoryzacja JWT i schemat użytkownika** — Archived 2026-05-25 → `context/archive/2026-05-25-auth-foundation/`. Lesson: —.
- **S-01: Wyszukiwanie przepisów po składnikach (publiczne)** — Archived 2026-05-26 → `context/archive/2026-05-26-public-recipe-matching/`. Lesson: —.
- **S-02: Tworzenie i edycja własnych prywatnych przepisów (CRUD)** — Archived 2026-05-28 → `context/archive/2026-05-28-private-recipe-crud/`. Lesson: —.
- **S-03: Wyszukiwanie przepisów po składnikach (publiczne + prywatne)** — Archived 2026-05-28 → `context/archive/2026-05-28-unified-matching/`. Lesson: —.
- **S-04: Retencja danych użytkownika (samodzielne usuwanie i czyszczenie nieaktywnych kont)** — Archived 2026-05-29 → `context/archive/2026-05-28-user-data-retention/`. Lesson: —.
- **S-05: Poprawienie spójności i wyrównania przycisków na dashboardzie (ui-button-alignment)** — Archived 2026-05-29 → `context/archive/2026-05-29-ui-button-alignment/`. Lesson: —.
- **F-04: (fundament) Refaktoryzacja API do klasycznych kontrolerów i odchudzenie punktów końcowych** — Archived 2026-05-29 → `context/archive/2026-05-29-backend-controllers-refactor/`. Lesson: —.
- **F-03: (fundament) Automatyzacja wdrożenia GitHub Actions na Azure** — Archived 2026-05-29 → `context/archive/2026-05-29-deployment-ci-cd/`. Lesson: —.
