# TL5 Risk-Based Test Strategy for a RealWorld Fullstack Application

Dieses Repository enthält das technische Artefakt zur Transferleistung TL5. Ziel war die Entwicklung und Evaluation einer risikobasierten Teststrategie für eine kleine Fullstack-Webanwendung. Dafür wurden Backend-, API-, Frontend- und End-to-End-Tests umgesetzt und ausgewertet.

## Repository-Struktur

```text
backend/   ASP.NET Core Backend der RealWorld-Anwendung
frontend/  Angular Frontend der RealWorld-Anwendung
docs/      Ergänzende Nachweise, Testberichte und Risikomapping
```

## Untersuchungsgegenstand

Als reproduzierbares Untersuchungsobjekt wurde eine RealWorld-Fullstack-Anwendung verwendet. Die Anwendung besteht aus einem ASP.NET-Core-Backend und einem Angular-Frontend.

## Eigenleistung

Im Rahmen der Arbeit wurden insbesondere folgende Artefakte ergänzt bzw. angepasst:

- risikobasierte Zuordnung von Qualitätsrisiken zu Testebenen
- Backend-Integrationstests
- API- und Validierungstests
- Frontend-Unit-/Service-Tests mit Vitest
- End-to-End-Tests mit Playwright
- ergänzende Testdokumentation und Risikomapping

## Ergänzende Nachweise

Die folgenden Dokumente ergänzen den Anhang der Arbeit:

- [Testklassen und Risikomapping](docs/mapping-testfunktionen-qualitaetsrisiken.pdf)
- [Backend-Testbericht](docs/backend-test-report.pdf)
- [Playwright-E2E-Testbericht](docs/playwright-test-report.pdf)

## Ausführung Frontend

```bash
cd frontend
bun install
bun run test
bun run test:e2e
```

## Ausführung Backend

```bash
cd backend
dotnet restore
dotnet test
```

## Hinweis

Dieses Repository dient als technischer Nachweis für die Transferleistung. Die Dokumente im Ordner `docs/` enthalten ergänzende Testübersichten und Auswertungen, die aufgrund ihres Umfangs nicht vollständig in den schriftlichen Anhang aufgenommen wurden.
