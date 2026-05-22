---
starter_id: dotnet
package_manager: dotnet
project_name: 10x-cookbook-backend
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: standard
  quality_override: false
  self_check_answers: null
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: false
  has_background_jobs: false
---

## Why this stack

A solo developer building the 10xCookBook backend API under a 3-week after-hours timeline with secure user authentication requirements accepted the recommended standard path for .NET. ASP.NET Core (`dotnet`) is the vetted recommended starter for API development in C#, satisfying all four agentic-readiness quality gates. Scaffolding is verified and will compile to a highly typed, convention-based API. The backend will deploy to Azure App Service via GitHub Actions with auto-deploy on merge to main, matching the Azure ecosystem and the frictionless user registration goals outlined in the PRD.
