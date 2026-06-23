# Самоучитель по C#

WPF-приложение для интерактивного курса по C#: теория, проверочные вопросы, практические задания и админ-режим для редактирования учебного контента.

## Поддерживаемые платформы

Проект собирается в режиме multi-target:

- `net40` - версия для аудиторий, где доступен только .NET Framework 4.0.
- `net8.0-windows` - современная версия для дальнейшей разработки и новых релизов.

Основной проект: `kursach.csproj`.
Решение: `csharp-self-instruction.sln`.

## Требования

- Windows.
- .NET SDK 8.x.
- Visual Studio или Build Tools с поддержкой WPF.
- Для сборки `net40` нужен .NET Framework 4.0 targeting pack.

## Сборка и проверка

```powershell
dotnet restore csharp-self-instruction.sln
dotnet build kursach.csproj -f net40
dotnet build kursach.csproj -f net8.0-windows
dotnet test csharp-self-instruction.sln
```

Готовые exe-файлы появляются в:

- `bin\Debug\net40\kursach.exe`
- `bin\Debug\net8.0-windows\kursach.exe`

Для релизной сборки добавьте `-c Release`.

## Публикация релиза

```powershell
.\build\publish.ps1
```

Скрипт собирает релизы в `artifacts\releases`:

- `selfinstruction-net40` - версия для .NET Framework 4.0. Требует установленный .NET Framework 4.0 на компьютере пользователя.
- `selfinstruction-net8-win-x64` - self-contained версия для Windows x64 с включенным .NET runtime.

Структура каждого релиза:

- `app\` - исполняемые файлы и библиотеки приложения.
- `content\` - `content.v2.json` и RTF-материалы курса.
- `logs\` - папка для логов приложения.

## Структура

- `kursach.csproj` - WPF-приложение.
- `csharp-self-instruction.sln` - основное решение.
- `src/content.v2.json` - структура курса, вопросы и практические задания.
- `src/new theory/диплом 4 курс Анна/` - RTF-материалы, на которые ссылается JSON.
- `build/publish.ps1` - сборка готовых релизных пакетов.
- `Services/` - загрузка контента, навигация, валидация, запуск практических заданий.
- `ViewModels/` - логика UI.
- `tests/Kursach.Tests/` - автотесты.

## Работа с ветками

- Именные ветки разработчиков используются для личной разработки.
- Фичи создаются от ветки разработчика или от актуальной целевой ветки.
- Стабильные изменения сливаются в `dev`.
- Глобально стабильные версии попадают в `master`.
- Multi-target версия ведется отдельной feature-веткой до стабилизации.

## Правила для контента

- Файлы курса хранить в UTF-8, если это JSON или исходный код.
- RTF-материалы должны лежать только в используемой директории курса.
- Не коммитить временные отчеты, архивы, резервные копии, локальные настройки IDE и одноразовые скрипты.
