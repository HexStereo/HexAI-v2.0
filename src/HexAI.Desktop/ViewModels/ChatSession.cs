using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HexAI.Desktop.ViewModels;

public class ChatSession : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    private string _title = "Yeni Sohbet";
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayTitle));
        }
    }

    public string DisplayTitle => $"💬  {Title}";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ObservableCollection<ChatMessage> Messages { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
