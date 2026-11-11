# Результаты проверок TASK-001

Дата: 11 ноября 2026. ОС: Windows x64. Использован релизный **SDK 11.0.100**, runtime **11.0.0**, `LangVersion=15.0`, `TreatWarningsAsErrors=true`.

## Выполнено

| Проверка | Фактический результат |
|---|---|
| `dotnet build CSharp15.UnionTypes.Demo.slnx -c Release --disable-build-servers` | Успех, 0 предупреждений, 0 ошибок |
| `dotnet test CSharp15.UnionTypes.Demo.slnx -c Release --no-build --disable-build-servers` | **50 passed, 0 failed, 0 skipped** |
| `legacy`, `union`, `compare` без сети | Все завершились с кодом 0; четыре синтетические формы обработаны |
| `examples` | Код 0; implicit conversion, int 42, boxed System.Int32 и default/null |
| Все три режима с `--live OL45804W` | Код 0; реальная строка description |
| `union --live OL82563W` | Код 0; реальный объект description |
| `legacy --live` | Код 0; явный JSON redirect `/works/OL45804W` |
| Отдельный неполный switch | Ожидаемый CS8509 с указанием RichText; exit 1 |
| Тот же пример после добавления RichText arm | Успех, 0 предупреждений, 0 ошибок |
| Все JSON, XML проектов/решения, синтаксис Verify.ps1 | Успех |
| SHA-256 шести оригинальных ответов | Совпадают с metadata |
| Whitespace-проверка новых файлов и финальный Git status | Проверены; пользовательских изменений до задачи не было |

Финальный полный прогон: `./scripts/Verify.ps1 -Dotnet <absolute-path>/.tools/dotnet/dotnet.exe -Live`. Локальный журнал `.artifacts/verification.log` содержит итоговые 50 тестов и вывод команд. Каталог игнорируется Git. Более ранний `.artifacts/TestResults/tests.trx` относится к промежуточному успешному прогону **48** тестов, до добавления двух проверок дополнительных ответов; его не следует путать с финальным прогоном.

Тесты покрывают четыре формы description, неизвестные/некорректные формы и конверты, transport round-trip и сохранение неизвестных полей, конфликт nullable-свойств, default union, boxing, шесть оригинальных ответов, HTTP 200/204/404/429/500, JSON redirects и их валидацию, сетевой сбой, timeout, отмену, валидацию work ID, аргументы/вывод CLI и live-ветку с подменённым HTTP handler. Автотесты не обращаются к интернету.

## Ограничения среды и промежуточные неудачи

Системный SDK был 10.0.404; первоначальные build/test не смогли найти 11.0.100. С разрешения пользователя SDK скачан в `.tools/dotnet`, SHA-512 проверен по официальному release metadata. Первые загрузки оборвались; возобновлённая загрузка прошла проверку. Системный SDK не заменялся, глобальный PATH не менялся. Первый запуск SDK сообщил об автоматическом создании development HTTPS-сертификата; команда доверия сертификату не выполнялась.

В sandbox DNS-запросы не проходили, а vstest не смог установить локальное соединение с testhost за 90 секунд. Один запуск build в sandbox также завершился кодом 1 без диагностик компиляции. После разрешённого запуска вне sandbox финальные сборка и тесты прошли. Эти промежуточные запуски не считаются успешными проверками.

Живой OL45883W первоначально выявил неподдержанный JSON redirect; добавлены отдельный case и тесты, после чего команда повторно успешно проверена. Никакие ошибки не подавлены настройками warnings-as-errors.

Не выполнялись проверки на Linux/macOS, performance benchmarks и массовое исследование каталога Open Library. Реальный explicit null и настоящая work-запись без description не найдены; для этих случаев используются синтетические fixtures. Round-trip проверяет семантику поддерживаемых JSON-данных, а не побайтовое совпадение сериализации. Отрицательный пример проверен отдельно, в обычную сборку он не входит.

## Git

Ветка: `feature/union-types-demo`. Исходное состояние: пустой репозиторий, unborn `main`; базовый коммит и текущий HEAD отсутствуют. Все исходники задачи — новые untracked файлы, staged/изменённых tracked/удалённых файлов нет. Существующих пользовательских файлов не было. Ignored: `.tools/`, `.artifacts/`, `bin/`, `obj/`.

Созданы:

- `.gitignore`, `global.json`, `Directory.Build.props`, `CSharp15.UnionTypes.Demo.slnx`.
- `README.md`, `README.en.md`.
- `docs/architecture.md`, `docs/article-notes.md`, `docs/test-results.md`.
- `src/UnionTypes.Demo/`: проект, `Program.cs`, `Description.cs`, `WorkPayload.cs`, `OpenLibraryClient.cs`, `LanguageExamples.cs`.
- `src/UnionTypes.Legacy/`: проект и `LegacyDescription.cs`.
- `tests/UnionTypes.Tests/`: проект, `DescriptionTests.cs`, `HttpAndCommandTests.cs`.
- `examples/incomplete-switch/IncompleteSwitch.cs.txt`, `scripts/Verify.ps1`.
- `samples/synthetic/`: `string.json`, `object.json`, `missing.json`, `null.json`.
- `samples/responses/README.md` и пары `.json` / `.metadata.json` для OL45883W, OL45804W, OL82563W, OL14933414W, OL27513W, OL262758W.

Коммиты и сетевые Git-операции не выполнялись. Upstream отсутствует; актуальность удалённого репозитория не установлена. Перед публичным распространением владелец должен выбрать лицензию.

Предлагаемый коммит (не выполнен):

```text
feat: add C# 15 union types demo with Open Library fixtures and tests
```
