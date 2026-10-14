using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HexAI.Desktop.ViewModels;

public class ChatMessage : INotifyPropertyChanged
{
    private string _text = "";
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged();
        }
    }

    public bool IsUser { get; set; }
    
    public string Author => IsUser ? "Siz" : "HexAI";
    public string Icon => IsUser ? "👤" : "✧";
    public string AvatarBackground => IsUser ? "#3B82F6" : "#10B981";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
