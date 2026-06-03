using System.Text;
using System.Text.Json;

namespace backend.Modules.Ai.Application;

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
        builder.AppendLine("operation должен быть строго одним из поддерживаемых значений. Предпочитай canonical English snake_case, русские aliases тоже принимаются backend:");
        builder.AppendLine("add_journal_entry, update_memory, update_scene, create_quest, update_quest, create_quest_step, complete_quest_step");
        builder.AppendLine("create_location, update_location, create_npc, update_npc, create_world_object, update_world_object, add_item, request_roll");
        builder.AppendLine("Additional recognized operations for play-flow: move_party_to_location, set_current_location, open_location_exit, close_location_exit, lock_location_exit, unlock_location_exit, start_combat, end_combat, add_combat_participant, remove_combat_participant.");
        builder.AppendLine("Do not propose dangerous operations unless the current scene clearly requires explicit player confirmation.");
        builder.AppendLine("������� aliases: ��������_�������, ��������_��, ��������_������, ��������_���������, �������_���������, ��������_�����, ��������_������_�������, �����������_�������, ���������_������, ��������_������, ��������_�����.");
        builder.AppendLine("Используй запросить_бросок, когда действие игрока требует проверки и результат нельзя честно определить без кубика. После применения change backend создаст mechanic request для frontend.");
        builder.AppendLine("Используй обновить_память только для важных фактов, текущей сцены, NPC, локаций, открытых линий и секретов мастера. После применения change backend обновит память кампании. Не обновляй память из-за мелких событий.");
        builder.AppendLine("������� ������� ����� ���������� ������ � payload ��������_������; �� ��������� �� � master_answer.");
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
        builder.AppendLine("Пример запроса броска:");
        builder.AppendLine("""
{
  "operation": "запросить_бросок",
  "payload": {
    "тип": "ability_check",
    "персонажId": null,
    "характеристика": "ловкость",
    "сложность": 14,
    "причина": "Перепрыгнуть через провал"
  }
}
""");
        builder.AppendLine("Пример обновления памяти:");
        builder.AppendLine("""
{
  "operation": "��������_������",
  "payload": {
    "���������������": "����� ����� ����� �������� ������ � ������� �������.",
    "������������": { "�����": "������ �������", "����������": "���������" },
    "�������������������": ["� ������� ������ ���� ������."],
    "���������������������": ["��������, ��� ������� ���� � �������."],
    "����������������������": []
  }
}
""");
        builder.AppendLine();
        builder.AppendLine("��������� ���� JSON:");
        builder.AppendLine(context.GetRawText());
        builder.AppendLine();
        builder.AppendLine("��������� ������:");
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
        builder.AppendLine("operation разрешён только из списка canonical English snake_case или русских aliases: add_journal_entry, update_memory, update_scene, create_quest, update_quest, create_quest_step, complete_quest_step, create_location, update_location, create_npc, update_npc, create_world_object, update_world_object, add_item, request_roll, добавить_предмет, изменить_хп, изменить_ресурс, добавить_состояние, удалить_состояние, обновить_квест, добавить_запись_журнала, переместить_предмет, запросить_бросок, обновить_память, обновить_сцену.");
        builder.AppendLine("�����:");
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
