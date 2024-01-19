using System.Reflection;

namespace Split_Schnitzel.Configuration
{
    public interface IConfigurationSection
    {
        public string SectionName { get; }
        
        public string GetSectionText()
        {
            string result = $"[{SectionName}]\n";

            foreach (FieldInfo field in GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                result += $"{field.Name}={field.GetValue(this)}\n";
            }

            return result + "\n";
        }
    }
}