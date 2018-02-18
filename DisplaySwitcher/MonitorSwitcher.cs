using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CCD;
using CCD.Enum;
using CCD.Struct;

namespace DisplaySwitcher
{
    public class MonitorSwitcher
    {
        public static void SetConfig(List<MonitorConfig> configToSet)
        {
            var infoArrays = GetDisplayConfigPathInfos(QueryDisplayFlags.OnlyActivePaths);
            var pathInfoArray = infoArrays.Paths;
            var modeInfoArray = infoArrays.Modes;

            var monitorsToConfigNames = configToSet.Select(c => c.Name).ToList();
            var enabledMonitorNames = pathInfoArray.Select(GetMonitorDisplayName).ToList();

            if (monitorsToConfigNames.Count == enabledMonitorNames.Count &&
                monitorsToConfigNames.All(n => enabledMonitorNames.Contains(n)))
                return;

            var monitorToDisable = enabledMonitorNames.Single(n => !monitorsToConfigNames.Contains(n));
            var monitorToEnable = monitorsToConfigNames.Single(n => !enabledMonitorNames.Contains(n));

            //this is the current active path for the monitor being disabled
            var indexOfPathToDisable = GetIndexOfPathToDisable(pathInfoArray, monitorToDisable);
            //var indexOfPathBeeingKept = Math.Abs(indexOfPathToDisable - 1);

            //this is the adapter id for the path being disabled, as we want to use that adapter id when enabling the other monitor
            var sourceIdOfPathToDisable = pathInfoArray[indexOfPathToDisable].sourceInfo.id;

            //disabing old path
            pathInfoArray[indexOfPathToDisable].flags = DisplayConfigFlags.Zero;
            

            var pathAndModeInfoToEnable = GetPathToEnableForMonitorSpecificAdapterId(monitorToEnable, sourceIdOfPathToDisable);
            var pathToEnable = pathAndModeInfoToEnable.Path;
            var modeToEnable = pathAndModeInfoToEnable.Mode;

            //enable new path, and set the source mode info (resolution/loation) to the passed in saved config
            pathToEnable.flags = DisplayConfigFlags.PathActive;
            modeToEnable.sourceMode = configToSet.Single(x => x.Name == monitorToEnable).SourceMode;

            //create new path and mode arrays
            var newModeArray = GetNewModeInfosArrayWithAddedInfo(modeInfoArray, modeToEnable);
            pathToEnable.sourceInfo.modeInfoIdx = (uint)(newModeArray.Length - 1);
            var newPathArray = new[] { pathInfoArray[0], pathInfoArray[1], pathToEnable };

            var successStatus = Wrapper.SetDisplayConfig(newPathArray.Length, newPathArray, newModeArray.Length, newModeArray,
                SdcFlags.Apply | SdcFlags.AllowChanges | SdcFlags.UseSuppliedDisplayConfig);

            if (successStatus != StatusCode.Success)
            {
                throw new Exception($"SetDisplayConfig() failed. Status: {successStatus}");
            }
        }

        private static DisplayConfigModeInfo[] GetNewModeInfosArrayWithAddedInfo(DisplayConfigModeInfo[] modeInfoArray,
            DisplayConfigModeInfo tvModeInfo)
        {
            var modeList = modeInfoArray.ToList();
            modeList.Add(tvModeInfo);
            var newModeArray = modeList.ToArray();
            return newModeArray;
        }

        private static int GetIndexOfPathToDisable(DisplayConfigPathInfo[] pathInfoArray, string monitorToDisable)
        {
            for (var i = 0; i < pathInfoArray.Length; i++)
            {
                var monitorDisplayName = GetMonitorDisplayName(pathInfoArray[i]);
                if (monitorDisplayName == monitorToDisable)
                {
                    return i;
                }
            }
            throw new Exception("Could not find path to disable");
        }

        private static string GetMonitorDisplayName(DisplayConfigPathInfo path)
        {
            var displayConfigTargetDeviceName = new DisplayConfigTargetDeviceName
            {
                header = new DisplayConfigDeviceInfoHeader
                {
                    adapterId = path.targetInfo.adapterId,
                    id = path.targetInfo.id,
                    size =
                        Marshal.SizeOf(
                            typeof(DisplayConfigTargetDeviceName)),
                    type = DisplayConfigDeviceInfoType.GetTargetName
                }
            };

            var successStatus = Wrapper.DisplayConfigGetDeviceInfo(ref displayConfigTargetDeviceName);
            if (successStatus != StatusCode.Success)
            {
                throw new Exception($"DisplayConfigGetDeviceInfo() failed. Status: {successStatus}");
            }

            var displayName = displayConfigTargetDeviceName.monitorFriendlyDeviceName;
            return displayName;
        }

        private static DisplayInfoArrays GetDisplayConfigPathInfos(QueryDisplayFlags displayFlags)
        {
            var status = Wrapper.GetDisplayConfigBufferSizes(
                displayFlags,
                out var numPathArrayElements,
                out var numModeInfoArrayElements);

            if (status != StatusCode.Success)
            {
                var reason = $"GetDisplayConfigBufferSizesFailed() failed. Status: {status}";
                throw new Exception(reason);
            }

            var pathInfoArray = new DisplayConfigPathInfo[numPathArrayElements];
            var modeInfoArray = new DisplayConfigModeInfo[numModeInfoArrayElements];

            var queryDisplayStatus = Wrapper.QueryDisplayConfig(
                displayFlags,
                ref numPathArrayElements,
                pathInfoArray,
                ref numModeInfoArrayElements, modeInfoArray);
            
            if (queryDisplayStatus != StatusCode.Success)
            {
                var reason = $"QueryDisplayConfig() failed. Status: {queryDisplayStatus}";
                throw new Exception(reason);
            }

            var infoArrays = new DisplayInfoArrays {Paths = pathInfoArray, Modes = modeInfoArray};
            return infoArrays;
        }

        private static DisplayConfigInfos GetPathToEnableForMonitorSpecificAdapterId(string monitorToEnableName, uint id)
        {
            var allPaths = GetDisplayConfigPathInfos(QueryDisplayFlags.AllPaths);
            var modeInfoArray = allPaths.Modes;
            var pathInfoArray = allPaths.Paths;

            for (var i = 0; i < pathInfoArray.Length; i++)
            {
                var path = pathInfoArray[i];
                if (path.sourceInfo.id == id)
                {
                    var monitorName = GetMonitorDisplayName(path);
                    if (monitorName == monitorToEnableName)
                    {
                        return new DisplayConfigInfos
                        {
                            Path = pathInfoArray[i],
                            Mode = modeInfoArray[pathInfoArray[i].sourceInfo.modeInfoIdx]
                        };
                    }
                }
            }

            throw new Exception("Couldn't find monitor to disable");
        }

        public class DisplayConfigInfos
        {
            public DisplayConfigPathInfo Path { get; set; }
            public DisplayConfigModeInfo Mode { get; set; }
        }

        public class DisplayInfoArrays
        {
            public DisplayConfigPathInfo[] Paths { get; set; }
            public DisplayConfigModeInfo[] Modes { get; set; }
        }

        public static List<MonitorConfig> GetCurrentConfig()
        {
            var currentConfigs = new List<MonitorConfig>();
            var currentPathInfos = GetDisplayConfigPathInfos(QueryDisplayFlags.OnlyActivePaths);
            foreach (var path in currentPathInfos.Paths)
            {
                currentConfigs.Add(new MonitorConfig
                {
                    Name = GetMonitorDisplayName(path),
                    Path = path,
                    SourceMode = currentPathInfos.Modes[path.sourceInfo.modeInfoIdx].sourceMode
                });
            }
            return currentConfigs;
        }


    }
}