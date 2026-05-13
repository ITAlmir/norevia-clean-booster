using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsCleanBooster.Models;

public class CleanTarget : INotifyPropertyChanged
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    // ✅ OVO TI FALI
    public CleanTargetType Type { get; set; } = CleanTargetType.Folder;

    // ✅ OVO TI FALI (za folder/file targete)
    public string? Path { get; set; }

    private bool _enabledByDefault = true;
    public bool EnabledByDefault
    {
        get => _enabledByDefault;
        set
        {
            if (_enabledByDefault == value) return;
            _enabledByDefault = value;
            OnPropertyChanged();
        }
    }

    public long EstimatedBytes { get; set; }
    public int EstimatedFiles { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}