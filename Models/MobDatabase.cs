using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MMOServer.Models;

public static class MobDatabase
{
    private static readonly Dictionary<int, MobTemplate> _templates = new();

    public static void Init()
    {
        string filePath = "Data/mobs.json";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[БД Мобов] ВНИМАНИЕ! Файл {filePath} не найден.");
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);

            var templatesList = JsonSerializer.Deserialize<List<MobTemplate>>(jsonString);

            if (templatesList != null)
            {
                foreach (var template in templatesList)
                {
                    _templates[template.TemplateId] = template;
                }
                Console.WriteLine($"[БД Мобов] Загружено шаблонов: {_templates.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[БД Мобов] Ошибка загрузки JSON: {ex.Message}");
        }
    }

    public static MobTemplate GetTemplate(int templateId)
    {
        _templates.TryGetValue(templateId, out MobTemplate template);
        return template;
    }
}
