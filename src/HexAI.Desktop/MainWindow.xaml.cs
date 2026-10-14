using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using HexAI.Assistant;
using HexAI.Assistant.Config;
using HexAI.Assistant.Evaluation;
using HexAI.Desktop.ViewModels;
using HexAI.Desktop.Services;

namespace HexAI.Desktop;

public partial class MainWindow : Window
{
    private AssistantEngine _engine = null!;
    private ChatSessionManager _sessionManager = null!;
    public ObservableCollection<ChatSession> Sessions { get; set; } = new();
    private ChatSession _activeSession = null!;

    // Son evaluation sonucu (tutarlılık raporu için saklanır)
    private EvaluationResult? _lastEvaluationResult;

    public MainWindow()
    {
        InitializeComponent();
        SessionList.ItemsSource = Sessions;
        _sessionManager = new ChatSessionManager();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPastSessions();

        // Motoru arka planda başlat
        await Task.Run(() =>
        {
            _engine = new AssistantEngine();
            _engine.Initialize();
        });

        LoadingIndicator.Visibility = Visibility.Collapsed;
        InputBox.IsEnabled = true;
        SendButton.IsEnabled = true;

        if (_activeSession.Messages.Count == 0)
        {
            AddMessage("Merhaba! 👋 Ben HexAI, senin kişisel masaüstü asistanın. Sana nasıl yardımcı olabilirim?", isUser: false, session: _activeSession);
        }

        // UI'ı güncelle
        UpdateProviderBadge();
        UpdateTokenCounter();
        
        InputBox.Focus();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await ProcessInput();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ProcessInput();
        }
    }

    private async Task ProcessInput()
    {
        string input = InputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        InputBox.Clear();
        InputBox.IsEnabled = false;
        SendButton.IsEnabled = false;

        // O anki aktif session'ı yakala (kullanıcı AI düşünürken başka sohbete geçerse diye)
        ChatSession currentSession = _activeSession;

        // Update title if new session
        if (currentSession.Title == "Yeni Sohbet" || currentSession.Title == "")
        {
            currentSession.Title = input.Length > 25 ? input.Substring(0, 25) + "..." : input;
            _sessionManager.SaveSession(currentSession);
        }

        AddMessage(input, isUser: true, session: currentSession);

        var msg = new ChatMessage { Text = "", IsUser = false };
        currentSession.Messages.Add(msg);
        if (currentSession == _activeSession) ScrollToBottom();

        int delay = _engine.Config.TypewriterDelayMs;

        await foreach (var token in _engine.ProcessMessageStreamAsync(input))
        {
            msg.Text += token + " ";
            if (currentSession == _activeSession) ScrollToBottom();

            if (delay > 0)
            {
                await Task.Delay(delay);
            }
        }
        msg.Text = msg.Text.TrimEnd();
        _sessionManager.SaveSession(currentSession);


        // Token sayacını güncelle
        UpdateTokenCounter();

        InputBox.IsEnabled = true;
        SendButton.IsEnabled = true;
        InputBox.Focus();
    }

    private void VoiceButton_Click(object sender, RoutedEventArgs e)
    {
        // Ses özelliği devre dışı bırakıldı
    }

    private void AddMessage(string text, bool isUser, ChatSession session)
    {
        var msg = new ChatMessage { Text = text, IsUser = isUser };
        session.Messages.Add(msg);
        _sessionManager.SaveSession(session);
        
        if (session == _activeSession)
        {
            ScrollToBottom();
        }
    }

    // TypewriterEffect removed because we are now streaming tokens directly.

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    // --- Backend Integration (Sidebar) ---
    private void LoadPastSessions()
    {
        var saved = _sessionManager.LoadAllSessions();
        foreach (var s in saved) Sessions.Add(s);
        
        if (Sessions.Count > 0)
        {
            SetActiveSession(Sessions[0]);
        }
        else
        {
            CreateNewSession();
        }
    }

    private void CreateNewSession()
    {
        var newSession = new ChatSession();
        Sessions.Insert(0, newSession);
        SetActiveSession(newSession);
        SessionList.SelectedItem = newSession;
    }

    private void SetActiveSession(ChatSession session)
    {
        _activeSession = session;
        
        // Sadece aktif seansın mesajlarını göster (Global liste yok)
        ChatList.ItemsSource = _activeSession.Messages;
        
        if (_engine != null)
        {
            _engine.Memory.Clear();
            // Engine'in hafızasına geçmişi yükle
            foreach (var m in session.Messages)
            {
                if (m.IsUser) _engine.Memory.AddUserMessage(m.Text);
                else _engine.Memory.AddAssistantMessage(m.Text);
            }
        }
        
        ScrollToBottom();
    }

    private void NewChatButton_Click(object sender, RoutedEventArgs e)
    {
        CreateNewSession();
        AddMessage("Merhaba! 👋 Ben HexAI, yeni bir sohbete başladık. Ne sormak istersin?", isUser: false, session: _activeSession);
    }

    private void SessionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SessionList.SelectedItem is ChatSession selected)
        {
            if (selected != _activeSession)
            {
                SetActiveSession(selected);
            }
        }
    }

    private void DeleteSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ChatSession session)
        {
            // Diskten sil
            _sessionManager.DeleteSession(session.Id);
            // Listeden sil
            Sessions.Remove(session);
            
            // Eğer sildiğimiz sohbet aktif sohbet ise başka birine geç
            if (_activeSession == session)
            {
                if (Sessions.Count > 0)
                {
                    SetActiveSession(Sessions[0]);
                }
                else
                {
                    CreateNewSession();
                }
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine != null)
        {
            string status = _engine.GetModelStatus();
            string extra = $"\n\n🌐 Aktif Persona: {_engine.ActivePersonaName}" +
                           $"\n📝 Sistem Prompt: {(_engine.Config.SystemPrompt.Length > 50 ? _engine.Config.SystemPrompt[..50] + "..." : _engine.Config.SystemPrompt)}" +
                           $"\n🪙 Oturum Token: {_engine.Config.SessionTokensUsed}" +
                           $"\n🪙 Toplam Token: {_engine.Config.TotalTokensUsed}" +
                           $"\n📚 Bilgi Tabanı: {_engine.Knowledge.Count} giriş" +
                           $"\n✅ Doğrulama: {(_engine.Config.EnableVerification ? "Açık" : "Kapalı")}";
            MessageBox.Show(status + extra, "HexAI Ayarlar & Durum", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // --- Evaluation & Games Buttons ---

    private void MiniGames_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        var gamesWindow = new MiniGamesWindow(_engine)
        {
            Owner = this
        };
        gamesWindow.ShowDialog();
    }

    private async void EvaluationTest_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        AddMessage("🧪 Eğitim değerlendirme testi başlatılıyor...", isUser: false, session: _activeSession);

        var result = await Task.Run(() => _engine.RunEvaluation());
        _lastEvaluationResult = result;

        string report = result.ToReportString();
        AddMessage(report, isUser: false, session: _activeSession);
    }

    private void ConsistencyReport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastEvaluationResult == null)
        {
            MessageBox.Show(
                "Henüz bir eğitim testi çalıştırılmadı.\n\n" +
                "Önce \"🧪 Eğitim Testi\" butonuna tıklayın.",
                "Tutarlılık Raporu", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string report = _lastEvaluationResult.ToReportString();
        MessageBox.Show(report, $"📊 Tutarlılık Raporu — Not: {_lastEvaluationResult.OverallGrade}", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void Benchmark_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        AddMessage("⚡ Benchmark testi başlatılıyor...", isUser: false, session: _activeSession);

        var result = await Task.Run(() => _engine.RunBenchmark());

        string report = result.ToReportString();
        AddMessage(report, isUser: false, session: _activeSession);
    }

    private void KnowledgeBase_Click(object sender, RoutedEventArgs e)
    {
        if (_engine == null) return;

        string stats = _engine.Knowledge.GetStats();
        MessageBox.Show(stats, "📚 Bilgi Tabanı İstatistikleri", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportChat_Click(object sender, RoutedEventArgs e)
    {
        if (_activeSession == null || _activeSession.Messages.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak sohbet yok.", "Dışa Aktarma", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            string path = _sessionManager.ExportToFile(_activeSession, "md");
            MessageBox.Show($"Sohbet başarıyla dışa aktarıldı:\n{path}", "📤 Dışa Aktarma", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // --- Model Selector ---

    private void ModelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_engine == null) return;
        if (ModelSelector.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var persona = tag switch
            {
                "Standard" => AIPersona.Standard,
                "Creative" => AIPersona.Creative,
                "Coder" => AIPersona.Coder,
                "Roleplay" => AIPersona.Roleplay,
                _ => AIPersona.Standard
            };

            _engine.SetActivePersona(persona);
            UpdateProviderBadge();
        }
    }

    // --- UI Helpers ---

    private void UpdateProviderBadge()
    {
        if (_engine == null) return;
        ProviderBadge.Text = _engine.Config.ActivePersona switch
        {
            AIPersona.Standard => "🤖 Standart",
            AIPersona.Creative => "🎭 Yaratıcı",
            AIPersona.Coder => "💻 Kodlayıcı",
            AIPersona.Roleplay => "🧙‍♂️ Rol Yapma",
            _ => "🤖 Standart"
        };
    }

    private void UpdateTokenCounter()
    {
        if (_engine == null) return;
        TokenCounter.Text = $"🪙 Token: {_engine.Config.SessionTokensUsed:N0}";
    }

    // --- Custom Title Bar Controls ---
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (WindowState == WindowState.Normal) WindowState = WindowState.Maximized;
            else WindowState = WindowState.Normal;
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

