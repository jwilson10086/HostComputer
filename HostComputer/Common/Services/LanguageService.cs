using HostComputer.Common.Services.StartupModules;
using System.ComponentModel;

public class LanguageService : INotifyPropertyChanged
{
    private Dictionary<string, Dictionary<string, string>> _resources = new();
    private string _currentLang = AppConfiguration.Current.UI.Language;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;

    public List<string> AvailableLanguages { get; private set; } = new();

    public LanguageService() { }

    /// <summary>
    /// 启动模块将 Excel 数据传进来
    /// </summary>
    public void Initialize(Dictionary<string, Dictionary<string, string>> languageData)
    {
        _resources = new();

        foreach (var lang in languageData.Keys)
        {
            foreach (var kv in languageData[lang])
            {
                string key = kv.Key;
                string value = kv.Value;

                if (!_resources.ContainsKey(key))
                    _resources[key] = new Dictionary<string, string>();

                _resources[key][lang] = value;
            }
        }

        // 设置可用语言
        AvailableLanguages = languageData.Keys.ToList();
    }

    public string CurrentLang
    {
        get => _currentLang;
        set
        {
            if (_currentLang != value)
            {
                _currentLang = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLang)));
                LanguageChanged?.Invoke();
            }
        }
    }

    public string Translate(string key)
    {
        if (_resources.ContainsKey(key) &&
            _resources[key].ContainsKey(CurrentLang))
            return _resources[key][CurrentLang];

        return key; // 找不到则返回 key
    }

    public string this[string key] => Translate(key);
}
