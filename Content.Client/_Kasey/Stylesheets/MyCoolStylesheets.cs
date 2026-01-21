using System.Linq;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Stylesheets;

namespace Content.Client.Stylesheets.Stylesheets;

[Virtual]
public partial class MyCoolStylesheet : CommonStylesheet
{

    // 1. Уникальное имя стиля (будет использоваться как ключ в StylesheetManager)
    public override string StylesheetName => "MyCoolTheme";
    public override NotoFontFamilyStack BaseFont { get; }

    // 2. Определяем корневую папку для текстур нашей темы
    public static readonly ResPath TextureRoot = new("/Textures/Interface/MyCoolTheme");

    // 3. Задаём, где искать текстуры для ресурсов типа TextureResource
    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot] }
    };

    // 4. Конструктор: задаём цвета и правила
    public MyCoolStylesheet(object config, StylesheetManager man) : base(config)
    {
        BaseFont = new NotoFontFamilyStack(ResCache);

        var rules = new[]
        {
            // Базовые правила для шрифтов
            GetRulesForFont(null, BaseFont, new List<(string?, int)>
            {
                (null, 12),
                (StyleClass.FontSmall, 10),
                (StyleClass.FontLarge, 14)
            }),

            // Наши кастомные правила
            [
                // Пример: меняем цвет текста по умолчанию
                Element().Prop(Label.StylePropertyFontColor, Color.FromHex("#e0e0e0")),
            ],

            // Подключаем ВСЕ общие Sheetlets
            GetAllSheetletRules<CommonStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }
}
