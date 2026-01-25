using Content.Client.Stylesheets.Palette;

namespace Content.Client.Stylesheets.Stylesheets;

public sealed partial class OceanStarStylesheet
{
    // Используем фабричный метод FromHexBase для простоты
    public override ColorPalette PrimaryPalette => ColorPalette.FromHexBase(
        hex: "#261218",
        lightnessShift: 0.06f,
        chromaShift: 0.00f,
        element: Color.FromHex("#261218"),
        background: Color.FromHex("#18090d"),
        text: Color.FromHex("#FB6734")
    );

    public override ColorPalette SecondaryPalette => ColorPalette.FromHexBase(
        hex: "#6B1A34",
        lightnessShift: 0.06f,
        element: Color.FromHex("#6B1A34"),
        background: Color.FromHex("#440a2c"),
        text: Color.FromHex("#CE3737")
    );

    public override ColorPalette PositivePalette => ColorPalette.FromHexBase(
        hex: "#FB6734",
        lightnessShift: 0.06f,
        element: Color.FromHex("#FB6734"),
        background: Color.FromHex("#9c2b17"),
        text: Color.FromHex("#261218")
    );

    public override ColorPalette NegativePalette => ColorPalette.FromHexBase(
        hex: "#CE3737",
        lightnessShift: 0.06f,
        element: Color.FromHex("#CE3737"),
        background: Color.FromHex("#8b1111"),
        text: Color.FromHex("#261218")
    );

    public override ColorPalette HighlightPalette => ColorPalette.FromHexBase(
        hex: "#1B3854",
        lightnessShift: 0.06f,
        element: Color.FromHex("#1B3854"),
        background: Color.FromHex("#1a2233"),
        text: Color.FromHex("#FB6734")
    );
}
