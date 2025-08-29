# Contributing to Axomate

Thanks for your interest in improving Axomate! This document explains how to set up your environment, propose changes, and submit pull requests.

## 📋 Prerequisites
- Windows 10/11 x64
- Visual Studio 2022 (17.10+) with **.NET desktop development** workload
- .NET SDK 8.x
- SQLite (via `Microsoft.Data.Sqlite`, no separate install required)

## 🧰 Getting Started
1. **Fork** the repo and **clone** your fork.
2. Create a branch:
   ```bash
   git checkout -b feature/<short-topic>
   # e.g., feature/mileage-lock or fix/xaml-validation
   ```
3. Build and run:
   ```bash
   dotnet build Axomate.sln
   cd Axomate.UI
   dotnet run
   ```

## 🧭 Project Layout
- `Axomate.Domain` – domain models & validation
- `Axomate.Infrastructure` – EF Core DbContext, Migrations, Repositories, Seeders
- `Axomate.ApplicationLayer` – services & interfaces (all **async**)
- `Axomate.UI` – WPF app (MVVM, CommunityToolkit)
- `Axomate.Tests` – unit tests

## 🧑‍💻 Code Style & Quality
- C# 12, nullable enabled. Run:
  ```bash
  dotnet format
  ```
- Prefer async/await end-to-end. Repository & service methods return `Task<T>`.
- Validation via **DataAnnotations** in Domain; UI uses `val:DataAnnotationsRule`.
- Keep namespaces consistent (e.g., `Axomate.Domain.Models` for all domain entities).

## 🔀 Branching & Commits
- Use **Conventional Commits**:
  - `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`, `build:`
- Keep PRs focused and small when possible.

## 🧪 Testing
- Add/maintain tests where feasible.
- Run all tests before pushing:
  ```bash
  dotnet test Axomate.sln
  ```

## 🗄️ Migrations & Data
Migrations live in **Axomate.Infrastructure**. When changing Domain/Infrastructure models:
```powershell
# From Axomate.Infrastructure directory
Add-Migration <Name> -StartupProject ..\Axomate.UI -Project Axomate.Infrastructure
Update-Database -StartupProject ..\Axomate.UI -Project Axomate.Infrastructure
```
Guidelines:
- Avoid empty migrations; delete and re-scaffold if necessary.
- Ensure the unique index on Invoice `(CustomerId, VehicleId, ServiceDate)` exists or is updated when related fields change.
- Seeders (`CompanySeeder`, `ServiceItemSeeder`) should remain idempotent.

## 🖨️ PDF & UI Notes
- `InvoicePdfService` should render from **persisted** entities (mileage and timestamps must match DB).
- Replace `local:DataAnnotationsRuleAdapter` with `val:DataAnnotationsRule`.
- Use `SaveAndPrintInvoiceCommand`; avoid duplicate/unwired commands.

## ✅ Pull Request Checklist
- [ ] Builds succeed (Debug & Release)
- [ ] Tests pass (`dotnet test`)
- [ ] EF migrations added/updated if applicable
- [ ] README and docs updated if behavior/commands changed
- [ ] Screenshots/GIFs for visible UI changes
- [ ] No secrets or personal data committed

## 📜 License
By contributing, you agree that your contributions will be licensed under the **MIT License** (see `LICENSE`).

## 🆘 Help
Open a GitHub issue with a minimal repro, logs, and screenshots where possible. Thanks for contributing!
