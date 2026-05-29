using System.Text;
using System.Text.Json;

namespace backend.Services.Rpg;

public sealed class PromptBuilder : IPromptBuilder
{
    private const int RepairResponseExcerptMaxLength = 4000;

    public string BuildTurnPrompt(JsonElement context, string playerMessage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Ты русскоязычный мастер настольной RPG-игры.");
        builder.AppendLine("Всегда отвечай только на русском языке. Не используй китайский или английский язык, кроме имён собственных и уже существующих названий из состояния игры.");
        builder.AppendLine("Верни только один валидный JSON object. Не добавляй markdown, пояснения, текст до или после JSON, блоки ```json или ```.");
        builder.AppendLine("Поле master_answer обязательно и всегда содержит ответ мастера на русском языке.");
        builder.AppendLine("Поле changes обязательно и всегда является массивом. Если изменений нет, верни \"changes\": [].");
        builder.AppendLine("Не меняй состояние игры напрямую. Только предлагай изменения в массиве changes.");
        builder.AppendLine("Не выдумывай прямые изменения характеристик, ресурсов или инвентаря без явной необходимости. Если нужно только зафиксировать событие, используй добавить_запись_журнала.");
        builder.AppendLine("operation должен быть строго одним из этих значений:");
        builder.AppendLine("добавить_предмет");
        builder.AppendLine("изменить_хп");
        builder.AppendLine("изменить_ресурс");
        builder.AppendLine("добавить_состояние");
        builder.AppendLine("удалить_состояние");
        builder.AppendLine("обновить_квест");
        builder.AppendLine("добавить_запись_журнала");
        builder.AppendLine("переместить_предмет");
        builder.AppendLine("Формат ответа строго такой:");
        builder.AppendLine("""
{
  "master_answer": "текст ответа мастера на русском",
  "changes": [
    {
      "operation": "добавить_запись_журнала",
      "payload": {
        "тип": "master",
        "текст": "краткая запись на русском",
        "важное": false
      }
    }
  ]
}
""");
        builder.AppendLine();
        builder.AppendLine("СОСТОЯНИЕ ИГРЫ JSON:");
        builder.AppendLine(context.GetRawText());
        builder.AppendLine();
        builder.AppendLine("СООБЩЕНИЕ ИГРОКА:");
        builder.AppendLine(playerMessage);
        return builder.ToString();
    }

    public string BuildRepairPrompt(string invalidResponse, string validationError)
    {
        var responseExcerpt = invalidResponse.Length <= RepairResponseExcerptMaxLength
            ? invalidResponse
            : invalidResponse[..RepairResponseExcerptMaxLength];

        var builder = new StringBuilder();
        builder.AppendLine("Ты вернул невалидный JSON для русскоязычной RPG-игры.");
        builder.AppendLine("Ошибка проверки: " + validationError);
        builder.AppendLine("Верни только исправленный валидный JSON object. Без markdown, без ```json, без пояснений.");
        builder.AppendLine("master_answer должен быть непустой строкой на русском языке.");
        builder.AppendLine("changes обязателен и должен быть массивом. Если изменений нет, верни \"changes\": [].");
        builder.AppendLine("operation разрешён только из списка: добавить_предмет, изменить_хп, изменить_ресурс, добавить_состояние, удалить_состояние, обновить_квест, добавить_запись_журнала, переместить_предмет.");
        builder.AppendLine("Схема:");
        builder.AppendLine("""
{
  "master_answer": "текст ответа мастера на русском",
  "changes": [
    {
      "operation": "добавить_запись_журнала",
      "payload": {
        "тип": "master",
        "текст": "краткая запись на русском",
        "важное": false
      }
    }
  ]
}
""");
        builder.AppendLine("Прошлый ответ:");
        builder.AppendLine(responseExcerpt);
        return builder.ToString();
    }
}
