---
project: "10xCookBook"
version: 1
status: draft
created: 2026-05-21
context_type: greenfield
product_type: web-app
target_scale:
  users: small
  qps: low
  data_volume: small
timeline_budget:
  mvp_weeks: 3
  hard_deadline: null
  after_hours_only: true
---

# Product Requirement Document: 10xCookBook

This document establishes the product requirements, user stories, success criteria, and domain rules for the **10xCookBook** greenfield web application.

## Vision & Problem Statement

Home cooks face daily decision fatigue and cognitive load when standing in front of an open fridge trying to choose what to make for dinner. Often, they have fresh ingredients that they want to utilize to prevent food waste, but existing recipe tools only support searching by recipe name, cuisine, or categories, leading to unnecessary grocery expenses and throwing away unused food.

**10xCookBook** takes an ingredient-first approach to recipe discovery. By matching available ingredients directly to recipes and prioritizing those that use the highest percentage of what's already on hand, it eliminates search friction and directly solves the decision paralysis and food waste problem.

## User & Persona

### Primary Persona
- **Name/Role**: Conscious Home Cook
- **Context**: A person who cooks regularly at home, manages their own kitchen inventory, and cares about reducing food waste and grocery expenses.
- **The Moment**: Standing in front of the open fridge, looking at ingredients that need to be used, and asking: *"What can I make with these right now?"*

## Success Criteria

### Primary
- **Ingredient-First Match Rate**: Users can input ingredients they have on hand and receive a list of recipes ranked by the percentage of their inputted ingredients that are utilized.
- **Algorithm Speed**: Recipe matching runs in under 500ms p95 for search lists of up to 15 ingredients against a base of 1,000+ recipes.

### Secondary
- **Custom Cookbook Curation**: Users can successfully register, log in, and manually add, edit, or delete custom recipes that are immediately integrated into their matching results.

### Guardrails
- **Data Isolation**: User-created recipes are strictly private; no user can view or match against another user's private recipes.
- **Responsive Layout**: The entire web app operates seamlessly on both standard mobile and desktop web browsers without UI degradation or horizontal scroll issues (handling screens down to 360px wide).

## User Stories

### US-01: Ingredient Matching Search

- **Given** a logged-in user who has a list of ingredients in their fridge
- **When** they input the list of ingredients and click "Match Recipes"
- **Then** they see a list of recipes (both public and their own private ones) ranked by the percentage of matched ingredients, showing the percentage match and highlighting which ingredients they have vs. which ones are missing.

#### Acceptance Criteria
- Recipes must be sorted in descending order of matching percentage.
- The matching percentage must be clearly displayed on each recipe card.
- User can click on any matched recipe to view full cooking instructions and a detailed ingredients checklist.

## Functional Requirements

### Recipe Matching
- **FR-001**: Authenticated User can input a list of ingredients to find matching recipes. Priority: must-have
  > Socrates: Counter-argument considered: "Guest access simplifies trial." Resolution: Changed to Authenticated User only; guest search disabled to prevent API abuse and scraping.
- **FR-002**: Authenticated User can view a ranked list of recipes based on the percentage of matching ingredients. Priority: must-have
  > Socrates: Counter-argument considered: "Complex rankings slow results." Resolution: Kept; use a simple in-memory intersection-count calculation to keep it lightweight.
- **FR-003**: Authenticated User can search recipes matching both public and their own private recipes. Priority: must-have
  > Socrates: Counter-argument considered: "Merging public and private lists in database query increases complexity." Resolution: Kept; fetch both lists and merge them in application memory to keep queries simple.

### Recipe Management
- **FR-004**: Authenticated User can view public recipes in the global database. Priority: must-have
  > Socrates: Counter-argument considered: "Guests could scrape public database." Resolution: Kept; restricted to logged-in users to prevent anonymous scraping.
- **FR-005**: Authenticated User can manually create a private recipe (specifying title, ingredients list, and instructions). Priority: must-have
  > Socrates: Counter-argument considered: "Typos or customized ingredient text ('egg' vs 'eggs') breaks matching." Resolution: Kept; use autocomplete UI with predefined ingredients to standardize names.
- **FR-006**: Authenticated User can view, edit, and delete their own private recipes. Priority: must-have
  > Socrates: Counter-argument considered: "Mutations can desync matching results." Resolution: Kept; invalidate matching cache immediately on recipe updates.

### Account Management
- **FR-007**: Guest can register a new account with email and password. Priority: must-have
  > Socrates: Counter-argument considered: "Spam accounts clutter database." Resolution: Kept; frictionless registration without strict verification for the MVP to prioritize user conversion.
- **FR-008**: Registered User can log in and out of their account. Priority: must-have
  > Socrates: Counter-argument considered: "Session complexity adds security risks." Resolution: Kept; use standard JWT or framework secure cookies (no custom security logic).

## Non-Functional Requirements

- **Performance**: A user receives recipe search and matching results within 500 milliseconds of clicking "Match Recipes" under standard network conditions.
- **Availability**: The application is accessible and functional at least 99.5% of the time monthly.
- **Security**: Registered user passwords are cryptographically hashed at rest using standard secure hashing functions and stored in secure sessions.
- **Usability**: The application meets WCAG 2.1 AA accessibility guidelines for color contrast and text scaling, and is fully responsive on mobile screens down to 360px wide.

## Business Logic

The application ranks recipes by calculating the percentage overlap between the user's selected fridge ingredients and the required ingredients of each recipe, prioritizing recipes that maximize the use of available ingredients to minimize food waste.

This domain calculation takes a user-supplied array of standardized ingredients (e.g. `["eggs", "spinach", "tomatoes"]`) and compares it against the required ingredients lists of all active recipes in the user's scope (their own private recipes plus global public recipes). For each recipe, it calculates the *Match Score* as:

$$\text{Match Score} = \frac{\text{Count}(\text{User Ingredients} \cap \text{Recipe Ingredients})}{\text{Count}(\text{Recipe Ingredients})} \times 100$$

Recipes are sorted in descending order of this score. High-scoring recipes (where the user has almost all required products) appear at the top. The dashboard visually highlights which ingredients the user has (matched) and which ones are missing, so the user knows exactly what they need or what they can cook immediately without buying anything else.

## Access Control

- **Authentication**: Simple Email & Password registration and login.
- **Data Boundaries**:
  - **Global/Shared Recipes**: A public base of recipes available to all authenticated users for viewing and ingredient-matching.
  - **User-Created Recipes**: Authenticated users can manually create recipes. These recipes are private and visible/editable/deletable *only* by the user who created them.
  - **Anonymous/Guest Access**: Guests cannot view public recipes or search. They must register and log in to use the 10xCookBook application.

## Non-Goals

- **Recipe Likes/Favorites**: Users cannot "like" or mark recipes as favorites in the MVP.
- **Alternative Search Methods**: Searching recipes by name, categories, tags, or cooking time is out of scope; search matches purely by ingredients list.
- **Social Sharing**: Users cannot share their private recipes with other users; all custom-created recipes are strictly private to the creator.
- **External Platform Integrations**: No integrations with smart kitchen appliances, online grocery services, or external recipe APIs.
- **Mobile Applications**: The application is web-only for the initial release; no native iOS or Android mobile apps will be developed.

## Open Questions

There are no active unresolved product questions at this stage. All functional scope, access control rules, and MVP boundaries have been successfully clarified during the discovery shaping process.
