# Заметки для будущей статьи

## Исследование API

Дата: 11 ноября 2026. Сначала sandbox не разрешал DNS имена. После разрешения пользователя выполнены прямые HTTP GET вне sandbox и сохранены оригинальные байты ответов.

Обнаружены: строка (`OL45804W`, `OL27513W`), объект (`OL82563W`) и JSON redirects (`OL45883W`, `OL14933414W`, `OL262758W`). В redirect нет title/description: это не work-записи без описания. Явный null и work без description в выборке не найдены; проверяются синтетически. URL, точное UTC-время и SHA-256 — в `samples/responses/*.metadata.json`. Содержимое ответа не выдаётся за универсальный или неизменный контракт API.

Особенно полезный сюжет для статьи: исходный URL OL45883W возвращает HTTP 200, но тело имеет `/type/redirect`. Проверка только статуса недостаточна. Клиент возвращает `WorkRedirect`, CLI показывает целевой ID и не следует JSON-ссылке автоматически. Это отдельный случай от HTTP redirect, который может обрабатываться HttpClient.

Open Library описывает получение work metadata через суффикс `.json`: [официальная документация](https://openlibrary.org/dev/docs/api/books). Примеры ответов и их происхождение перечислены в [каталоге samples](../samples/responses/README.md).

## Фрагменты для статьи

1. `src/UnionTypes.Legacy/LegacyDescription.cs`: два nullable-свойства и невозможный по смыслу, но компилируемый `new LegacyDescription("one", new("two"))`.
2. `src/UnionTypes.Demo/Description.cs`: нативное объявление и switch без `_`.
3. `src/UnionTypes.Demo/WorkPayload.cs`: транспортная форма без попытки десериализовать union автоматически.
4. `src/UnionTypes.Demo/OpenLibraryClient.cs`: явное множество результатов HTTP-запроса.
5. `src/UnionTypes.Demo/LanguageExamples.cs`: самостоятельные примеры conversion, matching, default/null и boxing.

Точный доменный фрагмент:

```csharp
public union Description(PlainText, TextObject, NoDescription);

public static string Render(Description description) => description switch
{
    PlainText text => $"text: {text.Value}",
    TextObject text => $"object: {text.Value}",
    NoDescription => "none",
    null => throw new InvalidOperationException("Uninitialized Description union.")
};
```

Неявное преобразование: `Description converted = new PlainText("implicit conversion");`. Типы cases не наследуют общий базовый класс. Обработка null не является catch-all: добавление четвёртого case по-прежнему требует новой ветки.

## Диагностика расширения

`examples/incomplete-switch/IncompleteSwitch.cs.txt` содержит отдельный пример с новым `RichText`, который switch не обрабатывает. `.txt` не компилируется в обычном решении. Для воспроизведения скопируйте его в отдельный net11.0-проект с `LangVersion=15.0`, implicit usings и warnings-as-errors. Сначала получите диагностику неполного switch, затем добавьте указанную в комментарии ветку и повторите сборку. Фактически наблюдавшийся результат проверки записывается в `test-results.md`.

## Особенности языка

По [справочнику C# union](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union), объявление генерирует struct с объектным хранением: значимые cases упаковываются. Полнота switch проверяется по типам cases; `default` может содержать null даже при nonnullable cases. Поэтому отсутствие предупреждений не отменяет runtime-проверок границы.

Пример с `NumberOrText` показывает boxed `System.Int32`, но не измеряет производительность или число аллокаций. Union не проверяет внешний JSON, смысл текста или корректность HTTP-ответа. В этом проекте для всего этого есть отдельные явные проверки. Не следует обобщать учебный результат на все API.

SDK закреплён на релизном 11.0.100, C# — на 15.0, preview не используется. [Официальная страница .NET 11](https://dotnet.microsoft.com/en-us/download/dotnet/11.0). Окончательные выводы о работоспособности должны опираться на журнал проверок, а не только на документацию.
