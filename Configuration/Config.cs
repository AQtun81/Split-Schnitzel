using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Split_Schnitzel.Configuration
{
    internal struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    
    public static class Config
    {
        private static bool isWriting = false;
        private const string BOOLEAN_TRUE_STARTING_CHARACTERS = "1TtYy";
        private const string BOOLEAN_FALSE_STARTING_CHARACTERS = "0FfNn";
        
        private static string ConfigFolderPath => $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Split Schnitzel";

        private static string ConfigPath => $"{ConfigFolderPath}\\config.ini";

        private static readonly List<IConfigurationSection> configurationSections = new()
        {
            new PreferencesSection
            {
                SplitterPosition = 0.5f,
                SplitterWidth = 2
            },
            new AutostartSection
            {
                AutostartEnabled = false,
                LeftPanelApplication = "",
                RightPanelApplication = ""
            }
        };

        public static PreferencesSection Preferences => (configurationSections[0] as PreferencesSection)!;
        public static AutostartSection Autostart => (configurationSections[1] as AutostartSection)!;

        /// <returns>True if Succeeded, False Otherwise</returns>
        public static bool SaveConfig()
        {
            string result = "";
            foreach (IConfigurationSection section in configurationSections)
            {
                result += section.GetSectionText();
            }

            try
            {
                UTF8Encoding encoding = new();
                byte[] encoded = encoding.GetBytes(result);
                OverwriteData(encoded, ConfigPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <returns>True if Succeeded, False Otherwise</returns>
        public static bool LoadConfig()
        {
            try
            {
                ReadData(ConfigPath, out string configText);
                Regex newLinesAndSpaces = new("\\ [\\n]{1,}");
                configText = newLinesAndSpaces.Replace(configText, "\n", int.MaxValue);
                Regex sectionTitles = new("\\[.*\\]");
                MatchCollection sectionMatches = sectionTitles.Matches(configText);
                Vector2Int[] sections = new Vector2Int[sectionMatches.Count];
                for (int i = 0; i < sectionMatches.Count; i++)
                {
                    // get start and end position
                    sections[i] = i == sectionMatches.Count - 1
                        ? new Vector2Int(sectionMatches[i].Index + sectionMatches[i].Length, configText.Length)
                        : new Vector2Int(sectionMatches[i].Index + sectionMatches[i].Length, sectionMatches[i + 1].Index);
                    // convert y to length
                    sections[i].y -= sections[i].x;
                }

                for (int i = 0; i < sections.Length; i++)
                {
                    LoadSection(sectionMatches[i].Value.Trim('[', ']'), configText.Substring(sections[i].x, sections[i].y).Trim('\n'));
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            
            static void LoadSection(string sectionName, string sectionContents)
            {
                try
                {
                    int sectionPosition = 0;
                    for (int i = 0; i < configurationSections.Count; i++)
                    {
                        if (configurationSections[i].SectionName != sectionName) continue;
                        sectionPosition = i;
                        break;
                    }

                    foreach (string line in sectionContents.Split('\n'))
                    {
                        string[] split = new string[2];
                        int splitPos = line.IndexOf('=');

                        split[0] = line[..splitPos];
                        split[1] = line.Substring(splitPos + 1, line.Length - splitPos - 1);
                    
                        foreach (FieldInfo field in configurationSections[sectionPosition].GetType().GetFields())
                        {
                            if (string.Equals(field.Name, split[0], StringComparison.CurrentCultureIgnoreCase))
                            {
                                Regex trimCharacters = Type.GetTypeCode(field.FieldType) switch
                                {
                                    TypeCode.Single => new Regex("[^0-9\\,\\.]"),
                                    TypeCode.Int32 => new Regex("[^0-9]"),
                                    TypeCode.Boolean => new Regex("[^01TtFfYyNn]"),
                                    TypeCode.String => new Regex(""),
                                    _ => new Regex("[^0-9]")
                                };
                                if (field.FieldType.IsEnum) trimCharacters = new Regex("");
                                TypeConverter converter = TypeDescriptor.GetConverter(field.FieldType);
                                split[1] = trimCharacters.Replace(split[1], string.Empty, int.MaxValue);
                                object value = null;
                                if (Type.GetTypeCode(field.FieldType) == TypeCode.Boolean)
                                {
                                    if (BOOLEAN_TRUE_STARTING_CHARACTERS.Any(x => split[1].StartsWith(x)))
                                    {
                                        value = true;
                                    }
                                    else if (BOOLEAN_FALSE_STARTING_CHARACTERS.Any(x => split[1].StartsWith(x)))
                                    {
                                        value = false;
                                    }
                                }
                                else if (Type.GetTypeCode(field.FieldType) == TypeCode.Single)
                                {
                                    split[1] = new Regex("[\\.\\,]").Replace(
                                        split[1], 
                                        string.Empty, 
                                        new Regex("[\\.\\,]").Matches(split[1]).Count - 1)
                                        .Replace(',', '.');
                                    value = converter.ConvertFrom(split[1]);
                                }
                                else if (field.FieldType.IsEnum)
                                {
                                    try
                                    {
                                        value = converter.ConvertFrom(split[1]);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        value = default;
                                    }
                                }
                                else
                                {
                                    value = converter.ConvertFrom(split[1]);
                                }
                                
                                field.SetValue(configurationSections[sectionPosition], value);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Config] Failed Parsing Section {sectionName} - Invalid Field:\n{e.Message}");
                }
            }
        }

        private static async void OverwriteData(byte[] encoded, string path)
        {
            PopulatePath(path);
            while (isWriting) await Task.Delay(100);
            isWriting = true;
            await using FileStream sourceStream = File.Open(path, FileMode.OpenOrCreate);
            sourceStream.SetLength(0);
            await sourceStream.WriteAsync(encoded, 0, encoded.Length);
            isWriting = false;
        }
        
        private static void ReadData(string path, out string text)
        {
            // Check Directory
            if (!Directory.Exists(GetDirectory(path)))
            {
                text = "";
                Console.WriteLine($"DIRECTORY DOES NOT EXIST: {GetDirectory(path)}, CREATING A NEW CONFIG");
                SaveConfig();
                return;
            }
            
            // Check File
            if (!File.Exists(ConfigPath))
            {
                text = "";
                Console.WriteLine("CONFIG DOES NOT EXIST, CREATING A NEW ONE");
                SaveConfig();
                return;
            }

            // Read Data
            using FileStream sourceStream = File.Open(path, FileMode.Open);
            byte[] encoded = new byte[sourceStream.Length];
            sourceStream.Read(encoded, 0, (int)sourceStream.Length);
            
            text = Encoding.UTF8.GetString(encoded);
            sourceStream.Dispose();
        }

        private static void PopulatePath(string path)
        {
            path = GetDirectory(path);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static string GetDirectory(string path)
        {
            string[] splitPath = path.Split('\\');
            if (splitPath[^1].Contains('.'))
                path = string.Join('\\', splitPath.Take(splitPath.Length - 1));
            return path;
        }
    }
}