using System.Collections.ObjectModel;

namespace UWPHook;

public class AppEntryModel
{
    public ObservableCollection<AppEntry> Entries { get; set; } = new();
}
