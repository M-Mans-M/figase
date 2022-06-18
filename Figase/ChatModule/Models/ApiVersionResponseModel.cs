using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChatModule.Models
{
    /// <summary>
    /// Модель служебной информации о микросервисе
    /// </summary>
    public class ApiVersionResponseModel
    {
        public ApiVersionResponseModel()
        {
            AssemblyName info = Assembly.GetEntryAssembly().GetName();
            string container = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");

            Name = info.Name;
            Version = info.Version.ToString(4);
            Culture = Thread.CurrentThread.CurrentUICulture.Name;
            InContainer = !string.IsNullOrWhiteSpace(container) && bool.TryParse(container, out bool inContainer) && inContainer;
            OS = RuntimeInformation.OSDescription;
            Current = DateTime.Now;
            TimeZone = TimeZoneInfo.Local?.Id;
        }

        /// <summary>
        /// Наименование сервиса
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Версия сервиса
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Системная локализация сервиса
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// Признак работы сервиса внутри контейнера
        /// </summary>
        public bool InContainer { get; set; }

        /// <summary>
        /// Средения об опрерационной системе
        /// </summary>
        public string OS { get; set; }

        /// <summary>
        /// Текущее время сервера
        /// </summary>
        public DateTime Current { get; set; }

        /// <summary>
        /// Текущая таймзона
        /// </summary>
        public string TimeZone { get; set; }
    }
}