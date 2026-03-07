using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// TimeTop 桌面组件主配置
    /// </summary>
    public class TimeTopSetting
    {
        /// <summary>
        /// 配置版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 选中的时间计划ID
        /// </summary>
        [JsonPropertyName("selectedScheduleId")]
        public string SelectedScheduleId { get; set; } = "Default";
    }
}
