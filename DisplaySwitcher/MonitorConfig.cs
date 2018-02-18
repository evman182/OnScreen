using CCD.Struct;

namespace DisplaySwitcher
{
    public class MonitorConfig
    {
        public DisplayConfigPathInfo Path { get; set; }
        public DisplayConfigSourceMode SourceMode { get; set; }
        public string Name { get; set; }
    }
}