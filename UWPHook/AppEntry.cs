using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace UWPHook;

public class AppEntry : INotifyPropertyChanged
{
    private bool _isSelected;

    /// <summary>
    /// Gets or sets if the application is selected
    /// </summary>
    public bool Selected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the name of the application
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the executable of the application
    /// </summary>
    public string? Executable { get; set; }

    /// <summary>
    /// Gets or sets the aumid of the application
    /// </summary>
    public string? Aumid { get; set; }

    /// <summary>
    /// Gets or sets the icon for the app
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Sets the path where icons for the app is
    /// </summary>
    public string? IconPath { get; set; }

    public string widestSquareIcon()
    {
        var result = string.Empty;
        var size = new Size(0, 0);
        var images = new List<string>();

        if (string.IsNullOrEmpty(IconPath))
        {
            return string.Empty;
        }

        try
        {
            // Get every png, jpg or jpeg in this directory; Steam only allows .png but jpgs work too.
            images.AddRange(Directory.GetFiles(IconPath, "*.png"));
            images.AddRange(Directory.GetFiles(IconPath, "*.jpg"));
            images.AddRange(Directory.GetFiles(IconPath, "*.jpeg"));
        }
        catch (DirectoryNotFoundException)
        {
            // Issue #56
            return string.Empty;
        }

        foreach (var image in images)
        {
            Image? icon = null;

            // Try to load the image; if it's invalid, skip it.
            try
            {
                icon = Image.FromFile(image);
            }
            catch (System.Exception)
            {
            }

            if (icon != null)
            {
                // UWP apps usually store live tile images in the same directory.
                // Pick the largest square image for use as the Steam icon.
                if (icon.Width == icon.Height && icon.Size.Height > size.Height)
                {
                    size = icon.Size;
                    result = image;
                }
            }
        }

        return result;
    }

    public string isKnownApp()
    {
        if (Aumid is not null && AppManager.IsKnownApp(Aumid, out string? name))
        {
            return name!;
        }

        return "Name not found, double click here to edit";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => $"{Name} ({Aumid})";
}
