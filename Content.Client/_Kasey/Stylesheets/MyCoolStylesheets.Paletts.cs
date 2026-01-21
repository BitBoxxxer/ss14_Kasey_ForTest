using Content.Client.Stylesheets.Palette;
using Robust.Client.Graphics;

namespace Content.Client.Stylesheets.Stylesheets;

public sealed partial class MyCoolStylesheet
{
    // Используем фабричный метод FromHexBase для простоты
    public override ColorPalette PrimaryPalette => ColorPalette.FromHexBase(
        hex: "#1a1a2e",
        lightnessShift: 0.06f,
        chromaShift: 0.00f,
        element: Color.FromHex("#1a1a2e"),
        background: Color.FromHex("#16213e"),
        text: Color.FromHex("#e0e0e0")
    );

    public override ColorPalette SecondaryPalette => ColorPalette.FromHexBase(
        hex: "#2a2a3e",
        lightnessShift: 0.06f,
        element: Color.FromHex("#2a2a3e"),
        background: Color.FromHex("#1a1a2e")
    );

    public override ColorPalette PositivePalette => ColorPalette.FromHexBase(
        hex: "#3e6c45",
        lightnessShift: 0.06f,
        element: Color.FromHex("#3e6c45"),
        background: Color.FromHex("#2a4a2f")
    );

    public override ColorPalette NegativePalette => ColorPalette.FromHexBase(
        hex: "#9b2236",
        lightnessShift: 0.06f,
        element: Color.FromHex("#9b2236"),
        background: Color.FromHex("#7a1a2a")
    );

    public override ColorPalette HighlightPalette => ColorPalette.FromHexBase(
        hex: "#a88b5e",
        lightnessShift: 0.06f,
        element: Color.FromHex("#a88b5e"),
        background: Color.FromHex("#887b4e")
    );
}
