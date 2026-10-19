# Architecture

Frontend: WPF (.NET 9)
Pattern: MVVM
Backend: FastAPI
Database: SQLite
ORM: SQLAlchemy
Testing: PyTest and xUnit
AI: Claude API, OpenAI API, Ollama
Vector Database: ChromaDB
Reporting: PDF Export
Charts: LiveCharts2

## Implementation (Phase 1)

```
WPF Desktop (CyberSecOS.Desktop)        FastAPI backend (backend/app)
  Views (XAML) + converters    HTTP/JSON   routers/   auth, users, courses,
        │ DataTemplates        ───────▶              progress, dashboard
  View models (CyberSecOS.Core)  Bearer    services/  seed, quiz, progress, stats
  ICyberSecApi → HttpClient      JWT       models.py  SQLAlchemy 2.0 → SQLite
```

- **MVVM:** view models, models and the API client live in `CyberSecOS.Core`
  (plain `net9.0`, CommunityToolkit.Mvvm) so they are unit tested with xUnit on
  any OS. `CyberSecOS.Desktop` holds XAML views, theming and DI wiring only.
- **Navigation:** view-model-first. `INavigationService` creates page view
  models from the DI container; `App.xaml` DataTemplates map each to its view.
- **Charts:** view models expose chart-neutral `ChartData`; WPF converters turn
  it into LiveCharts2 series and axes.
- **Auth:** single local account, bcrypt hashes, HS256 JWT kept in memory by the
  desktop client, lockout after repeated failures.
- **Content:** YAML in `backend/app/seed`, loaded idempotently at startup.
- **Later phases:** Claude/OpenAI/Ollama (AI Mentor), ChromaDB and PDF export are
  not used in Phase 1.
