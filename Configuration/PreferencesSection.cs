namespace Split_Schnitzel.Configuration;

public class PreferencesSection : IConfigurationSection
{
    public string SectionName => "Preferences";
    public float SplitterPosition = 0.5f;
    public int SplitterWidth = 2;
}