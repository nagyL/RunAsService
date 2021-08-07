using System.Collections.Generic;
using System.Linq;

namespace RunAsService
{
    public class Applications
    {
        public IEnumerable<ApplicationInfo>? Items { get; set; }
    }

    public class ApplicationInfo
    {
        public string? FileName { get; set; }
        public string? Arguments { get; set; }
        public string? WorkDir { get; set; }
        public bool RestartIfExited { get; set; }
    }
}