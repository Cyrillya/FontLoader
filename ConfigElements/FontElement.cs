using Terraria.UI;

namespace FontLoader.ConfigElements;

public class FontElement : UIElement
{
    public string Name { get; set; }
    public string Path { get; set; }
    
    public FontElement(string name, string path) {
        Name = name;
        Path = path;
    }
}