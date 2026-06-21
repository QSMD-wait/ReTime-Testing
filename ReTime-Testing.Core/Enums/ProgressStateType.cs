using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 进度条状态类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProgressStateType
    {
        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 进行中
        /// </summary>
        Progress,

        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused,

        /// <summary>
        /// 隐藏
        /// </summary>
        Hidden,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled
    }
}