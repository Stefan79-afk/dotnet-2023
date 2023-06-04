using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace Mapster.ClientApplication;

public partial class MapTile : Image
{
    public MapTile()
    {
        InitializeComponent();
    }

    public MapTile(byte[] imageData, int size)
    {
        InitializeComponent();

        var stream = new MemoryStream(imageData);
        Source = Bitmap.DecodeToHeight(stream, size);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}