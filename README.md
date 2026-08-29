[![](https://img.shields.io/github/actions/workflow/status/soenneker/Soenneker.Runners.Whisper.CTranslate/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/Soenneker.Runners.Whisper.CTranslate/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/Soenneker.Runners.Whisper.CTranslate/daily-automatic-update.yml?style=for-the-badge&label=Daily%20Update)](https://github.com/soenneker/Soenneker.Runners.Whisper.CTranslate/actions/workflows/daily-automatic-update.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/Soenneker.Runners.Whisper.CTranslate/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/Soenneker.Runners.Whisper.CTranslate/actions/workflows/codeql.yml)

# Soenneker.Runners.Whisper.CTranslate

Defines the build library util contract.

> This is an automation runner, not a package intended for application consumption.

## What the runner does

- `IBuildLibraryUtil.Build(cancellationToken)` — Builds build Library.
- `Constants.FileName` — The file name.
- `Constants.Library` — The library.
- `ConsoleHostedService.StartAsync(cancellationToken)` — Starts the Console Hosted Service and begins its background work.
- `ConsoleHostedService.StopAsync(cancellationToken)` — Stops the Console Hosted Service and waits for its background work to finish.

## What you get

- `IBuildLibraryUtil` — Defines the build library util contract.
- `Constants` — Represents the constants.
- `ConsoleHostedService` — Represents the console hosted service.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IBuildLibraryUtil.Build(cancellationToken)` | Builds build Library. | A task whose result is the text returned by build. |
| `ConsoleHostedService.StartAsync(cancellationToken)` | Starts the Console Hosted Service and begins its background work. | A task that completes after the Console Hosted Service has started. |
| `ConsoleHostedService.StopAsync(cancellationToken)` | Stops the Console Hosted Service and waits for its background work to finish. | A task that completes after the Console Hosted Service has stopped. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
