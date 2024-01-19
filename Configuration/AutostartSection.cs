namespace Split_Schnitzel.Configuration
{
    public class AutostartSection : IConfigurationSection
    {
        public string SectionName => "Autostart";
        public bool AutostartEnabled;
        public string LeftPanelApplication = "";
        public string RightPanelApplication = "";
    }
}