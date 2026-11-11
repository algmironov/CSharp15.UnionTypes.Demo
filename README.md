# C# 15 Union Types Demo

Учебное консольное приложение на .NET 11 / C# 15: один `description` из Open Library бывает строкой, объектом или отсутствует. Сравниваем nullable-модель с нативным `union`, отделяя JSON от доменных вариантов. Сторонних union-библиотек нет.

## Запуск

Нужен **релизный .NET SDK 11.0.100** (или совместимый patch). Версия закреплена в `global.json`, язык — `15.0`, предупреждения считаются ошибками. Первое восстановление тестовых зависимостей требует NuGet или заполненного кеша.

```sh
dotnet restore CSharp15.UnionTypes.Demo.slnx
dotnet build CSharp15.UnionTypes.Demo.slnx -c Release --no-restore
dotnet test CSharp15.UnionTypes.Demo.slnx -c Release --no-build
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- legacy
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- union
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- compare
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- examples
```

Стандартный запуск использует четыре небольших **синтетических** fixture из `samples/synthetic/`, не обращается к сети и не зависит от текущего каталога. Оригинальные ответы лежат отдельно в `samples/responses/` и проверяются тестами.

В PowerShell весь набор проверок запускает `./scripts/Verify.ps1`; опция `-Live` дополнительно запускает три режима с реальным HTTP. Для локального SDK можно передать `-Dotnet ./.tools/dotnet/dotnet.exe` (либо абсолютный путь к `dotnet.exe`).

Ожидаемый вывод `compare`:

```text
Synthetic string [String]
legacy: text: A sample description.
union: text: A sample description.
Synthetic object [Object]
legacy: object: A sample description.
union: object: A sample description.
Synthetic missing [Undefined]
legacy: none
union: none
Synthetic null [Null]
legacy: none
union: none
```

Реальные запросы включаются явно:

```sh
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- compare --live OL45804W
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- union --live OL82563W
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- legacy --live
```

Без ID используется `OL45883W`. На дату получения это **JSON-запись перенаправления**, не work с отсутствующим описанием. Приложение выводит `Redirect: /works/OL45804W` и предлагает повторить запрос с целевым ID; автоматически JSON redirect не проходит. Код возврата: `0` — успех (включая обнаруженный redirect), `1` — ошибка данных/запроса/чтения, `2` — неверные аргументы. Нет повторных запросов, timeout — 20 секунд.

## Сравнение

| Legacy | Native union |
|---|---|
| `string? Text` и `LegacyTextObject? Object` | `union Description(PlainText, TextObject, NoDescription)` |
| Можно одновременно заполнить оба свойства | Одно содержимое из объявленных case types |
| Инвариант проверяется вручную при выводе | `switch` перечисляет варианты без `_` |
| Новый nullable-член не требует обновить обработчик | Новый case обнаруживается проверкой полноты `switch` |

Оба подхода строго проверяют внешний JSON. Union не решает десериализацию автоматически и не проверяет смысл строк внутри case types. `default(Description)` содержит null: это ошибка инициализации, отличная от `NoDescription`.

## Данные и материалы

- [Происхождение ответов](samples/responses/README.md): реальные строка, объект и JSON redirects; missing/null для work проверяются синтетически.
- [Архитектура](docs/architecture.md): граница транспорта и домена, ошибки и round-trip.
- [Заметки для статьи](docs/article-notes.md): фрагменты кода, ограничения и неполный switch.
- [Результаты проверок](docs/test-results.md): фактически выполненные команды и ограничения.
- [English overview](README.en.md).

Полноценная статья не включена. Репозиторий не назначает лицензию от имени владельца: перед публичным распространением кода нужно выбрать её отдельно.
