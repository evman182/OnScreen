using System.Configuration;
using System.IO;
using System.Linq;
using AudioSwitcher.AudioApi.CoreAudio;
using Newtonsoft.Json;

namespace DisplaySwitcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var audioControllerc = new CoreAudioController();

            if ((args.FirstOrDefault() ?? "") == "save")
            {
                if(args.Length < 2 || args[1] != "TV" || args[1] != "Normal")
                SaveMultimediaConfig(audioControllerc, args[1]);
                return;
            }

            var configToUse = "Normal";
            if (!string.IsNullOrWhiteSpace(args.FirstOrDefault()))
                configToUse = args[0];


            var tvAuth = ConfigurationManager.AppSettings["TvAuth"];
            var tvIpAddress = ConfigurationManager.AppSettings["TvIpAddress"];
            var tvInput = ConfigurationManager.AppSettings["TvInput"];
            var vizioController = new VizioController(tvAuth, tvIpAddress);
            if (configToUse == "TV")
            {
                vizioController.TurnOnTv();
                vizioController.SetInput(tvInput);
            }
            else
            {
                vizioController.TurnOffTv();
            }

            var configJson = File.ReadAllText($@"C:\MonitorConfigs\{configToUse}.txt");
            var configToSet = JsonConvert.DeserializeObject<MultimediaSetup>(configJson);
            
            MonitorSwitcher.SetConfig(configToSet.MonitorConfig);
            audioControllerc.GetDevice(configToSet.DefaultAudioId).SetAsDefault();
        }

        private static void SaveMultimediaConfig(CoreAudioController ac, string configName)
        {
            var currentAudioDefaultId = ac.DefaultPlaybackDevice.Id;
            var currentMonitorConfig = MonitorSwitcher.GetCurrentConfig();
            var currentMultimediaSetup =
                new MultimediaSetup {MonitorConfig = currentMonitorConfig, DefaultAudioId = currentAudioDefaultId};
            var currentConfigJson = JsonConvert.SerializeObject(currentMultimediaSetup);

            if (!Directory.Exists(@"C:\MonitorConfigs\"))
                Directory.CreateDirectory(@"C:\MonitorConfigs");

            File.WriteAllText($@"C:\MonitorConfigs\{configName}.txt", currentConfigJson);
        }
    }
}
